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
    /// API Quản lý Danh mục Đơn vị (Hội sở chính, Chi nhánh, Đơn vị thành viên).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/DanhMuc/don-vi")]
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

        /// <summary>
        /// Lấy tất cả danh sách Đơn vị trong hệ thống.
        /// </summary>
        /// <returns>Danh sách đơn vị</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<DonViDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            if (!await IsAdminAsync()) return Forbid();
            var items = await _donViService.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Lấy chi tiết thông tin Đơn vị theo ID.
        /// </summary>
        /// <param name="id">Mã định danh Đơn vị (GUID)</param>
        /// <returns>Thông tin Đơn vị</returns>
        /// <response code="200">Tìm thấy đơn vị</response>
        /// <response code="404">Không tìm thấy đơn vị</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(DonViDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await IsAdminAsync()) return Forbid();
            var item = await _donViService.GetByIdAsync(id);
            if (item == null) return NotFound("Không tìm thấy đơn vị.");
            return Ok(item);
        }

        /// <summary>
        /// Tạo mới một Đơn vị.
        /// </summary>
        /// <param name="dto">Thông tin đơn vị cần tạo</param>
        /// <returns>Đơn vị vừa tạo</returns>
        /// <response code="200">Tạo đơn vị thành công</response>
        [HttpPost]
        [ProducesResponseType(typeof(DonViDto), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Thêm mới danh sách Đơn vị hàng loạt.
        /// </summary>
        /// <param name="dtos">Danh sách Đơn vị</param>
        /// <returns>Danh sách đơn vị vừa tạo</returns>
        /// <response code="200">Tạo danh sách thành công</response>
        [HttpPost("bulk")]
        [ProducesResponseType(typeof(IEnumerable<DonViDto>), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Cập nhật thông tin Đơn vị theo ID.
        /// </summary>
        /// <param name="id">Mã định danh Đơn vị (GUID)</param>
        /// <param name="dto">Dữ liệu cập nhật</param>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="404">Không tìm thấy đơn vị</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Xóa Đơn vị theo ID.
        /// </summary>
        /// <param name="id">Mã định danh Đơn vị (GUID)</param>
        /// <response code="200">Xóa thành công</response>
        /// <response code="404">Không tìm thấy đơn vị</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!await IsAdminAsync()) return Forbid();
            var success = await _donViService.DeleteAsync(id);
            if (!success) return NotFound("Không tìm thấy đơn vị.");
            return Ok(new { Message = "Xóa đơn vị thành công." });
        }
    }
}
