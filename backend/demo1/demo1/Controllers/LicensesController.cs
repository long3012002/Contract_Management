using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Authorize]
[Route("api/licenses")]
public class LicensesController : CrudControllerBase<LicenseDto, CreateLicenseDto, UpdateLicenseDto>
{
    private readonly ILicenseService _licenseService;

    public LicensesController(ILicenseService service) : base(service)
    {
        _licenseService = service;
    }

    [HttpPost("single")]
    public async Task<ActionResult<LicenseDto>> CreateSingle([FromBody] CreateLicenseDto dto)
    {
        try
        {
            var result = await _licenseService.CreateAsync(dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("by-duan/{duAnId:guid}")]
    public async Task<ActionResult<PagedResult<LicenseDto>>> GetByDuAnId(
        Guid duAnId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _licenseService.GetByDuAnIdAsync(duAnId, search, page, pageSize);
        return Ok(result);
    }

    [HttpGet("expiring")]
    public async Task<ActionResult<IReadOnlyList<LicenseDto>>> GetExpiringLicenses([FromQuery] int? daysThreshold)
    {
        var result = await _licenseService.GetExpiringLicensesAsync(daysThreshold);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<LicenseSummaryDto>> GetLicenseSummary([FromQuery] Guid? duAnId)
    {
        var result = await _licenseService.GetLicenseSummaryAsync(duAnId);
        return Ok(result);
    }
}
