using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using demo1.DTOs;
using demo1.DTOs.Permission;
using demo1.Services.Interfaces;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using demo1.Data;

namespace demo1.Controllers
{
    /// <summary>
    /// API Quản lý Yêu cầu Cấp quyền (Gửi yêu cầu xin quyền, Xem danh sách yêu cầu cá nhân, Quản trị phê duyệt/từ chối yêu cầu).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/NghiepVu/permission-requests")]
    public class PermissionRequestsController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        private readonly AppDbContext _context;

        public PermissionRequestsController(IPermissionService permissionService, AppDbContext context)
        {
            _permissionService = permissionService;
            _context = context;
        }

        private async Task<Guid?> GetCurrentUserIdAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;
            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            return dbUser?.Id;
        }

        private async Task<bool> IsAdminAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            return user?.IsSystemAdmin ?? false;
        }

        /// <summary>
        /// Tạo mới một Yêu cầu Cấp quyền bổ sung trên tính năng / bản ghi.
        /// </summary>
        /// <param name="dto">Thông tin yêu cầu xin cấp quyền</param>
        /// <returns>Thông tin yêu cầu cấp quyền vừa tạo</returns>
        /// <response code="200">Gửi yêu cầu thành công</response>
        /// <response code="400">Yêu cầu không hợp lệ</response>
        [HttpPost]
        [ProducesResponseType(typeof(PermissionRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRequest([FromBody] CreatePermissionRequestDto dto)
        {
            var userId = await GetCurrentUserIdAsync();
            if (!userId.HasValue) return Unauthorized();

            try
            {
                var result = await _permissionService.CreateRequestAsync(userId.Value, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách các Yêu cầu Cấp quyền do chính Người dùng hiện tại đã gửi.
        /// </summary>
        /// <returns>Danh sách các yêu cầu của tôi</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet("my-requests")]
        [ProducesResponseType(typeof(IEnumerable<PermissionRequestDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = await GetCurrentUserIdAsync();
            if (!userId.HasValue) return Unauthorized();

            var requests = await _permissionService.GetUserRequestsAsync(userId.Value);
            return Ok(requests);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách Yêu cầu Cấp quyền cho Quản trị viên (Phân trang và lọc theo trạng thái/tìm kiếm).
        /// </summary>
        /// <param name="status">Trạng thái yêu cầu (PENDING, APPROVED, REJECTED)</param>
        /// <param name="search">Từ khóa tìm kiếm</param>
        /// <param name="page">Trang hiện tại (Mặc định: 1)</param>
        /// <param name="pageSize">Số lượng bản ghi (Mặc định: 20)</param>
        /// <returns>Danh sách yêu cầu phân trang</returns>
        /// <response code="200">Lấy danh sách cho admin thành công</response>
        /// <response code="403">Chưa phân quyền quản trị</response>
        [HttpGet("admin")]
        [ProducesResponseType(typeof(PagedResult<PermissionRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllRequests(
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Unauthorized();

            if (!user.IsSystemAdmin && !user.IdChucVu.HasValue)
            {
                return Forbid();
            }

            var result = await _permissionService.GetAllRequestsAsync(status, search, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Phê duyệt hoặc Từ chối một Yêu cầu Cấp quyền.
        /// </summary>
        /// <param name="id">Mã định danh Yêu cầu Cấp quyền (GUID)</param>
        /// <param name="dto">Trạng thái phê duyệt và lý do/ghi chú</param>
        /// <response code="200">Xử lý yêu cầu thành công</response>
        /// <response code="404">Không tìm thấy yêu cầu</response>
        [HttpPost("{id:guid}/review")]
        [ProducesResponseType(typeof(PermissionRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReviewRequest(Guid id, [FromBody] ReviewPermissionRequestDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();

            var reviewerId = await GetCurrentUserIdAsync();
            if (!reviewerId.HasValue) return Unauthorized();

            try
            {
                var result = await _permissionService.ReviewRequestAsync(id, reviewerId.Value, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
