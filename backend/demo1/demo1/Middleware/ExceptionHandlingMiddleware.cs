using System.Net;
using demo1.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace demo1.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
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
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, $"Không tìm thấy tài nguyên: {ex.Message}");
            await WriteErrorAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.InnerException is Npgsql.NpgsqlException)
            {
                _logger.LogError(ex, $"Lỗi kết nối cơ sở dữ liệu (Inner): {ex.InnerException.Message}");
                await WriteErrorAsync(context, HttpStatusCode.ServiceUnavailable, "Hệ thống máy chủ dữ liệu hiện không hoạt động hoặc đang bảo trì. Vui lòng quay lại sau.", ex.InnerException.Message);
                return;
            }

            _logger.LogWarning(ex, $"Thao tác xung đột hoặc không hợp lệ: {ex.Message}");
            await WriteErrorAsync(context, HttpStatusCode.Conflict, ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, $"Cập nhật cơ sở dữ liệu thất bại: {ex.Message}");
            
            var friendlyMessage = "Cập nhật dữ liệu thất bại.";
            if (ex.InnerException is Npgsql.PostgresException pgEx)
            {
                if (pgEx.SqlState == "23505") // Unique Violation
                {
                    friendlyMessage = GetFriendlyUniqueConstraintMessage(pgEx);
                }
                else if (pgEx.SqlState == "23503") // Foreign Key Violation
                {
                    friendlyMessage = "Dữ liệu đang được liên kết sử dụng ở chức năng khác, không thể xóa hoặc thay đổi.";
                }
                else if (pgEx.SqlState == "23502") // Not Null Violation
                {
                    friendlyMessage = "Vui lòng nhập đầy đủ các trường thông tin bắt buộc.";
                }
            }
            
            await WriteErrorAsync(context, HttpStatusCode.Conflict, friendlyMessage, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Npgsql.NpgsqlException ex)
        {
            _logger.LogError(ex, $"Lỗi kết nối cơ sở dữ liệu: {ex.Message}");
            await WriteErrorAsync(context, HttpStatusCode.ServiceUnavailable, "Hệ thống máy chủ dữ liệu hiện không hoạt động hoặc đang bảo trì. Vui lòng quay lại sau.", ex.Message);
        }
        catch (Exception ex)
        {
            if (ex.InnerException is Npgsql.NpgsqlException)
            {
                _logger.LogError(ex, $"Lỗi kết nối cơ sở dữ liệu (Inner): {ex.Message}");
                await WriteErrorAsync(context, HttpStatusCode.ServiceUnavailable, "Hệ thống máy chủ dữ liệu hiện không hoạt động hoặc đang bảo trì. Vui lòng quay lại sau.", ex.InnerException.Message);
                return;
            }

            _logger.LogError(ex, $"Lỗi hệ thống không xác định: {ex.Message}");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "Đã có lỗi xảy ra. Vui lòng thử lại", ex.Message);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, HttpStatusCode statusCode, string message, string? detail = null)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var isDevelopment = _env.IsDevelopment();

        await context.Response.WriteAsJsonAsync(new ApiErrorResponse
        {
            Message = message,
            Detail = isDevelopment ? detail : null
        });
    }

    private static readonly System.Text.RegularExpressions.Regex reUniqueKey = 
        new(@"Key \((.*?)\)=\((.*?)\) already exists", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Collections.Generic.Dictionary<string, string> FieldTranslations = new(System.StringComparer.OrdinalIgnoreCase)
    {
        { "MaGoiThau", "Mã gói thầu" },
        { "TenGoiThau", "Tên gói thầu" },
        { "SoHopDong", "Số hợp đồng" },
        { "TenHopDong", "Tên hợp đồng" },
        { "Username", "Tên đăng nhập" },
        { "Email", "Email" },
        { "Phone", "Số điện thoại" },
        { "TenPhongBan", "Tên phòng ban" },
        { "TenToNhom", "Tên tổ nhóm" },
        { "TenChucVu", "Tên chức vụ" },
        { "TenDonVi", "Tên đơn vị" },
        { "MaDuAn", "Mã dự án" },
        { "TenDuAn", "Tên dự án" },
        { "Code", "Mã" },
        { "Name", "Tên" }
    };

    private static string GetFriendlyUniqueConstraintMessage(Npgsql.PostgresException pgEx)
    {
        var constraint = pgEx.ConstraintName ?? "";
        var detail = pgEx.Detail ?? "";

        if (constraint.Contains("GoiThaus", System.StringComparison.OrdinalIgnoreCase) || detail.Contains("MaGoiThau", System.StringComparison.OrdinalIgnoreCase))
        {
            if (constraint.Contains("Ma", System.StringComparison.OrdinalIgnoreCase) || detail.Contains("MaGoiThau", System.StringComparison.OrdinalIgnoreCase))
                return "Mã gói thầu đã tồn tại trong hệ thống.";
            if (constraint.Contains("Ten", System.StringComparison.OrdinalIgnoreCase))
                return "Tên gói thầu đã tồn tại trong hệ thống.";
        }
        if (constraint.Contains("HopDongs", System.StringComparison.OrdinalIgnoreCase) || detail.Contains("SoHopDong", System.StringComparison.OrdinalIgnoreCase))
        {
            if (constraint.Contains("So", System.StringComparison.OrdinalIgnoreCase) || detail.Contains("SoHopDong", System.StringComparison.OrdinalIgnoreCase))
                return "Số hợp đồng đã tồn tại trong hệ thống.";
        }
        if (constraint.Contains("Users", System.StringComparison.OrdinalIgnoreCase) || detail.Contains("Username", System.StringComparison.OrdinalIgnoreCase))
        {
            if (constraint.Contains("Username", System.StringComparison.OrdinalIgnoreCase) || detail.Contains("Username", System.StringComparison.OrdinalIgnoreCase))
                return "Tên tài khoản (username) đã tồn tại trong hệ thống.";
            if (constraint.Contains("Email", System.StringComparison.OrdinalIgnoreCase))
                return "Email đã tồn tại trong hệ thống.";
        }
        if (constraint.Contains("PhongBans", System.StringComparison.OrdinalIgnoreCase) || detail.Contains("TenPhongBan", System.StringComparison.OrdinalIgnoreCase))
        {
            return "Tên phòng ban đã tồn tại trong hệ thống.";
        }
        if (constraint.Contains("ToNhoms", System.StringComparison.OrdinalIgnoreCase) || detail.Contains("TenToNhom", System.StringComparison.OrdinalIgnoreCase))
        {
            return "Tên tổ nhóm đã tồn tại trong hệ thống.";
        }
        if (constraint.Contains("ChucVus", System.StringComparison.OrdinalIgnoreCase) || detail.Contains("TenChucVu", System.StringComparison.OrdinalIgnoreCase))
        {
            return "Tên chức vụ đã tồn tại trong hệ thống.";
        }
        if (constraint.Contains("DonVis", System.StringComparison.OrdinalIgnoreCase) || detail.Contains("TenDonVi", System.StringComparison.OrdinalIgnoreCase))
        {
            return "Tên đơn vị đã tồn tại trong hệ thống.";
        }
        if (constraint.Contains("DuAns", System.StringComparison.OrdinalIgnoreCase) || detail.Contains("MaDuAn", System.StringComparison.OrdinalIgnoreCase))
        {
            if (constraint.Contains("Ma", System.StringComparison.OrdinalIgnoreCase) || detail.Contains("MaDuAn", System.StringComparison.OrdinalIgnoreCase))
                return "Mã dự án đã tồn tại trong hệ thống.";
            if (constraint.Contains("Ten", System.StringComparison.OrdinalIgnoreCase))
                return "Tên dự án đã tồn tại trong hệ thống.";
        }

        var match = reUniqueKey.Match(detail);
        if (match.Success)
        {
            var keyName = match.Groups[1].Value.Trim();
            var keyValue = match.Groups[2].Value.Trim();

            if (FieldTranslations.TryGetValue(keyName, out var translatedKey))
            {
                keyName = translatedKey;
            }

            return $"Dữ liệu '{keyValue}' của trường '{keyName}' đã tồn tại trong hệ thống.";
        }

        return "Dữ liệu bị trùng lặp. Vui lòng kiểm tra lại.";
    }
}
