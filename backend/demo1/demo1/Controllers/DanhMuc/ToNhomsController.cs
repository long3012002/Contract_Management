using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace demo1.Controllers
{
    /// <summary>
    /// API Quản lý Danh mục Tổ nhóm thuộc phòng ban.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/DanhMuc/to-nhom")]
    public class ToNhomsController : ControllerBase
    {
        private readonly IToNhomService _toNhomService;
        private readonly IAdminService _adminService;

        public ToNhomsController(IToNhomService toNhomService, IAdminService adminService)
        {
            _toNhomService = toNhomService;
            _adminService = adminService;
        }

        private async Task<bool> IsAdminAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            return await _adminService.IsSystemAdminAsync(username);
        }

        /// <summary>
        /// Lấy tất cả danh sách Tổ nhóm trong hệ thống.
        /// </summary>
        /// <returns>Danh sách tổ nhóm</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ToNhomDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            if (!await IsAdminAsync()) return Forbid();
            var items = await _toNhomService.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Lấy chi tiết thông tin Tổ nhóm theo ID.
        /// </summary>
        /// <param name="id">Mã định danh Tổ nhóm (GUID)</param>
        /// <returns>Thông tin tổ nhóm</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ToNhomDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await IsAdminAsync()) return Forbid();
            var item = await _toNhomService.GetByIdAsync(id);
            if (item == null) return NotFound("Không tìm thấy tổ nhóm.");
            return Ok(item);
        }

        /// <summary>
        /// Tạo mới một Tổ nhóm.
        /// </summary>
        /// <param name="dto">Thông tin tổ nhóm cần tạo</param>
        /// <returns>Tổ nhóm vừa tạo</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ToNhomDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateToNhomDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var result = await _toNhomService.CreateAsync(dto);
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

        /// <summary>
        /// Thêm mới danh sách Tổ nhóm hàng loạt.
        /// </summary>
        /// <param name="dtos">Danh sách tổ nhóm</param>
        /// <returns>Danh sách tổ nhóm vừa tạo</returns>
        [HttpPost("bulk")]
        [ProducesResponseType(typeof(IEnumerable<ToNhomDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateRange([FromBody] IEnumerable<CreateToNhomDto> dtos)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var result = await _toNhomService.CreateRangeAsync(dtos);
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

        /// <summary>
        /// Cập nhật thông tin Tổ nhóm theo ID.
        /// </summary>
        /// <param name="id">Mã định danh Tổ nhóm (GUID)</param>
        /// <param name="dto">Dữ liệu cập nhật</param>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateToNhomDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var success = await _toNhomService.UpdateAsync(id, dto);
                if (!success) return NotFound("Không tìm thấy tổ nhóm.");
                return Ok(new { Message = "Cập nhật tổ nhóm thành công." });
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

        /// <summary>
        /// Xóa Tổ nhóm theo ID.
        /// </summary>
        /// <param name="id">Mã định danh Tổ nhóm (GUID)</param>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!await IsAdminAsync()) return Forbid();
            var success = await _toNhomService.DeleteAsync(id);
            if (!success) return NotFound("Không tìm thấy tổ nhóm.");
            return Ok(new { Message = "Xóa tổ nhóm thành công." });
        }
    }
}
