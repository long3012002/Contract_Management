using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs.HangHoa;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Authorize]
[Route("api/hang-hoa")]
public class HangHoasController : CrudControllerBase<HangHoaDto, CreateHangHoaDto, UpdateHangHoaDto>
{
    private readonly IHangHoaService _hangHoaService;

    public HangHoasController(IHangHoaService service) : base(service)
    {
        _hangHoaService = service;
    }

    [HttpGet("by-parent/{idParent:guid}")]
    public async Task<ActionResult<IEnumerable<HangHoaDto>>> GetByIdParent(Guid idParent)
    {
        var result = await _hangHoaService.GetByIdParentAsync(idParent);
        return Ok(result);
    }

    [HttpPost("batch")]
    public async Task<ActionResult<IEnumerable<HangHoaDto>>> CreateBatch([FromBody] IEnumerable<CreateHangHoaDto> dtos)
    {
        var result = await _hangHoaService.CreateRangeAsync(dtos);
        return Ok(result);
    }
}
