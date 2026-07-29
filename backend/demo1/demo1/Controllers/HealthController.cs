using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using demo1.Services.Interfaces;

namespace demo1.Controllers;

/// <summary>
/// API Kiểm tra Trạng thái Hoạt động của Máy chủ &amp; Kết nối Dịch vụ (Health Check, Thử nghiệm SMTP Email).
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Kiểm tra trạng thái máy chủ Web API.
    /// </summary>
    /// <returns>Trạng thái Healthy và thời gian hệ thống</returns>
    /// <response code="200">Máy chủ đang hoạt động bình thường</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Time = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Thử nghiệm gửi Email qua dịch vụ SMTP.
    /// </summary>
    /// <param name="emailService">Dịch vụ gửi email</param>
    /// <param name="toEmail">Địa chỉ email nhận (Mặc định: quangmd@co-opbank.vn)</param>
    /// <response code="200">Gửi mail thử nghiệm thành công</response>
    [HttpGet("test-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestEmail([FromServices] IEmailService emailService, [FromQuery] string toEmail = "quangmd@co-opbank.vn")
    {
        await emailService.SendEmailAsync(toEmail, "Test Email from Contract Management System", "This is a test email sent from the Co-opBank Contract Management system to verify SMTP configuration.");
        return Ok(new { Message = $"Test email triggered to {toEmail}." });
    }
}
