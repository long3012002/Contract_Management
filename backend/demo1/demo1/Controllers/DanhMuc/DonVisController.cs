using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using demo1.DTOs;
using demo1.Services.Interfaces;

namespace demo1.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/don-vi")]
    public class DonVisController : ControllerBase
    {
        private readonly IDonViService _donViService;
        private readonly IAdminService _adminService;

        public DonVisController(IDonViService donViService, IAdminService adminService)
        {
            _donViService = donViService;
            _adminService = adminService;
        }

        private async Task<bool> IsAdminAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            return await _adminService.IsSystemAdminAsync(username);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<DonViDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            if (!await IsAdminAsync()) return Forbid();
            var items = await _donViService.GetAllAsync();
            return Ok(items);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(DonViDto), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await IsAdminAsync()) return Forbid();
            var item = await _donViService.GetByIdAsync(id);
            if (item == null) return NotFound("Không tìm thấy đơn vị.");
            return Ok(item);
        }

        [HttpPost]
        [ProducesResponseType(typeof(DonViDto), 200)]
        public async Task<IActionResult> Create([FromBody] CreateDonViDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var result = await _donViService.CreateAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("bulk")]
        [ProducesResponseType(typeof(IEnumerable<DonViDto>), 200)]
        public async Task<IActionResult> CreateRange([FromBody] IEnumerable<CreateDonViDto> dtos)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var result = await _donViService.CreateRangeAsync(dtos);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDonViDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var success = await _donViService.UpdateAsync(id, dto);
                if (!success) return NotFound("Không tìm thấy đơn vị.");
                return Ok(new { Message = "Cập nhật đơn vị thành công." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!await IsAdminAsync()) return Forbid();
            var success = await _donViService.DeleteAsync(id);
            if (!success) return NotFound("Không tìm thấy đơn vị.");
            return Ok(new { Message = "Xóa đơn vị thành công." });
        }
    }
}
