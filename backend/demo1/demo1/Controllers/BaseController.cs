using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using demo1.Entities;
using demo1.Services;

namespace demo1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseController<TEntity, TResponseDto, TCreateDto, TUpdateDto> : ControllerBase 
        where TEntity : BaseEntity
    {
        protected readonly IBaseService<TEntity, TResponseDto, TCreateDto, TUpdateDto> _service;

        public BaseController(IBaseService<TEntity, TResponseDto, TCreateDto, TUpdateDto> service)
        {
            _service = service;
        }

        [HttpGet]
        public virtual async Task<ActionResult<IEnumerable<TResponseDto>>> GetAll()
        {
            var dtos = await _service.GetAllAsync();
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public virtual async Task<ActionResult<TResponseDto>> GetById(Guid id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
            {
                return NotFound();
            }
            return Ok(dto);
        }

        [HttpPost]
        public virtual async Task<ActionResult<TResponseDto>> Create([FromBody] TCreateDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }
            var createdDto = await _service.CreateAsync(dto);
            var idProperty = createdDto?.GetType().GetProperty("Oid") ?? createdDto?.GetType().GetProperty("Id");
            var idValue = idProperty?.GetValue(createdDto, null);

            return CreatedAtAction(nameof(GetById), new { id = idValue }, createdDto);
        }

        [HttpPut("{id}")]
        public virtual async Task<IActionResult> Update(Guid id, [FromBody] TUpdateDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var existing = await _service.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            await _service.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> Delete(Guid id)
        {
            var existing = await _service.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
