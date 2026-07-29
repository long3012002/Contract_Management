using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs.DichVu;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Authorize]
[Route("api/dich-vu")]
public class DichVusController : CrudControllerBase<DichVuDto, CreateDichVuDto, UpdateDichVuDto>
{
    private readonly IDichVuService _dichVuService;

    public DichVusController(IDichVuService service) : base(service)
    {
        _dichVuService = service;
    }

    [HttpGet("by-parent/{idParent:guid}")]
    public async Task<ActionResult<IEnumerable<DichVuDto>>> GetByIdParent(Guid idParent)
    {
        var result = await _dichVuService.GetByIdParentAsync(idParent);
        return Ok(result);
    }

    [HttpPost("batch")]
    public async Task<ActionResult<IEnumerable<DichVuDto>>> CreateBatch([FromBody] IEnumerable<CreateDichVuDto> dtos)
    {
        var result = await _dichVuService.CreateRangeAsync(dtos);
        return Ok(result);
    }
}
