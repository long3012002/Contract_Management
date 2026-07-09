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
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, "Yêu cầu không hợp lệ.", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.NotFound, "Không tìm thấy tài nguyên.", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.Conflict, "Thao tác xung đột hoặc không hợp lệ.", ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Cập nhật cơ sở dữ liệu thất bại.");
            await WriteErrorAsync(context, HttpStatusCode.Conflict, "Cập nhật dữ liệu thất bại.", "Vui lòng kiểm tra lại dữ liệu trùng lặp hoặc các ràng buộc liên quan.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi hệ thống không xác định.");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "Lỗi hệ thống nội bộ.");
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
