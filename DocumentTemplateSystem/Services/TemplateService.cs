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
    /// Service xử lý nghiệp vụ Quản lý và Khai thác Thư viện Mẫu Văn Bản
    /// </summary>
    public class TemplateService
    {
        private readonly AppLogger _logger = AppLogger.GetLogger(typeof(TemplateService));
        private readonly IConfiguration _configuration;
        private readonly IObjectStorage _objectStorage;

        public TemplateService(IConfiguration configuration, IObjectStorage objectStorage)
        {
            _configuration = configuration;
            _objectStorage = objectStorage;
        }

        /// <summary>
        /// Lấy toàn bộ danh mục mẫu văn bản
        /// </summary>
        public List<DocCategoryEntity> GetCategories()
        {
            try
            {
                _logger.Info("GetCategories - Bắt đầu lấy danh sách danh mục");
                using (var db = new DatabaseManager(_configuration))
                {
                    return db.SelectCollection<DocCategoryEntity>(CategoryQueries.SelectAllCategories);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("GetCategories - Lỗi truy vấn danh mục", ex);
                throw;
            }
        }

        /// <summary>
        /// Tìm kiếm và lọc danh sách mẫu văn bản theo danh mục và từ khóa
        /// </summary>
        public List<DocTemplateEntity> GetTemplates(int? categoryId = null, string? keyword = null)
        {
            try
            {
                _logger.Info($"GetTemplates - Tìm kiếm với CategoryId: {categoryId}, Keyword: '{keyword}'");
                using (var db = new DatabaseManager(_configuration))
                {
                    var parameters = new Hashtable
                    {
                        { "categoryId", (object?)categoryId ?? DBNull.Value },
                        { "keyword", (object?)keyword?.Trim() ?? DBNull.Value }
                    };

                    var templates = db.SelectCollection<DocTemplateEntity>(TemplateQueries.SelectTemplatesWithFilter, parameters);
                    foreach (var template in templates)
                    {
                        EnsureTemplateAssets(db, template);
                        template.ContentHtml = HtmlContentSanitizer.Sanitize(template.ContentHtml);
                    }
                    return templates;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("GetTemplates - Lỗi truy vấn danh sách mẫu văn bản", ex);
                throw;
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết một mẫu văn bản theo ID
        /// </summary>
        public DocTemplateEntity? GetTemplateById(int id)
        {
            try
            {
                _logger.Info($"GetTemplateById - Lấy chi tiết mẫu ID: {id}");
                using (var db = new DatabaseManager(_configuration))
                {
                    var parameters = new Hashtable { { "id", id } };
                    var template = db.SelectSingle<DocTemplateEntity>(TemplateQueries.SelectTemplateById, parameters);
                    if (template != null)
                    {
                        EnsureTemplateAssets(db, template);
                        template.ContentHtml = HtmlContentSanitizer.Sanitize(template.ContentHtml);
                    }
                    return template;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"GetTemplateById - Lỗi lấy mẫu ID {id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Thêm mẫu văn bản mới vào thư viện (mở rộng linh hoạt mà không cần sửa giao diện)
        /// </summary>
        public int CreateTemplate(CreateTemplateRequest req)
        {
            try
            {
                _logger.Info($"CreateTemplate - Thêm mẫu mới: {req.Title} (Code: {req.Code})");

                using (var db = new DatabaseManager(_configuration))
                {
                    var parameters = new Hashtable
                    {
                        { "code", req.Code },
                        { "title", req.Title },
                        { "categoryId", req.CategoryId },
                        { "description", req.Description ?? string.Empty },
                        { "thumbnailUrl", TemplateAssetStorage.ThumbnailUrl(req.Code) },
                        { "filePath", TemplateAssetStorage.TemplateUrl(req.Code) },
                        { "contentHtml", HtmlContentSanitizer.Sanitize(req.ContentHtml) },
                        { "tags", req.Tags ?? string.Empty }
                    };

                    db.BeginTransaction();
                    bool objectUploaded = false;
                    try
                    {
                        object? newId = db.ExecuteScalar(TemplateQueries.InsertTemplate, parameters);
                        _objectStorage.Put(
                            TemplateAssetStorage.TemplateUrl(req.Code),
                            DocxExportHelper.GenerateDocx(req.Title, HtmlContentSanitizer.Sanitize(req.ContentHtml)),
                            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                        objectUploaded = true;
                        TemplateAssetStorage.EnsureThumbnail(req.Code, req.Title, "0D6EFD");
                        db.Commit();
                        return Convert.ToInt32(newId);
                    }
                    catch
                    {
                        db.Rollback();
                        if (objectUploaded)
                        {
                            _objectStorage.Delete(TemplateAssetStorage.TemplateUrl(req.Code));
                        }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("CreateTemplate - Lỗi thêm mẫu văn bản mới", ex);
                throw;
            }
        }

        /// <summary>Đảm bảo file mẫu và thumbnail tồn tại, đồng bộ khóa asset vào database.</summary>
        private void EnsureTemplateAssets(DatabaseManager db, DocTemplateEntity template)
        {
            string filePath = TemplateAssetStorage.TemplateUrl(template.Code);
            string thumbnailUrl = TemplateAssetStorage.ThumbnailUrl(template.Code);
            // Tạo lại file từ nội dung database khi object bị thiếu để luồng dùng mẫu vẫn khôi phục được.
            if (!_objectStorage.Exists(filePath))
            {
                _objectStorage.Put(
                    filePath,
                    DocxExportHelper.GenerateDocx(template.Title, HtmlContentSanitizer.Sanitize(template.ContentHtml)),
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            }
            TemplateAssetStorage.EnsureThumbnail(template.Code, template.Title, "0D6EFD");
            if (template.FilePath != filePath || template.ThumbnailUrl != thumbnailUrl)
            {
                db.ExecuteNonQuery(TemplateQueries.UpdateTemplateAssets, new Hashtable
                {
                    { "id", template.Id },
                    { "filePath", filePath },
                    { "thumbnailUrl", thumbnailUrl }
                });
                template.FilePath = filePath;
                template.ThumbnailUrl = thumbnailUrl;
            }
        }
    }
}
