using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Route("api/du-an")]
[FeatureAuthorize("PROJECT")] // Keep PROJECT feature code for authorization purposes
public class DuAnsController : CrudControllerBase<DuAnDto, CreateDuAnDto, UpdateDuAnDto>
{
    private readonly IDuAnService _duAnService;

    public DuAnsController(IDuAnService service) : base(service)
    {
        _duAnService = service;
    }

    [HttpPost("{id}/dieu-chinh")]
    public async Task<ActionResult<DieuChinhDuAnDto>> AdjustBudget(Guid id, [FromBody] CreateDieuChinhDuAnDto dto)
    {
        try
        {
            var result = await _duAnService.AdjustBudgetAsync(id, dto);
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
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/dieu-chinh")]
    public async Task<ActionResult<IReadOnlyList<DieuChinhDuAnDto>>> GetAdjustments(Guid id)
    {
        var result = await _duAnService.GetAdjustmentsAsync(id);
        return Ok(result);
    }
}
