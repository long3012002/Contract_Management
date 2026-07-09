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
    [Route("api/chuc-vu")]
    public class ChucVusController : ControllerBase
    {
        private readonly IChucVuService _chucVuService;
        private readonly IAdminService _adminService;

        public ChucVusController(IChucVuService chucVuService, IAdminService adminService)
        {
            _chucVuService = chucVuService;
            _adminService = adminService;
        }

        private async Task<bool> IsAdminAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            return await _adminService.IsSystemAdminAsync(username);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ChucVuDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            if (!await IsAdminAsync()) return Forbid();
            var items = await _chucVuService.GetAllAsync();
            return Ok(items);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ChucVuDto), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await IsAdminAsync()) return Forbid();
            var item = await _chucVuService.GetByIdAsync(id);
            if (item == null) return NotFound("Không tìm thấy chức vụ.");
            return Ok(item);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ChucVuDto), 200)]
        public async Task<IActionResult> Create([FromBody] CreateChucVuDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var result = await _chucVuService.CreateAsync(dto);
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
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateChucVuDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var success = await _chucVuService.UpdateAsync(id, dto);
                if (!success) return NotFound("Không tìm thấy chức vụ.");
                return Ok(new { Message = "Cập nhật chức vụ thành công." });
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
            var success = await _chucVuService.DeleteAsync(id);
            if (!success) return NotFound("Không tìm thấy chức vụ.");
            return Ok(new { Message = "Xóa chức vụ thành công." });
        }

        [HttpGet("{id:guid}/permissions")]
        [ProducesResponseType(typeof(IEnumerable<ChucVuPermissionDto>), 200)]
        public async Task<IActionResult> GetPermissions(Guid id)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var result = await _chucVuService.GetPermissionsAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [HttpPut("{id:guid}/permissions")]
        public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] List<UpdateChucVuPermissionDto> permissions)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                await _chucVuService.UpdatePermissionsAsync(id, permissions);
                return Ok(new { Message = "Cập nhật phân quyền chức vụ thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }
    }
}
