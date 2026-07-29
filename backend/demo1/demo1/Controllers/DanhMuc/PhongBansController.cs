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
    /// API Quản lý Danh mục Phòng ban (Phòng CNTT, Phòng Kế toán, Phòng Dự án...).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/DanhMuc/phong-ban")]
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

        /// <summary>
        /// Lấy tất cả danh sách Phòng ban trong hệ thống.
        /// </summary>
        /// <returns>Danh sách phòng ban</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PhongBanDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            if (!await IsAdminAsync()) return Forbid();
            var items = await _phongBanService.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Lấy chi tiết thông tin Phòng ban theo ID.
        /// </summary>
        /// <param name="id">Mã định danh Phòng ban (GUID)</param>
        /// <returns>Thông tin phòng ban</returns>
        /// <response code="200">Tìm thấy phòng ban</response>
        /// <response code="404">Không tìm thấy phòng ban</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PhongBanDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await IsAdminAsync()) return Forbid();
            var item = await _phongBanService.GetByIdAsync(id);
            if (item == null) return NotFound("Không tìm thấy phòng ban.");
            return Ok(item);
        }

        /// <summary>
        /// Tạo mới một Phòng ban.
        /// </summary>
        /// <param name="dto">Thông tin phòng ban cần tạo</param>
        /// <returns>Phòng ban vừa tạo</returns>
        /// <response code="200">Tạo phòng ban thành công</response>
        [HttpPost]
        [ProducesResponseType(typeof(PhongBanDto), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Thêm mới danh sách Phòng ban hàng loạt.
        /// </summary>
        /// <param name="dtos">Danh sách phòng ban</param>
        /// <returns>Danh sách phòng ban vừa tạo</returns>
        /// <response code="200">Tạo danh sách thành công</response>
        [HttpPost("bulk")]
        [ProducesResponseType(typeof(IEnumerable<PhongBanDto>), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Cập nhật thông tin Phòng ban theo ID.
        /// </summary>
        /// <param name="id">Mã định danh Phòng ban (GUID)</param>
        /// <param name="dto">Dữ liệu cập nhật</param>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="404">Không tìm thấy phòng ban</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Xóa Phòng ban theo ID.
        /// </summary>
        /// <param name="id">Mã định danh Phòng ban (GUID)</param>
        /// <response code="200">Xóa thành công</response>
        /// <response code="404">Không tìm thấy phòng ban</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!await IsAdminAsync()) return Forbid();
            var success = await _phongBanService.DeleteAsync(id);
            if (!success) return NotFound("Không tìm thấy phòng ban.");
            return Ok(new { Message = "Xóa phòng ban thành công." });
        }
    }
}
