using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PresentationTemplateSystem.Models.DTO;
using PresentationTemplateSystem.Services;
using TemplateManagement.Core.Api;

namespace PresentationTemplateSystem.Controllers.Api
{
    [Route("api/[controller]")]
    [Authorize]
    [AutoValidateAntiforgeryToken]
    public class PresentationApiController : BaseApiController
    {
        private readonly TemplateService _templateService;
        private readonly UserDeckService _userDeckService;
        private readonly ILogger<PresentationApiController> _logger;

        public PresentationApiController(
            TemplateService templateService,
            UserDeckService userDeckService,
            ILogger<PresentationApiController> logger)
        {
            _templateService = templateService;
            _userDeckService = userDeckService;
            _logger = logger;
        }

        [HttpGet("/api/presentations/templates")]
        [HttpGet("list")]
        [AllowAnonymous]
        /// <summary>Trả về danh sách bộ slide đã lọc theo danh mục và từ khóa.</summary>
        public IActionResult GetList([FromQuery] int? categoryId, [FromQuery] string? keyword)
        {
            var templates = _templateService.GetTemplates(categoryId, keyword);
            return OkResponse(templates, $"Tìm thấy {templates.Count} bộ template trình chiếu");
        }

        [HttpGet("/api/presentations/templates/{id:int}")]
        [AllowAnonymous]
        [HttpGet("{id:int}")]
        /// <summary>Trả về thông tin bộ slide cùng các slide chi tiết theo ID.</summary>
        public IActionResult GetById(int id)
        {
            var template = _templateService.GetTemplateById(id);
            if (template == null)
            {
                return NotFoundResponse($"Không tìm thấy bộ template có ID: {id}");
            }
            return OkResponse(template);
        }

        [HttpPost("/api/presentations/templates")]
        [HttpPost("create-template")]
        /// <summary>Kiểm tra yêu cầu và tạo bộ mẫu trình chiếu mới.</summary>
        public IActionResult CreateTemplate([FromBody] CreatePresentationTemplateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Code))
            {
                return BadRequestResponse("Mã (Code) và Tiêu đề (Title) không được để trống");
            }
            if (req.Slides == null || req.Slides.Count == 0)
            {
                return BadRequestResponse("Bộ template phải có ít nhất một slide");
            }

            try
            {
                int newId = _templateService.CreateTemplate(req);
                return OkResponse(new { Id = newId }, "Thêm bộ template trình chiếu thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to create a presentation template.");
                return InternalErrorResponse("Đã xảy ra lỗi hệ thống.");
            }
        }

        [HttpPost("/api/presentations/templates/{id:int}/decks")]
        [HttpPost("create-deck")]
        /// <summary>Tạo bản trình chiếu riêng của người dùng từ bộ mẫu được chọn.</summary>
        public IActionResult CreateDeck([FromBody] CreateDeckFromTemplateRequest req, [FromRoute] int? id = null)
        {
            int templateId = id ?? req.TemplateId;
            if (templateId <= 0)
            {
                return BadRequestResponse("ID template không hợp lệ");
            }

            try
            {
                var newDeck = _userDeckService.CreateFromTemplate(
                    templateId, CurrentUserId, req.CustomTitle, User.Identity?.Name);
                return CreatedResponse(nameof(GetDeckById), new { id = newDeck.Id }, newDeck, "Khởi tạo bản trình chiếu độc lập thành công");
            }
            catch (ArgumentException aex)
            {
                _logger.LogInformation(aex, "Presentation template {TemplateId} was not found.", templateId);
                return NotFoundResponse("Không tìm thấy bộ mẫu trình chiếu.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to create a deck from template {TemplateId}.", templateId);
                return InternalErrorResponse("Đã xảy ra lỗi hệ thống.");
            }
        }

        [HttpPut("/api/presentations/decks/{id:int}")]
        [HttpPost("save-deck")]
        /// <summary>Lưu nội dung slide đã chỉnh sửa cho deck của người dùng hiện tại.</summary>
        public IActionResult SaveDeck([FromBody] SaveDeckRequest req, [FromRoute] int? id = null)
        {
            req.Id = id ?? req.Id;
            if (req.Id <= 0)
            {
                return BadRequestResponse("ID bản trình chiếu không hợp lệ");
            }

            try
            {
                bool success = _userDeckService.SaveUserDeck(req, CurrentUserId);
                if (success)
                {
                    return OkResponse(new { req.Id }, "Đã lưu bản trình chiếu thành công");
                }
                return NotFoundResponse("Không tìm thấy bản trình chiếu cần lưu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to save presentation deck {DeckId}.", req.Id);
                return InternalErrorResponse("Đã xảy ra lỗi hệ thống.");
            }
        }

        [HttpGet("/api/presentations/decks/{id:int}/file")]
        [HttpGet("export-pptx/{id:int}")]
        /// <summary>Xuất file PPTX cho deck thuộc người dùng hiện tại.</summary>
        public IActionResult ExportPptx(int id)
        {
            try
            {
                byte[] bytes = _userDeckService.ExportPptx(id, CurrentUserId, out string fileName);
                return File(bytes, "application/vnd.openxmlformats-officedocument.presentationml.presentation", fileName);
            }
            catch (ArgumentException aex)
            {
                _logger.LogInformation(aex, "Presentation deck {DeckId} was not found for export.", id);
                return NotFoundResponse("Không tìm thấy bản trình chiếu.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to export presentation deck {DeckId}.", id);
                return InternalErrorResponse("Đã xảy ra lỗi hệ thống.");
            }
        }

        [HttpDelete("/api/presentations/decks/{id:int}")]
        [HttpDelete("delete-deck/{id:int}")]
        /// <summary>Xóa deck thuộc người dùng hiện tại.</summary>
        public IActionResult DeleteDeck(int id)
        {
            bool success = _userDeckService.DeleteUserDeck(id, CurrentUserId);
            if (success)
            {
                return OkResponse<object>(null, "Đã xóa bản trình chiếu");
            }
            return NotFoundResponse("Không tìm thấy bản trình chiếu cần xóa");
        }

        [HttpGet("/api/presentations/decks/{id:int}")]
        /// <summary>Trả về deck theo ID nếu deck thuộc người dùng hiện tại.</summary>
        public IActionResult GetDeckById(int id)
        {
            var deck = _userDeckService.GetUserDeckById(id, CurrentUserId);
            return deck == null
                ? NotFoundResponse($"Không tìm thấy bản trình chiếu có ID: {id}")
                : OkResponse(deck);
        }
    }
}
