using System.Threading.Tasks;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Cảnh báo Hợp đồng (Sắp hết hạn, Đã quá hạn, Vượt ngân sách).
/// </summary>
[Authorize]
[FeatureAuthorize("CONTRACT_MANAGEMENT")]
[ApiController]
[Route("api/HeThong/warnings/contracts")]
public class ContractWarningsController(IWarningService service) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách các Hợp đồng sắp hết hạn hiệu lực trong vòng 30 ngày.
    /// </summary>
    /// <returns>Danh sách cảnh báo hợp đồng sắp hết hạn</returns>
    /// <response code="200">Lấy danh sách cảnh báo thành công</response>
    [HttpGet("expiring-soon")]
    [HttpGet("~/api/HeThong/warnings/contracts-expiring-soon")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractsExpiringSoon()
    {
        var result = await service.GetContractsExpiringSoonAsync();
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các Hợp đồng đã quá hạn hiệu lực.
    /// </summary>
    /// <returns>Danh sách hợp đồng quá hạn</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("expired")]
    [HttpGet("~/api/HeThong/warnings/expired-contracts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiredContracts()
    {
        var result = await service.GetExpiredContractsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các Hợp đồng có giá trị vượt quá giá trị gói thầu/ngân sách dự án.
    /// </summary>
    /// <returns>Danh sách hợp đồng vượt ngân sách</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("over-budget")]
    [HttpGet("~/api/HeThong/warnings/over-budget-contracts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverBudgetContracts()
    {
        var result = await service.GetOverBudgetContractsAsync();
        return Ok(result);
    }
}
