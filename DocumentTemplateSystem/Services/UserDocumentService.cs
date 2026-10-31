using DocumentTemplateSystem.Common;
using DocumentTemplateSystem.Models.DB;
using DocumentTemplateSystem.Models.DTO;
using DocumentTemplateSystem.Queries;
using TemplateManagement.Core.Common;
using TemplateManagement.Core.Data;
using TemplateManagement.Core.Security;
using TemplateManagement.Core.Storage;
using Microsoft.Extensions.Configuration;
using System.Collections;
using System.IO;

namespace DocumentTemplateSystem.Services
{
    /// <summary>
    /// Service xử lý nghiệp vụ Tài Liệu Người Dùng
    /// Đảm bảo nguyên tắc cô lập: Tạo tài liệu mới là bản sao độc lập, hoàn toàn không sửa đổi Template gốc.
    /// </summary>
    public class UserDocumentService
    {
        private readonly AppLogger _logger = AppLogger.GetLogger(typeof(UserDocumentService));
        private readonly IConfiguration _configuration;
        private readonly IObjectStorage _objectStorage;

        public UserDocumentService(IConfiguration configuration, IObjectStorage objectStorage)
        {
            _configuration = configuration;
            _objectStorage = objectStorage;
        }

        /// <summary>
        /// Tạo tài liệu mới dựa trên mẫu văn bản được chọn
        /// </summary>
        /// <param name="templateId">ID mẫu gốc</param>
        /// <param name="customTitle">Tiêu đề tài liệu do người dùng đặt (hoặc tự sinh)</param>
        /// <param name="authorName">Tên người tạo</param>
        /// <returns>Tài liệu mới tạo</returns>
        public DocUserDocumentEntity CreateFromTemplate(int templateId, Guid ownerUserId, string? customTitle = null, string? authorName = null)
        {
            try
            {
                _logger.Info($"CreateFromTemplate - Khởi tạo tài liệu mới từ TemplateId: {templateId}");

                using (var db = new DatabaseManager(_configuration))
                {
                    // 1. Lấy thông tin Template gốc
                    var templateParam = new Hashtable { { "id", templateId } };
                    var template = db.SelectSingle<DocTemplateEntity>(TemplateQueries.SelectTemplateById, templateParam);
                    if (template == null)
                    {
                        throw new ArgumentException($"Không tìm thấy mẫu văn bản có ID: {templateId}");
                    }
                    if (!TemplateAssetStorage.IsTemplateStorageKey(template.FilePath ?? string.Empty, template.Code))
                    {
                        throw new InvalidOperationException($"Template {template.Code} chưa có MinIO object key hợp lệ trong file_path.");
                    }
                    if (!_objectStorage.Exists(template.FilePath!))
                    {
                        _objectStorage.Put(
                            template.FilePath!,
                            DocxExportHelper.GenerateDocx(
                                template.Title,
                                HtmlContentSanitizer.Sanitize(template.ContentHtml)),
                            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                    }

                    // 2. Tạo bản sao tài liệu độc lập với nội dung từ template
                    string docTitle = !string.IsNullOrWhiteSpace(customTitle)
                        ? customTitle.Trim()
                        : $"{template.Title} - Bản sao {DateTime.Now:yyyyMMdd_HHmm}";

                    string author = !string.IsNullOrWhiteSpace(authorName) ? authorName.Trim() : "Người dùng";
                    Guid documentId = Guid.NewGuid();
                    string storageKey = TemplateAssetStorage.DocumentUrl(documentId);

                    var insertParams = new Hashtable
                    {
                        { "ownerUserId", ownerUserId },
                        { "templateId", template.Id },
                        { "documentTitle", docTitle },
                        { "contentHtml", HtmlContentSanitizer.Sanitize(template.ContentHtml) },
                        { "filePath", storageKey },
                        { "authorName", author },
                        { "status", "Draft" }
                    };

                    db.BeginTransaction();
                    bool objectUploaded = false;
                    try
                    {
                        object? newId = db.ExecuteScalar(UserDocumentQueries.InsertUserDocument, insertParams);
                        int userDocId = Convert.ToInt32(newId);
                        _objectStorage.Put(
                            storageKey,
                            _objectStorage.Get(template.FilePath!),
                            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                        objectUploaded = true;
                        db.ExecuteNonQuery(TemplateQueries.IncrementUsageCount, templateParam);
                        db.Commit();

                        _logger.Info($"CreateFromTemplate - Đã tạo thành công tài liệu mới ID: {userDocId} từ Template ID: {templateId}");
                        return GetUserDocumentById(userDocId, ownerUserId)!;
                    }
                    catch
                    {
                        db.Rollback();
                        if (objectUploaded)
                        {
                            _objectStorage.Delete(storageKey);
                        }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"CreateFromTemplate - Lỗi khi tạo tài liệu từ mẫu {templateId}", ex);
                throw;
            }
        }

        /// <summary>
        /// Lấy thông tin tài liệu người dùng theo ID
        /// </summary>
        public DocUserDocumentEntity? GetUserDocumentById(int id, Guid ownerUserId)
        {
            try
            {
                _logger.Info($"GetUserDocumentById - Truy vấn tài liệu ID: {id}");
                using (var db = new DatabaseManager(_configuration))
                {
                    var parameters = new Hashtable { { "id", id }, { "ownerUserId", ownerUserId } };
                    var document = db.SelectSingle<DocUserDocumentEntity>(UserDocumentQueries.SelectUserDocumentById, parameters);
                    if (document != null)
                    {
                        document.ContentHtml = HtmlContentSanitizer.Sanitize(document.ContentHtml);
                    }
                    return document;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"GetUserDocumentById - Lỗi truy vấn tài liệu ID {id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Lấy tất cả tài liệu người dùng đã tạo
        /// </summary>
        public List<DocUserDocumentEntity> GetAllUserDocuments(Guid ownerUserId)
        {
            try
            {
                _logger.Info("GetAllUserDocuments - Lấy danh sách tài liệu người dùng");
                using (var db = new DatabaseManager(_configuration))
                {
                    var documents = db.SelectCollection<DocUserDocumentEntity>(
                        UserDocumentQueries.SelectAllUserDocuments,
                        new Hashtable { { "ownerUserId", ownerUserId } });
                    foreach (var document in documents)
                    {
                        document.ContentHtml = HtmlContentSanitizer.Sanitize(document.ContentHtml);
                    }
                    return documents;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("GetAllUserDocuments - Lỗi lấy danh sách tài liệu người dùng", ex);
                throw;
            }
        }

        /// <summary>
        /// Cập nhật nội dung tài liệu người dùng từ trình soạn thảo
        /// </summary>
        public bool SaveUserDocument(SaveDocumentRequest req, Guid ownerUserId)
        {
            try
            {
                _logger.Info($"SaveUserDocument - Lưu tài liệu ID: {req.Id}, Tiêu đề: '{req.DocumentTitle}'");

                using (var db = new DatabaseManager(_configuration))
                {
                    var existing = db.SelectSingle<DocUserDocumentEntity>(UserDocumentQueries.SelectUserDocumentById,
                        new Hashtable { { "id", req.Id }, { "ownerUserId", ownerUserId } });
                    if (existing == null)
                    {
                        return false;
                    }

                    string storageKey = TemplateAssetStorage.IsDocumentStorageKey(existing.FilePath ?? string.Empty)
                        ? existing.FilePath!
                        : TemplateAssetStorage.DocumentUrl(Guid.NewGuid());
                    var parameters = new Hashtable
                    {
                        { "id", req.Id },
                        { "ownerUserId", ownerUserId },
                        { "documentTitle", req.DocumentTitle },
                        { "contentHtml", HtmlContentSanitizer.Sanitize(req.ContentHtml) },
                        { "status", req.Status ?? "Draft" }
                    };

                    db.BeginTransaction();
                    int rows = db.ExecuteNonQuery(UserDocumentQueries.UpdateUserDocument, parameters);
                    if (rows > 0)
                    {
                        _objectStorage.Put(
                            storageKey,
                            DocxExportHelper.GenerateDocx(req.DocumentTitle, HtmlContentSanitizer.Sanitize(req.ContentHtml)),
                            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                        db.ExecuteNonQuery(UserDocumentQueries.UpdateUserDocumentFilePath, new Hashtable
                        {
                            { "id", req.Id },
                            { "ownerUserId", ownerUserId },
                            { "filePath", storageKey }
                        });
                        db.Commit();
                    }
                    else
                    {
                        db.Rollback();
                    }
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"SaveUserDocument - Lỗi khi lưu tài liệu ID {req.Id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Xóa tài liệu người dùng
        /// </summary>
        public bool DeleteUserDocument(int id, Guid ownerUserId)
        {
            try
            {
                _logger.Info($"DeleteUserDocument - Xóa tài liệu ID: {id}");
                using (var db = new DatabaseManager(_configuration))
                {
                    var document = db.SelectSingle<DocUserDocumentEntity>(UserDocumentQueries.SelectUserDocumentById,
                        new Hashtable { { "id", id }, { "ownerUserId", ownerUserId } });
                    if (document == null)
                    {
                        return false;
                    }
                    db.BeginTransaction();
                    int rows = db.ExecuteNonQuery(UserDocumentQueries.DeleteUserDocument,
                        new Hashtable { { "id", id }, { "ownerUserId", ownerUserId } });
                    db.Commit();
                    if (TemplateAssetStorage.IsDocumentStorageKey(document.FilePath ?? string.Empty))
                    {
                        _objectStorage.Delete(document.FilePath!);
                    }
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"DeleteUserDocument - Lỗi xóa tài liệu ID {id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Xuất tài liệu ra định dạng Microsoft Word (.docx) chuẩn OpenXML
        /// </summary>
        public byte[] ExportDocx(int documentId, Guid ownerUserId, out string fileName)
        {
            var doc = GetUserDocumentById(documentId, ownerUserId);
            if (doc == null)
            {
                throw new ArgumentException($"Không tìm thấy tài liệu với ID: {documentId}");
            }

            // Tạo tên file an toàn
            string safeTitle = string.Concat(doc.DocumentTitle.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
            fileName = $"{safeTitle}_{DateTime.Now:yyyyMMdd_HHmm}.docx";

            // Gọi tiện ích xuất OpenXML Word
            if (TemplateAssetStorage.IsDocumentStorageKey(doc.FilePath ?? string.Empty))
            {
                return _objectStorage.Get(doc.FilePath!);
            }

            string storageKey = TemplateAssetStorage.DocumentUrl(Guid.NewGuid());
            byte[] content = DocxExportHelper.GenerateDocx(doc.DocumentTitle, HtmlContentSanitizer.Sanitize(doc.ContentHtml));
            _objectStorage.Put(
                storageKey,
                content,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            using (var db = new DatabaseManager(_configuration))
            {
                db.ExecuteNonQuery(UserDocumentQueries.UpdateUserDocumentFilePath, new Hashtable
                {
                    { "id", doc.Id },
                    { "ownerUserId", ownerUserId },
                    { "filePath", storageKey }
                });
            }
            return content;
        }
    }
}
