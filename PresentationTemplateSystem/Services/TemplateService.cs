using PresentationTemplateSystem.Common;
using PresentationTemplateSystem.Models.DB;
using PresentationTemplateSystem.Models.DTO;
using PresentationTemplateSystem.Queries;
using TemplateManagement.Core.Common;
using TemplateManagement.Core.Data;
using Microsoft.Extensions.Configuration;
using TemplateManagement.Core.Security;
using TemplateManagement.Core.Storage;
using System.Collections;
using System.IO;

namespace PresentationTemplateSystem.Services
{
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

        /// <summary>Lấy danh sách danh mục bộ trình chiếu.</summary>
        public List<PresentationCategoryEntity> GetCategories()
        {
            try
            {
                _logger.Info("GetCategories - Bắt đầu lấy danh mục mẫu trình chiếu");
                using (var db = new DatabaseManager(_configuration))
                {
                    return db.SelectCollection<PresentationCategoryEntity>(CategoryQueries.SelectAllCategories);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("GetCategories - Lỗi truy vấn danh mục slide", ex);
                throw;
            }
        }

        /// <summary>Lọc bộ trình chiếu và nạp các slide cùng asset xem trước của từng bộ.</summary>
        public List<PresentationTemplateEntity> GetTemplates(int? categoryId = null, string? keyword = null)
        {
            try
            {
                _logger.Info($"GetTemplates - Tìm kiếm bộ template CategoryId: {categoryId}, Keyword: '{keyword}'");
                using (var db = new DatabaseManager(_configuration))
                {
                    var parameters = new Hashtable
                    {
                        { "categoryId", (object?)categoryId ?? DBNull.Value },
                        { "keyword", (object?)keyword?.Trim() ?? DBNull.Value }
                    };

                    var templates = db.SelectCollection<PresentationTemplateEntity>(TemplateQueries.SelectTemplatesWithFilter, parameters);

                    // Nạp danh sách slide xem trước cho từng template
                    foreach (var t in templates)
                    {
                        var slideParam = new Hashtable { { "templateId", t.Id } };
                        t.Slides = db.SelectCollection<PresentationSlideEntity>(SlideQueries.SelectSlidesByTemplateId, slideParam);
                        foreach (var slide in t.Slides)
                        {
                            slide.SlideTitle = HtmlContentSanitizer.Sanitize(slide.SlideTitle);
                            slide.Subtitle = HtmlContentSanitizer.Sanitize(slide.Subtitle ?? string.Empty);
                            slide.ContentBody = HtmlContentSanitizer.Sanitize(slide.ContentBody ?? string.Empty);
                            slide.Notes = HtmlContentSanitizer.Sanitize(slide.Notes ?? string.Empty);
                        }
                        EnsureTemplateAssets(db, t);
                    }

                    return templates;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("GetTemplates - Lỗi truy vấn bộ template trình chiếu", ex);
                throw;
            }
        }

        /// <summary>Lấy bộ trình chiếu theo ID cùng danh sách slide và asset cần thiết.</summary>
        public PresentationTemplateEntity? GetTemplateById(int id)
        {
            try
            {
                _logger.Info($"GetTemplateById - Lấy chi tiết bộ template ID: {id}");
                using (var db = new DatabaseManager(_configuration))
                {
                    var parameters = new Hashtable { { "id", id } };
                    var template = db.SelectSingle<PresentationTemplateEntity>(TemplateQueries.SelectTemplateById, parameters);
                    if (template != null)
                    {
                        var slideParam = new Hashtable { { "templateId", template.Id } };
                        template.Slides = db.SelectCollection<PresentationSlideEntity>(SlideQueries.SelectSlidesByTemplateId, slideParam);
                        foreach (var slide in template.Slides)
                        {
                            slide.SlideTitle = HtmlContentSanitizer.Sanitize(slide.SlideTitle);
                            slide.Subtitle = HtmlContentSanitizer.Sanitize(slide.Subtitle ?? string.Empty);
                            slide.ContentBody = HtmlContentSanitizer.Sanitize(slide.ContentBody ?? string.Empty);
                            slide.Notes = HtmlContentSanitizer.Sanitize(slide.Notes ?? string.Empty);
                        }
                        EnsureTemplateAssets(db, template);
                    }
                    return template;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"GetTemplateById - Lỗi lấy bộ template ID {id}", ex);
                throw;
            }
        }

        /// <summary>Tạo mẫu trình chiếu, các slide và file PPTX trong một luồng giao dịch.</summary>
        public int CreateTemplate(CreatePresentationTemplateRequest req)
        {
            try
            {
                if (req.Slides == null || req.Slides.Count == 0)
                {
                    throw new ArgumentException("Bộ template phải có ít nhất một slide.", nameof(req));
                }

                _logger.Info($"CreateTemplate - Thêm bộ template mới: {req.Title} (Code: {req.Code})");

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
                        { "totalSlides", req.Slides.Count },
                        { "tags", req.Tags ?? string.Empty }
                    };

                    db.BeginTransaction();
                    object? newId = db.ExecuteScalar(TemplateQueries.InsertTemplate, parameters);
                    int templateId = Convert.ToInt32(newId);
                    string accentColor = GetAccentColor(req.Code);
                    bool objectUploaded = false;

                    try
                    {
                        // Lưu số thứ tự liên tục và nội dung đã làm sạch cho từng slide.
                        int slideNumber = 1;
                        foreach (var s in req.Slides)
                        {
                            int number = slideNumber++;
                            string slideTitle = HtmlContentSanitizer.Sanitize(s.SlideTitle);
                            string subtitle = HtmlContentSanitizer.Sanitize(s.Subtitle ?? string.Empty);
                            string contentBody = HtmlContentSanitizer.Sanitize(s.ContentBody ?? string.Empty);
                            string notes = HtmlContentSanitizer.Sanitize(s.Notes ?? string.Empty);
                            string imageUrl = TemplateAssetStorage.EnsureSlidePreview(
                                req.Code, number, slideTitle, subtitle, contentBody, accentColor);
                            var sp = new Hashtable
                            {
                                { "templateId", templateId },
                                { "slideNumber", number },
                                { "slideTitle", slideTitle },
                                { "subtitle", subtitle },
                                { "contentBody", contentBody },
                                { "notes", notes },
                                { "layoutType", s.LayoutType ?? "TitleAndContent" },
                                { "imageUrl", imageUrl }
                            };
                            db.ExecuteNonQuery(SlideQueries.InsertSlide, sp);
                        }

                        var templateSlides = req.Slides.Select((slide, index) => new ExportSlideItem
                        {
                            SlideNumber = index + 1,
                            Title = HtmlContentSanitizer.Sanitize(slide.SlideTitle),
                            Subtitle = HtmlContentSanitizer.Sanitize(slide.Subtitle ?? string.Empty),
                            ContentBody = HtmlContentSanitizer.Sanitize(slide.ContentBody ?? string.Empty),
                            Notes = HtmlContentSanitizer.Sanitize(slide.Notes ?? string.Empty)
                        }).ToList();
                        _objectStorage.Put(
                            TemplateAssetStorage.TemplateUrl(req.Code),
                            PptxExportHelper.GeneratePptx(req.Title, templateSlides, accentColor),
                            "application/vnd.openxmlformats-officedocument.presentationml.presentation");
                        objectUploaded = true;
                        TemplateAssetStorage.EnsureThumbnail(req.Code, req.Title, accentColor);
                        db.Commit();
                        return templateId;
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
                _logger.Error("CreateTemplate - Lỗi thêm bộ template mới", ex);
                throw;
            }
        }

        /// <summary>Đảm bảo file PPTX và thumbnail tồn tại, đồng bộ khóa asset vào database.</summary>
        private void EnsureTemplateAssets(DatabaseManager db, PresentationTemplateEntity template)
        {
            string filePath = TemplateAssetStorage.TemplateUrl(template.Code);
            string thumbnailUrl = TemplateAssetStorage.ThumbnailUrl(template.Code);
            // Khôi phục object bị thiếu từ các slide đã lưu trong database.
            if (!_objectStorage.Exists(filePath))
            {
                var exportSlides = template.Slides.Select(slide => new ExportSlideItem
                {
                    SlideNumber = slide.SlideNumber,
                    Title = slide.SlideTitle,
                    Subtitle = slide.Subtitle,
                    ContentBody = slide.ContentBody,
                    Notes = slide.Notes
                }).ToList();
                _objectStorage.Put(
                    filePath,
                    PptxExportHelper.GeneratePptx(template.Title, exportSlides, GetAccentColor(template.Code)),
                    "application/vnd.openxmlformats-officedocument.presentationml.presentation");
            }
            TemplateAssetStorage.EnsureThumbnail(template.Code, template.Title, GetAccentColor(template.Code));
            foreach (var slide in template.Slides)
            {
                string imageUrl = TemplateAssetStorage.EnsureSlidePreview(
                    template.Code, slide.SlideNumber, slide.SlideTitle, slide.Subtitle, slide.ContentBody, GetAccentColor(template.Code));
                if (slide.ImageUrl != imageUrl)
                {
                    db.ExecuteNonQuery(SlideQueries.UpdateSlideImageUrl, new Hashtable
                    {
                        { "id", slide.Id },
                        { "imageUrl", imageUrl }
                    });
                    slide.ImageUrl = imageUrl;
                }
            }
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

        /// <summary>Chọn màu nhấn nhất quán theo mã bộ trình chiếu.</summary>
        internal static string GetAccentColor(string code) => code switch
        {
            "PPT-001" => "7030A0",
            "PPT-002" => "008C95",
            "PPT-003" => "F28C28",
            _ => "0D6EFD"
        };
    }
}
