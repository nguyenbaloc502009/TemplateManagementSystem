using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TemplateManagement.Core.Api;

namespace TemplateManagement.Core.Web
{
    public static class SafeExceptionHandlingExtensions
    {
        /// <summary>Đăng ký middleware trả lỗi an toàn mà không để lộ chi tiết nội bộ.</summary>
        public static void UseSafeExceptionHandling(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
            {
                Exception? exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception == null)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    return;
                }

                int statusCode = exception is ArgumentException or BadHttpRequestException
                    ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status500InternalServerError;
                ILogger logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("UnhandledRequestException");
                if (statusCode == StatusCodes.Status500InternalServerError)
                {
                    logger.LogError(exception, "Unhandled request failure. TraceId: {TraceId}", context.TraceIdentifier);
                }
                else
                {
                    logger.LogWarning(exception, "Invalid request. TraceId: {TraceId}", context.TraceIdentifier);
                }

                context.Response.Clear();
                context.Response.StatusCode = statusCode;
                string message = statusCode == StatusCodes.Status400BadRequest
                    ? "Yêu cầu không hợp lệ."
                    : "Đã xảy ra lỗi hệ thống.";
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await JsonSerializer.SerializeAsync(
                        context.Response.Body,
                        ApiResponse<object>.Create(statusCode, null, message),
                        new JsonSerializerOptions(JsonSerializerDefaults.Web),
                        context.RequestAborted);
                }
                else
                {
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    await context.Response.WriteAsync(message, context.RequestAborted);
                }
            }));
        }
    }
}
