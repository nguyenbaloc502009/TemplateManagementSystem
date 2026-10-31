using DocumentTemplateSystem.Models.DTO;
using DocumentTemplateSystem.Services;
using TemplateManagement.Core.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTemplateSystem.Controllers.Api
{
    /// <summary>
    /// RESTful API Quản lý Thư viện Mẫu Văn Bản và Tài Liệu Người Dùng
    /// Kế thừa BaseApiController theo kiến trúc chuẩn
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    [AutoValidateAntiforgeryToken]
    public class DocTemplateApiController : BaseApiController
    {
        private readonly TemplateService _templateService;
        private readonly UserDocumentService _userDocService;
        private readonly ILogger<DocTemplateApiController> _logger;

        public DocTemplateApiController(
            TemplateService templateService,
            UserDocumentService userDocService,
            ILogger<DocTemplateApiController> logger)
        {
            _templateService = templateService;
            _userDocService = userDocService;
            _logger = logger;
        }

        /// <summary>
        /// API lấy danh sách mẫu văn bản (hỗ trợ tìm kiếm từ khóa và lọc theo danh mục)
        /// GET: /api/templates?categoryId=1&keyword=nghỉ
        /// </summary>
        [HttpGet("/api/templates")]
        [HttpGet("list")]
        [AllowAnonymous]
        /// <summary>Trả về danh sách mẫu văn bản đã lọc theo danh mục và từ khóa.</summary>
        public IActionResult GetList([FromQuery] int? categoryId, [FromQuery] string? keyword)
        {
            var templates = _templateService.GetTemplates(categoryId, keyword);
            return OkResponse(templates, $"Tìm thấy {templates.Count} mẫu văn bản");
        }

        /// <summary>
        /// API lấy thông tin chi tiết một mẫu văn bản để xem trước
        /// GET: /api/templates/{id}
        /// </summary>
        [HttpGet("/api/templates/{id:int}")]
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        /// <summary>Trả về thông tin mẫu văn bản theo ID để xem trước hoặc sử dụng.</summary>
        public IActionResult GetById(int id)
        {
            var template = _templateService.GetTemplateById(id);
            if (template == null)
            {
                return NotFoundResponse($"Không tìm thấy mẫu văn bản có ID: {id}");
            }
            return OkResponse(template);
        }

        /// <summary>
        /// API thêm mẫu văn bản mới vào thư viện (mở rộng linh hoạt mà không cần sửa giao diện)
        /// POST: /api/templates
        /// </summary>
        [HttpPost("/api/templates")]
        [HttpPost("create-template")]
        /// <summary>Kiểm tra yêu cầu và tạo mẫu văn bản mới trong thư viện.</summary>
        public IActionResult CreateTemplate([FromBody] CreateTemplateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Code))
            {
                return BadRequestResponse("Mã mẫu (Code) và Tên mẫu (Title) không được để trống");
            }

            try
            {
                int newId = _templateService.CreateTemplate(req);
                return OkResponse(new { Id = newId }, "Thêm mẫu văn bản mới vào thư viện thành công!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to create a document template.");
                return InternalErrorResponse("Đã xảy ra lỗi hệ thống.");
            }
        }

        /// <summary>
        /// API tạo một tài liệu mới dựa trên mẫu được chọn (Tạo bản sao độc lập, KHÔNG sửa template gốc)
        /// POST: /api/templates/{id}/documents
        /// </summary>
        [HttpPost("/api/templates/{id:int}/documents")]
        [HttpPost("create-document")]
        /// <summary>Tạo tài liệu riêng của người dùng từ mẫu được chỉ định.</summary>
        public IActionResult CreateDocument([FromBody] CreateDocumentFromTemplateRequest req, [FromRoute] int? id = null)
        {
            int templateId = id ?? req.TemplateId;
            if (templateId <= 0)
            {
                return BadRequestResponse("ID mẫu không hợp lệ");
            }

            try
            {
                var newDoc = _userDocService.CreateFromTemplate(
                    templateId, CurrentUserId, req.CustomTitle, User.Identity?.Name);
                return CreatedResponse(nameof(GetDocumentById), new { id = newDoc.Id }, newDoc, "Khởi tạo tài liệu mới thành công");
            }
            catch (ArgumentException aex)
            {
                _logger.LogInformation(aex, "Document template {TemplateId} was not found.", templateId);
                return NotFoundResponse("Không tìm thấy mẫu văn bản.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to create a document from template {TemplateId}.", templateId);
                return InternalErrorResponse("Đã xảy ra lỗi hệ thống.");
            }
        }

        /// <summary>
        /// API lưu nội dung tài liệu người dùng chỉnh sửa trong editor
        /// PUT: /api/documents/{id}
        /// </summary>
        [HttpPut("/api/documents/{id:int}")]
        [HttpPost("save-document")]
        /// <summary>Lưu nội dung đã chỉnh sửa cho tài liệu thuộc người dùng hiện tại.</summary>
        public IActionResult SaveDocument([FromBody] SaveDocumentRequest req, [FromRoute] int? id = null)
        {
            req.Id = id ?? req.Id;
            if (req.Id <= 0)
            {
                return BadRequestResponse("ID tài liệu không hợp lệ");
            }

            try
            {
                bool success = _userDocService.SaveUserDocument(req, CurrentUserId);
                if (success)
                {
                    return OkResponse(new { req.Id }, "Đã lưu tài liệu thành công vào cơ sở dữ liệu");
                }
                return NotFoundResponse("Không tìm thấy tài liệu cần lưu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to save document {DocumentId}.", req.Id);
                return InternalErrorResponse("Đã xảy ra lỗi hệ thống.");
            }
        }

        /// <summary>
        /// API xuất và tải xuống file Microsoft Word (.docx) thực tế chuẩn OpenXML
        /// GET: /api/documents/{id}/file
        /// </summary>
        [HttpGet("/api/documents/{id:int}/file")]
        [HttpGet("export-docx/{id:int}")]
        /// <summary>Xuất tài liệu DOCX nếu tài liệu thuộc người dùng hiện tại.</summary>
        public IActionResult ExportDocx(int id)
        {
            try
            {
                byte[] bytes = _userDocService.ExportDocx(id, CurrentUserId, out string fileName);
                return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
            }
            catch (ArgumentException aex)
            {
                _logger.LogInformation(aex, "Document {DocumentId} was not found for export.", id);
                return NotFoundResponse("Không tìm thấy tài liệu.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to export document {DocumentId}.", id);
                return InternalErrorResponse("Đã xảy ra lỗi hệ thống.");
            }
        }

        /// <summary>
        /// API xóa tài liệu người dùng
        /// DELETE: /api/documents/{id}
        /// </summary>
        [HttpDelete("/api/documents/{id:int}")]
        [HttpDelete("delete-document/{id:int}")]
        /// <summary>Xóa tài liệu thuộc người dùng hiện tại.</summary>
        public IActionResult DeleteDocument(int id)
        {
            bool success = _userDocService.DeleteUserDocument(id, CurrentUserId);
            if (success)
            {
                return OkResponse<object>(null, "Xóa tài liệu thành công");
            }
            return NotFoundResponse("Không tìm thấy tài liệu cần xóa");
        }

        [HttpGet("/api/documents/{id:int}")]
        /// <summary>Trả về tài liệu theo ID nếu tài liệu thuộc người dùng hiện tại.</summary>
        public IActionResult GetDocumentById(int id)
        {
            var document = _userDocService.GetUserDocumentById(id, CurrentUserId);
            return document == null
                ? NotFoundResponse($"Không tìm thấy tài liệu có ID: {id}")
                : OkResponse(document);
        }
    }
}
