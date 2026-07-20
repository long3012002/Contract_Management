using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Route("api/cong-viec-goi-thau")]
[FeatureAuthorize("BID_PACKAGE")]
public class CongViecGoiThausController : CrudControllerBase<CongViecGoiThauDto, CreateCongViecGoiThauDto, UpdateCongViecGoiThauDto>
{
    private readonly ICongViecGoiThauService _congViecGoiThauService;

    public CongViecGoiThausController(ICongViecGoiThauService service) : base(service)
    {
        _congViecGoiThauService = service;
    }

    [HttpGet("by-goi-thau/{idGoiThau:guid}")]
    public async Task<ActionResult<IEnumerable<CongViecGoiThauDto>>> GetByGoiThauId(Guid idGoiThau)
    {
        var result = await _congViecGoiThauService.GetByParentIdAsync(idGoiThau);
        return Ok(result);
    }

    [HttpGet("by-goi-thau/{idGoiThau:guid}/paged")]
    public async Task<ActionResult<PagedResult<CongViecGoiThauDto>>> GetByGoiThauIdPaged(
        Guid idGoiThau,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? cursor = null)
    {
        var result = await _congViecGoiThauService.GetByParentIdPagedAsync(idGoiThau, search, page, pageSize, cursor);
        return Ok(result);
    }

    [HttpDelete("by-goi-thau/{idGoiThau:guid}")]
    public async Task<IActionResult> DeleteByGoiThauId(Guid idGoiThau)
    {
        var success = await _congViecGoiThauService.DeleteByParentIdAsync(idGoiThau);
        return success ? NoContent() : NotFound(new { message = $"Không tìm thấy công việc nào cho gói thầu '{idGoiThau}'." });
    }

    [HttpGet("by-goi-thau/{idGoiThau:guid}/report")]
    public async Task<ActionResult<CongViecGoiThauReportDto>> GetReport(Guid idGoiThau)
    {
        var report = await _congViecGoiThauService.GetReportByGoiThauIdAsync(idGoiThau);
        return Ok(report);
    }
}
