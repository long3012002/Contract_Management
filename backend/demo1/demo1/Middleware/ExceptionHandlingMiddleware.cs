using System.Net;
using demo1.DTOs;
using Microsoft.EntityFrameworkCore;

namespace demo1.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, $"Yêu cầu không hợp lệ: {ex.Message}");
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, "Yêu cầu không hợp lệ.");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, $"Không tìm thấy tài nguyên: {ex.Message}");
            await WriteErrorAsync(context, HttpStatusCode.NotFound, "Không tìm thấy tài nguyên.");
        }
        catch (InvalidOperationException ex)
        {
            if (ex.InnerException is MySqlConnector.MySqlException)
            {
                _logger.LogError(ex, $"Lỗi kết nối cơ sở dữ liệu (Inner): {ex.InnerException.Message}");
                await WriteErrorAsync(context, HttpStatusCode.ServiceUnavailable, "Hệ thống máy chủ dữ liệu hiện không hoạt động hoặc đang bảo trì. Vui lòng quay lại sau.");
                return;
            }

            _logger.LogWarning(ex, $"Thao tác xung đột hoặc không hợp lệ: {ex.Message}");
            await WriteErrorAsync(context, HttpStatusCode.Conflict, ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, $"Cập nhật cơ sở dữ liệu thất bại: {ex.Message}");
            await WriteErrorAsync(context, HttpStatusCode.Conflict, "Cập nhật dữ liệu thất bại.", "Vui lòng kiểm tra lại dữ liệu trùng lặp hoặc các ràng buộc liên quan.");
        }
        catch (MySqlConnector.MySqlException ex)
        {
            _logger.LogError(ex, $"Lỗi kết nối cơ sở dữ liệu: {ex.Message}");
            await WriteErrorAsync(context, HttpStatusCode.ServiceUnavailable, "Hệ thống máy chủ dữ liệu hiện không hoạt động hoặc đang bảo trì. Vui lòng quay lại sau.");
        }
        catch (Exception ex)
        {
            if (ex.InnerException is MySqlConnector.MySqlException)
            {
                _logger.LogError(ex, $"Lỗi kết nối cơ sở dữ liệu (Inner): {ex.Message}");
                await WriteErrorAsync(context, HttpStatusCode.ServiceUnavailable, "Hệ thống máy chủ dữ liệu hiện không hoạt động hoặc đang bảo trì. Vui lòng quay lại sau.");
                return;
            }

            _logger.LogError(ex, $"Lỗi hệ thống không xác định: {ex.Message}");
            // await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "Lỗi hệ thống nội bộ.");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "Đã có lỗi xảy ra. Vui lòng thử lại");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, HttpStatusCode statusCode, string message, string? detail = null)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new ApiErrorResponse
        {
            Message = message,
            Detail = detail
        });
    }
}
