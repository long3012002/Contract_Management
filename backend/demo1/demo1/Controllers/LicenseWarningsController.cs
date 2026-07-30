using System.Threading.Tasks;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Cảnh báo License Phần mềm (Sắp hết hạn, Đã hết hạn).
/// </summary>
[Authorize]
[FeatureAuthorize("CONTRACT_MANAGEMENT")]
[ApiController]
[Route("api/HeThong/warnings/licenses")]
public class LicenseWarningsController(IWarningService service) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách các License phần mềm sắp hết hạn trong 30 ngày.
    /// </summary>
    /// <returns>Danh sách License sắp hết hạn</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("expiring-soon")]
    [HttpGet("~/api/HeThong/warnings/licenses-expiring-soon")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLicensesExpiringSoon()
    {
        var result = await service.GetLicensesExpiringSoonAsync();
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các License phần mềm đã hết hạn.
    /// </summary>
    /// <returns>Danh sách License đã hết hạn</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("expired")]
    [HttpGet("~/api/HeThong/warnings/expired-licenses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiredLicenses()
    {
        var result = await service.GetExpiredLicensesAsync();
        return Ok(result);
    }
}
