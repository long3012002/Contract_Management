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
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, "Invalid request.", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.Conflict, "Operation conflict.", ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update failed.");
            await WriteErrorAsync(context, HttpStatusCode.Conflict, "Database update failed.", "Please check duplicated or related data.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled API error.");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "Internal server error.");
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
