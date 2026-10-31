using Microsoft.AspNetCore.Mvc;
using PresentationTemplateSystem.Models.DTO;
using PresentationTemplateSystem.Models.ViewModels;
using PresentationTemplateSystem.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PresentationTemplateSystem.Controllers
{
    [Authorize]
    public class PresentationController : Controller
    {
        private readonly TemplateService _templateService;
        private readonly UserDeckService _userDeckService;
        private readonly ILogger<PresentationController> _logger;

        public PresentationController(
            TemplateService templateService,
            UserDeckService userDeckService,
            ILogger<PresentationController> logger)
        {
            _templateService = templateService;
            _userDeckService = userDeckService;
            _logger = logger;
        }

        [AllowAnonymous]
        /// <summary>Hiển thị thư viện bộ slide theo danh mục và từ khóa tùy chọn.</summary>
        public IActionResult Index(int? categoryId, string? keyword)
        {
            var categories = _templateService.GetCategories();
            var templates = _templateService.GetTemplates(categoryId, keyword);

            var model = new PresentationListViewModel
            {
                Categories = categories,
                Templates = templates,
                SelectedCategoryId = categoryId,
                Keyword = keyword,
                TotalCount = templates.Count
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        /// <summary>Tạo bản trình chiếu riêng từ mẫu và chuyển đến trình soạn thảo.</summary>
        public IActionResult UseTemplate(int id)
        {
            try
            {
                var newDeck = _userDeckService.CreateFromTemplate(id, CurrentUserId, authorName: User.Identity?.Name);
                TempData["SuccessMessage"] = "Đã khởi tạo bản trình chiếu mới từ bộ mẫu thành công!";
                return RedirectToAction("Editor", new { id = newDeck.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to create a user deck from template {TemplateId}.", id);
                TempData["ErrorMessage"] = "Không thể sử dụng mẫu lúc này. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>Tải bản trình chiếu của người dùng và hiển thị trình soạn thảo.</summary>
        public IActionResult Editor(int id)
        {
            var deck = _userDeckService.GetUserDeckById(id, CurrentUserId);
            if (deck == null)
            {
                TempData["ErrorMessage"] = "Bản trình chiếu không tồn tại!";
                return RedirectToAction("Index");
            }

            var originalTemplate = deck.TemplateId.HasValue
                ? _templateService.GetTemplateById(deck.TemplateId.Value)
                : null;

            var slides = new List<SlideItemDto>();
            if (!string.IsNullOrWhiteSpace(deck.DeckDataJson))
            {
                slides = JsonSerializer.Deserialize<List<SlideItemDto>>(deck.DeckDataJson)
                    ?? throw new InvalidDataException($"Slide data for deck {id} is empty.");
            }

            var model = new PresentationEditorViewModel
            {
                Deck = deck,
                OriginalTemplate = originalTemplate,
                Slides = slides
            };

            return View(model);
        }

        /// <summary>Liệt kê các bản trình chiếu thuộc người dùng hiện tại.</summary>
        public IActionResult MyDecks()
        {
            var decks = _userDeckService.GetAllUserDecks(CurrentUserId);
            return View(decks);
        }

        /// <summary>Lấy mã tài khoản hiện tại từ claim đã xác thực.</summary>
        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
