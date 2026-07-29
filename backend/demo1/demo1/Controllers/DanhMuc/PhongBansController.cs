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
    [Route("api/phong-ban")]
    public class PhongBansController : ControllerBase
    {
        private readonly IPhongBanService _phongBanService;
        private readonly IAdminService _adminService;

        public PhongBansController(IPhongBanService phongBanService, IAdminService adminService)
        {
            _phongBanService = phongBanService;
            _adminService = adminService;
        }

        private async Task<bool> IsAdminAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            return await _adminService.IsSystemAdminAsync(username);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PhongBanDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            if (!await IsAdminAsync()) return Forbid();
            var items = await _phongBanService.GetAllAsync();
            return Ok(items);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PhongBanDto), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await IsAdminAsync()) return Forbid();
            var item = await _phongBanService.GetByIdAsync(id);
            if (item == null) return NotFound("Không tìm thấy phòng ban.");
            return Ok(item);
        }

        [HttpPost]
        [ProducesResponseType(typeof(PhongBanDto), 200)]
        public async Task<IActionResult> Create([FromBody] CreatePhongBanDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var result = await _phongBanService.CreateAsync(dto);
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
        [ProducesResponseType(typeof(IEnumerable<PhongBanDto>), 200)]
        public async Task<IActionResult> CreateRange([FromBody] IEnumerable<CreatePhongBanDto> dtos)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var result = await _phongBanService.CreateRangeAsync(dtos);
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
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePhongBanDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var success = await _phongBanService.UpdateAsync(id, dto);
                if (!success) return NotFound("Không tìm thấy phòng ban.");
                return Ok(new { Message = "Cập nhật phòng ban thành công." });
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
            var success = await _phongBanService.DeleteAsync(id);
            if (!success) return NotFound("Không tìm thấy phòng ban.");
            return Ok(new { Message = "Xóa phòng ban thành công." });
        }
    }
}
