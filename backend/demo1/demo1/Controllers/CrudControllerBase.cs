using System;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Authorize]
[ApiController]
public abstract class CrudControllerBase<TDto, TCreateDto, TUpdateDto> : ControllerBase
    where TDto : IHasId
{
    private readonly ICrudService<TDto, TCreateDto, TUpdateDto> _service;

    protected CrudControllerBase(ICrudService<TDto, TCreateDto, TUpdateDto> service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetAllAsync(search, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TDto>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<IEnumerable<TDto>>> Create([FromBody] IEnumerable<TCreateDto> dtos)
    {
        try
        {
            var result = await _service.CreateRangeAsync(dtos);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TUpdateDto dto)
    {
        try
        {
            var success = await _service.UpdateAsync(id, dto);
            return success ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }
}
