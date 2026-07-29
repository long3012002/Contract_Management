using System.Threading.Tasks;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Cảnh báo Hệ thống (Cảnh báo Hợp đồng sắp hết hạn/Đã hết hạn/Vượt ngân sách, Cảnh báo License phần mềm sắp hết hạn/Đã hết hạn).
/// </summary>
[Authorize]
[FeatureAuthorize("CONTRACT_MANAGEMENT")]
[ApiController]
[Route("api/HeThong/warnings")]
public class WarningsController : ControllerBase
{
    private readonly IWarningService _service;

    public WarningsController(IWarningService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lấy danh sách các Hợp đồng sắp hết hạn hiệu lực trong vòng 30 ngày.
    /// </summary>
    /// <returns>Danh sách cảnh báo hợp đồng sắp hết hạn</returns>
    /// <response code="200">Lấy danh sách cảnh báo thành công</response>
    [HttpGet("contracts-expiring-soon")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractsExpiringSoon()
    {
        var result = await _service.GetContractsExpiringSoonAsync();
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các Hợp đồng đã quá hạn hiệu lực.
    /// </summary>
    /// <returns>Danh sách hợp đồng quá hạn</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("expired-contracts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiredContracts()
    {
        var result = await _service.GetExpiredContractsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các Hợp đồng có giá trị vượt quá giá trị gói thầu/ngân sách dự án.
    /// </summary>
    /// <returns>Danh sách hợp đồng vượt ngân sách</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("over-budget-contracts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverBudgetContracts()
    {
        var result = await _service.GetOverBudgetContractsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các License phần mềm sắp hết hạn trong 30 ngày.
    /// </summary>
    /// <returns>Danh sách License sắp hết hạn</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("licenses-expiring-soon")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLicensesExpiringSoon()
    {
        var result = await _service.GetLicensesExpiringSoonAsync();
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các License phần mềm đã hết hạn.
    /// </summary>
    /// <returns>Danh sách License đã hết hạn</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("expired-licenses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiredLicenses()
    {
        var result = await _service.GetExpiredLicensesAsync();
        return Ok(result);
    }
}
