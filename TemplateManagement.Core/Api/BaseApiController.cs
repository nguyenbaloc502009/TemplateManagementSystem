using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TemplateManagement.Core.Api
{
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        protected Guid CurrentUserId =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("The authenticated user ID claim is missing."));

        protected IActionResult OkResponse<T>(T? data, string? message = "Thành công") =>
            Ok(ApiResponse<T>.Create(StatusCodes.Status200OK, data, message));

        protected IActionResult CreatedResponse<T>(string actionName, object routeValues, T data, string? message = "Đã tạo tài nguyên") =>
            CreatedAtAction(actionName, routeValues,
                ApiResponse<T>.Create(StatusCodes.Status201Created, data, message));

        /// <summary>Tạo phản hồi API chuẩn cho yêu cầu không hợp lệ.</summary>
        protected IActionResult BadRequestResponse(string message) =>
            BadRequest(ApiResponse<object>.Create(StatusCodes.Status400BadRequest, null, message));

        /// <summary>Tạo phản hồi API chuẩn khi không tìm thấy tài nguyên.</summary>
        protected IActionResult NotFoundResponse(string message) =>
            NotFound(ApiResponse<object>.Create(StatusCodes.Status404NotFound, null, message));

        /// <summary>Tạo phản hồi API chuẩn cho lỗi nội bộ đã được xử lý.</summary>
        protected IActionResult InternalErrorResponse(string message) =>
            StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Create(StatusCodes.Status500InternalServerError, null, message));
    }
}
