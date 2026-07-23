using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using demo1.Services.Interfaces;

namespace demo1.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Time = DateTime.UtcNow
        });
    }

    [HttpGet("test-email")]
    public async Task<IActionResult> TestEmail([FromServices] IEmailService emailService, [FromQuery] string toEmail = "quangmd@co-opbank.vn")
    {
        await emailService.SendEmailAsync(toEmail, "Test Email from Contract Management System", "This is a test email sent from the Co-opBank Contract Management system to verify SMTP configuration.");
        return Ok(new { Message = $"Test email triggered to {toEmail}." });
    }
}
