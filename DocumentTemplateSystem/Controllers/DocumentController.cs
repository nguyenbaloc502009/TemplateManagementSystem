using DocumentTemplateSystem.Models.ViewModels;
using DocumentTemplateSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DocumentTemplateSystem.Controllers
{
    /// <summary>
    /// MVC Controller quản lý giao diện Thư viện Mẫu Văn Bản và Trình Soạn Thảo
    /// </summary>
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly TemplateService _templateService;
        private readonly UserDocumentService _userDocService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            TemplateService templateService,
            UserDocumentService userDocService,
            ILogger<DocumentController> logger)
        {
            _templateService = templateService;
            _userDocService = userDocService;
            _logger = logger;
        }

        /// <summary>
        /// Màn hình Thư viện Mẫu Văn Bản
        /// Hiển thị danh sách mẫu dạng lưới/danh sách, tìm kiếm, lọc danh mục
        /// </summary>
        [AllowAnonymous]
        public IActionResult Index(int? categoryId, string? keyword)
        {
            var categories = _templateService.GetCategories();
            var templates = _templateService.GetTemplates(categoryId, keyword);

            var model = new DocTemplateListViewModel
            {
                Categories = categories,
                Templates = templates,
                SelectedCategoryId = categoryId,
                Keyword = keyword,
                TotalCount = templates.Count
            };

            return View(model);
        }

        /// <summary>
        /// Action thực hiện luồng sử dụng mẫu:
        /// Chọn template -> Tạo tài liệu mới (không sửa template gốc) -> Chuyển vào Editor
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UseTemplate(int id)
        {
            try
            {
                var newDoc = _userDocService.CreateFromTemplate(id, CurrentUserId, authorName: User.Identity?.Name);
                TempData["SuccessMessage"] = "Đã khởi tạo tài liệu mới từ mẫu thành công!";
                return RedirectToAction("Editor", new { id = newDoc.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to create a user document from template {TemplateId}.", id);
                TempData["ErrorMessage"] = "Không thể sử dụng mẫu lúc này. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Màn hình Trình Soạn Thảo Văn Bản Tích Hợp (WYSIWYG Editor)
        /// Cho phép người dùng chỉnh sửa nội dung trong phạm vi trình soạn thảo, lưu và tải DOCX
        /// </summary>
        /// <summary>Lấy tài liệu thuộc người dùng hiện tại và hiển thị trình soạn thảo.</summary>
        public IActionResult Editor(int id)
        {
            var doc = _userDocService.GetUserDocumentById(id, CurrentUserId);
            if (doc == null)
            {
                TempData["ErrorMessage"] = "Tài liệu không tồn tại hoặc đã bị xóa!";
                return RedirectToAction("Index");
            }

            var originalTemplate = doc.TemplateId.HasValue 
                ? _templateService.GetTemplateById(doc.TemplateId.Value) 
                : null;

            var model = new DocEditorViewModel
            {
                Document = doc,
                OriginalTemplate = originalTemplate
            };

            return View(model);
        }

        /// <summary>
        /// Màn hình quản lý các tài liệu cá nhân đã tạo từ mẫu
        /// </summary>
        public IActionResult MyDocuments()
        {
            var docs = _userDocService.GetAllUserDocuments(CurrentUserId);
            return View(docs);
        }

        /// <summary>Lấy mã tài khoản hiện tại từ claim đã xác thực.</summary>
        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
