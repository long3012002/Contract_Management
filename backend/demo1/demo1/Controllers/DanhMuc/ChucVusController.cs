using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using demo1.DTOs;
using demo1.Services.Interfaces;

namespace demo1.Controllers
{
    /// <summary>
    /// API Quản lý Danh mục Chức vụ (Chuyên viên, Trưởng phòng, Giám đốc, Ban quản lý...).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/DanhMuc/chuc-vu")]
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

        /// <summary>
        /// Lấy tất cả danh sách Chức vụ trong hệ thống.
        /// </summary>
        /// <returns>Danh sách Chức vụ</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        /// <response code="403">Chưa được cấp quyền truy cập danh mục Chức vụ</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ChucVuDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll()
        {
            if (!await IsAdminAsync()) return Forbid();
            var items = await _chucVuService.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một Chức vụ theo ID (GUID).
        /// </summary>
        /// <param name="id">Mã định danh Chức vụ (GUID)</param>
        /// <returns>Thông tin Chức vụ</returns>
        /// <response code="200">Tìm thấy chức vụ</response>
        /// <response code="404">Không tìm thấy chức vụ</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ChucVuDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await IsAdminAsync()) return Forbid();
            var item = await _chucVuService.GetByIdAsync(id);
            if (item == null) return NotFound("Không tìm thấy chức vụ.");
            return Ok(item);
        }

        /// <summary>
        /// Tạo mới một Chức vụ.
        /// </summary>
        /// <param name="dto">Thông tin Chức vụ cần tạo mới</param>
        /// <returns>Thông tin Chức vụ vừa tạo</returns>
        /// <response code="200">Tạo chức vụ thành công</response>
        /// <response code="400">Tên hoặc mã chức vụ không hợp lệ</response>
        [HttpPost]
        [ProducesResponseType(typeof(ChucVuDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Tạo mới danh sách Chức vụ hàng loạt.
        /// </summary>
        /// <param name="dtos">Danh sách Chức vụ</param>
        /// <returns>Danh sách Chức vụ vừa tạo</returns>
        /// <response code="200">Tạo danh sách thành công</response>
        [HttpPost("bulk")]
        [ProducesResponseType(typeof(IEnumerable<ChucVuDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateRange([FromBody] IEnumerable<CreateChucVuDto> dtos)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var result = await _chucVuService.CreateRangeAsync(dtos);
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
        /// Cập nhật thông tin Chức vụ theo ID (GUID).
        /// </summary>
        /// <param name="id">Mã định danh Chức vụ (GUID)</param>
        /// <param name="dto">Thông tin cập nhật</param>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="404">Không tìm thấy chức vụ</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Xóa Chức vụ theo ID (GUID).
        /// </summary>
        /// <param name="id">Mã định danh Chức vụ (GUID)</param>
        /// <response code="200">Xóa chức vụ thành công</response>
        /// <response code="404">Không tìm thấy chức vụ</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!await IsAdminAsync()) return Forbid();
            var success = await _chucVuService.DeleteAsync(id);
            if (!success) return NotFound("Không tìm thấy chức vụ.");
            return Ok(new { Message = "Xóa chức vụ thành công." });
        }
    }
}
