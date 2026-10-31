using PresentationTemplateSystem.Common;
using PresentationTemplateSystem.Models.DB;
using PresentationTemplateSystem.Models.DTO;
using PresentationTemplateSystem.Queries;
using TemplateManagement.Core.Common;
using TemplateManagement.Core.Data;
using TemplateManagement.Core.Storage;
using Microsoft.Extensions.Configuration;
using System.Collections;
using System.IO;
using System.Text.Json;

namespace PresentationTemplateSystem.Services
{
    public class UserDeckService
    {
        private readonly AppLogger _logger = AppLogger.GetLogger(typeof(UserDeckService));
        private readonly IConfiguration _configuration;
        private readonly TemplateService _templateService;
        private readonly IObjectStorage _objectStorage;

        public UserDeckService(IConfiguration configuration, TemplateService templateService, IObjectStorage objectStorage)
        {
            _configuration = configuration;
            _templateService = templateService;
            _objectStorage = objectStorage;
        }

        /// <summary>Tạo bản deck độc lập, sao chép slide và file mẫu cho người dùng sở hữu.</summary>
        public PresentationUserDeckEntity CreateFromTemplate(int templateId, Guid ownerUserId, string? customTitle = null, string? authorName = null)
        {
            try
            {
                _logger.Info($"CreateFromTemplate - Bắt đầu nhân bản bộ trình chiếu từ TemplateId: {templateId}");

                using (var db = new DatabaseManager(_configuration))
                {
                    var templateParam = new Hashtable { { "id", templateId } };
                    var template = db.SelectSingle<PresentationTemplateEntity>(TemplateQueries.SelectTemplateById, templateParam);
                    if (template == null)
                    {
                        throw new ArgumentException($"Không tìm thấy bộ template trình chiếu có ID: {templateId}");
                    }
                    if (!TemplateAssetStorage.IsTemplateStorageKey(template.FilePath ?? string.Empty, template.Code))
                    {
                        throw new InvalidOperationException($"Template {template.Code} chưa có MinIO object key hợp lệ trong file_path.");
                    }

                    // Lấy toàn bộ slide gốc
                    var slideParam = new Hashtable { { "templateId", template.Id } };
                    var originalSlides = db.SelectCollection<PresentationSlideEntity>(SlideQueries.SelectSlidesByTemplateId, slideParam);
                    if (!_objectStorage.Exists(template.FilePath!))
                    {
                        var templateSlides = originalSlides.Select(slide => new ExportSlideItem
                        {
                            SlideNumber = slide.SlideNumber,
                            Title = slide.SlideTitle,
                            Subtitle = slide.Subtitle,
                            ContentBody = slide.ContentBody,
                            Notes = slide.Notes
                        }).ToList();
                        _objectStorage.Put(
                            template.FilePath!,
                            PptxExportHelper.GeneratePptx(
                                template.Title,
                                templateSlides,
                                TemplateService.GetAccentColor(template.Code)),
                            "application/vnd.openxmlformats-officedocument.presentationml.presentation");
                    }
                    // Chuyển danh sách slide gốc thành danh sách SlideItemDto độc lập
                    var userSlides = originalSlides.Select(s => new SlideItemDto
                    {
                        SlideNumber = s.SlideNumber,
                        SlideTitle = s.SlideTitle,
                        Subtitle = s.Subtitle,
                        ContentBody = s.ContentBody,
                        Notes = s.Notes,
                        LayoutType = s.LayoutType
                    }).ToList();

                    string deckTitle = !string.IsNullOrWhiteSpace(customTitle)
                        ? customTitle.Trim()
                        : $"{template.Title} - Bản sao {DateTime.Now:yyyyMMdd_HHmm}";

                    string author = !string.IsNullOrWhiteSpace(authorName) ? authorName.Trim() : "Người dùng";
                    string deckJson = JsonSerializer.Serialize(userSlides);
                    Guid deckFileId = Guid.NewGuid();
                    string storageKey = TemplateAssetStorage.DeckUrl(deckFileId);

                    var insertParams = new Hashtable
                    {
                        { "ownerUserId", ownerUserId },
                        { "templateId", template.Id },
                        { "deckTitle", deckTitle },
                        { "authorName", author },
                        { "filePath", storageKey },
                        { "deckDataJson", deckJson },
                        { "status", "Draft" }
                    };

                    db.BeginTransaction();
                    bool objectUploaded = false;
                    try
                    {
                        object? newId = db.ExecuteScalar(UserDeckQueries.InsertUserDeck, insertParams);
                        int deckId = Convert.ToInt32(newId);
                        _objectStorage.Put(
                            storageKey,
                            _objectStorage.Get(template.FilePath!),
                            "application/vnd.openxmlformats-officedocument.presentationml.presentation");
                        objectUploaded = true;
                        db.ExecuteNonQuery(TemplateQueries.IncrementUsageCount, templateParam);
                        db.Commit();

                        _logger.Info($"CreateFromTemplate - Đã tạo bản trình chiếu độc lập ID: {deckId} từ Template {templateId}");
                        return GetUserDeckById(deckId, ownerUserId)!;
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
                _logger.Error($"CreateFromTemplate - Lỗi khi nhân bản template {templateId}", ex);
                throw;
            }
        }

        /// <summary>Lấy deck theo ID có kiểm tra quyền sở hữu của người dùng.</summary>
        public PresentationUserDeckEntity? GetUserDeckById(int id, Guid ownerUserId)
        {
            try
            {
                _logger.Info($"GetUserDeckById - Lấy bản trình chiếu ID: {id}");
                using (var db = new DatabaseManager(_configuration))
                {
                    var parameters = new Hashtable { { "id", id }, { "ownerUserId", ownerUserId } };
                    return db.SelectSingle<PresentationUserDeckEntity>(UserDeckQueries.SelectUserDeckById, parameters);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"GetUserDeckById - Lỗi lấy bản trình chiếu ID {id}", ex);
                throw;
            }
        }

        /// <summary>Lấy danh sách deck thuộc người dùng được chỉ định.</summary>
        public List<PresentationUserDeckEntity> GetAllUserDecks(Guid ownerUserId)
        {
            try
            {
                _logger.Info("GetAllUserDecks - Lấy danh sách bản trình chiếu người dùng");
                using (var db = new DatabaseManager(_configuration))
                {
                    return db.SelectCollection<PresentationUserDeckEntity>(
                        UserDeckQueries.SelectAllUserDecks,
                        new Hashtable { { "ownerUserId", ownerUserId } });
                }
            }
            catch (Exception ex)
            {
                _logger.Error("GetAllUserDecks - Lỗi lấy danh sách bản trình chiếu", ex);
                throw;
            }
        }

        /// <summary>Lưu nội dung slide và metadata cho deck thuộc người dùng.</summary>
        public bool SaveUserDeck(SaveDeckRequest req, Guid ownerUserId)
        {
            try
            {
                _logger.Info($"SaveUserDeck - Lưu bản trình chiếu ID: {req.Id}, Tiêu đề: '{req.DeckTitle}' ({req.Slides.Count} slides)");

                using (var db = new DatabaseManager(_configuration))
                {
                    string deckJson = JsonSerializer.Serialize(req.Slides);

                    var parameters = new Hashtable
                    {
                        { "id", req.Id },
                        { "ownerUserId", ownerUserId },
                        { "deckTitle", req.DeckTitle },
                        { "deckDataJson", deckJson },
                        { "status", req.Status ?? "Draft" }
                    };

                    var existing = db.SelectSingle<PresentationUserDeckEntity>(UserDeckQueries.SelectUserDeckById,
                        new Hashtable { { "id", req.Id }, { "ownerUserId", ownerUserId } });
                    if (existing == null)
                    {
                        return false;
                    }
                    string storageKey = TemplateAssetStorage.IsDeckStorageKey(existing.FilePath ?? string.Empty)
                        ? existing.FilePath!
                        : TemplateAssetStorage.DeckUrl(Guid.NewGuid());

                    db.BeginTransaction();
                    int rows = db.ExecuteNonQuery(UserDeckQueries.UpdateUserDeck, parameters);
                    if (rows > 0)
                    {
                        var deck = db.SelectSingle<PresentationUserDeckEntity>(
                            UserDeckQueries.SelectUserDeckById,
                            new Hashtable { { "id", req.Id }, { "ownerUserId", ownerUserId } });
                        if (deck == null)
                        {
                            throw new InvalidOperationException($"Không tìm thấy bản trình chiếu {req.Id} sau khi lưu.");
                        }

                        byte[] bytes = deck.TemplateId is int templateId
                            ? GenerateDeckFromTemplate(templateId, req.DeckTitle, req.Slides)
                            : PptxExportHelper.GeneratePptx(req.DeckTitle, req.Slides.Select((slide, index) => new ExportSlideItem
                            {
                                SlideNumber = index + 1,
                                Title = slide.SlideTitle,
                                Subtitle = slide.Subtitle,
                                ContentBody = slide.ContentBody,
                                Notes = slide.Notes
                            }).ToList());
                        _objectStorage.Put(
                            storageKey,
                            bytes,
                            "application/vnd.openxmlformats-officedocument.presentationml.presentation");
                        db.ExecuteNonQuery(UserDeckQueries.UpdateUserDeckFilePath, new Hashtable
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
                _logger.Error($"SaveUserDeck - Lỗi lưu bản trình chiếu ID {req.Id}", ex);
                throw;
            }
        }

        /// <summary>Xóa deck và file lưu trữ khi deck thuộc người dùng hiện tại.</summary>
        public bool DeleteUserDeck(int id, Guid ownerUserId)
        {
            try
            {
                _logger.Info($"DeleteUserDeck - Xóa bản trình chiếu ID: {id}");
                using (var db = new DatabaseManager(_configuration))
                {
                    var deck = db.SelectSingle<PresentationUserDeckEntity>(UserDeckQueries.SelectUserDeckById,
                        new Hashtable { { "id", id }, { "ownerUserId", ownerUserId } });
                    if (deck == null)
                    {
                        return false;
                    }
                    db.BeginTransaction();
                    int rows = db.ExecuteNonQuery(UserDeckQueries.DeleteUserDeck,
                        new Hashtable { { "id", id }, { "ownerUserId", ownerUserId } });
                    db.Commit();
                    if (TemplateAssetStorage.IsDeckStorageKey(deck.FilePath ?? string.Empty))
                    {
                        _objectStorage.Delete(deck.FilePath!);
                    }
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"DeleteUserDeck - Lỗi xóa bản trình chiếu ID {id}", ex);
                throw;
            }
        }

        /// <summary>Xuất deck thuộc người dùng thành file PPTX và trả về tên file.</summary>
        public byte[] ExportPptx(int deckId, Guid ownerUserId, out string fileName)
        {
            var deck = GetUserDeckById(deckId, ownerUserId);
            if (deck == null)
            {
                throw new ArgumentException($"Không tìm thấy bản trình chiếu ID: {deckId}");
            }

            string safeTitle = string.Concat(deck.DeckTitle.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
            fileName = $"{safeTitle}_{DateTime.Now:yyyyMMdd_HHmm}.pptx";

            if (TemplateAssetStorage.IsDeckStorageKey(deck.FilePath ?? string.Empty))
            {
                return _objectStorage.Get(deck.FilePath!);
            }

            var slides = string.IsNullOrWhiteSpace(deck.DeckDataJson)
                ? new List<SlideItemDto>()
                : JsonSerializer.Deserialize<List<SlideItemDto>>(deck.DeckDataJson)
                    ?? throw new InvalidOperationException($"Dữ liệu slide của bản trình chiếu {deckId} không hợp lệ.");
            var exportItems = slides.Select((slide, index) => new ExportSlideItem
            {
                SlideNumber = index + 1,
                Title = slide.SlideTitle,
                Subtitle = slide.Subtitle,
                ContentBody = slide.ContentBody,
                Notes = slide.Notes
            }).ToList();

            byte[] pptx = deck.TemplateId is int templateId
                ? GenerateDeckFromTemplate(templateId, deck.DeckTitle, slides)
                : PptxExportHelper.GeneratePptx(deck.DeckTitle, exportItems);
            string storageKey = TemplateAssetStorage.DeckUrl(Guid.NewGuid());
            _objectStorage.Put(
                storageKey,
                pptx,
                "application/vnd.openxmlformats-officedocument.presentationml.presentation");
            using (var db = new DatabaseManager(_configuration))
            {
                db.ExecuteNonQuery(UserDeckQueries.UpdateUserDeckFilePath, new Hashtable
                {
                    { "id", deck.Id },
                    { "ownerUserId", ownerUserId },
                    { "filePath", storageKey }
                });
            }
            return pptx;
        }

        /// <summary>Tạo file deck từ mẫu PPTX gốc hoặc dựng deck mới khi mẫu chưa tồn tại.</summary>
        private byte[] GenerateDeckFromTemplate(int templateId, string deckTitle, List<SlideItemDto> slides)
        {
            var template = _templateService.GetTemplateById(templateId);
            if (template == null)
            {
                throw new InvalidOperationException($"Không tìm thấy template gốc có ID: {templateId}.");
            }
            if (!TemplateAssetStorage.IsTemplateStorageKey(template.FilePath ?? string.Empty, template.Code))
            {
                throw new InvalidOperationException($"Template {template.Code} chưa có MinIO object key hợp lệ trong file_path.");
            }

            var exportSlides = slides.Select((s, index) => new ExportSlideItem
            {
                SlideNumber = index + 1,
                Title = s.SlideTitle,
                Subtitle = s.Subtitle,
                ContentBody = s.ContentBody,
                Notes = s.Notes
            }).ToList();
            return PptxExportHelper.GeneratePptxFromTemplate(
                _objectStorage.Get(template.FilePath!),
                deckTitle,
                exportSlides);
        }
    }
}
