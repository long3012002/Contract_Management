using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using demo1.DTOs.Permission;
using demo1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using demo1.Data;

namespace demo1.Controllers
{
    /// <summary>
    /// API Quản lý Quyền Người dùng Chi tiết (Phân quyền theo Đối tượng / Dự án, Tra cứu Catalog quyền, Trực tiếp Cấp quyền &amp; Thu hồi quyền).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/NghiepVu/user-permissions")]
    public class UserPermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        private readonly AppDbContext _context;

        public UserPermissionsController(IPermissionService permissionService, AppDbContext context)
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
        /// Lấy danh sách Quyền của Người dùng (Lọc theo ID Người dùng và Mã tính năng).
        /// </summary>
        /// <param name="userId">Mã định danh Người dùng (GUID, tùy chọn)</param>
        /// <param name="featureCode">Mã tính năng hệ thống (tùy chọn)</param>
        /// <returns>Danh sách quyền chi tiết của người dùng</returns>
        /// <response code="200">Lấy danh sách quyền thành công</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserPermissionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserPermissions([FromQuery] Guid? userId, [FromQuery] string? featureCode)
        {
            var permissions = await _permissionService.GetUserPermissionsAsync(userId, featureCode);
            return Ok(permissions);
        }

        /// <summary>
        /// Lấy Danh mục Catalog tất cả các loại Quyền hệ thống hiện có.
        /// </summary>
        /// <returns>Danh mục Catalog phân quyền</returns>
        /// <response code="200">Lấy catalog thành công</response>
        [HttpGet("catalog")]
        [ProducesResponseType(typeof(IEnumerable<PermissionCatalogDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCatalog()
        {
            var catalog = await _permissionService.GetPermissionCatalogAsync();
            return Ok(catalog);
        }

        /// <summary>
        /// Quản trị viên cấp trực tiếp Quyền đặc thù cho Người dùng.
        /// </summary>
        /// <param name="dto">Dữ liệu thông tin phân quyền cho người dùng</param>
        /// <returns>Thông tin quyền vừa được cấp</returns>
        /// <response code="200">Cấp quyền thành công</response>
        /// <response code="403">Yêu cầu quyền Quản trị hệ thống</response>
        /// <response code="404">Không tìm thấy người dùng hoặc tính năng</response>
        [HttpPost]
        [ProducesResponseType(typeof(UserPermissionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GrantPermission([FromBody] CreateUserPermissionDto dto)
        {
            var isSystemAdmin = await IsAdminAsync();
            var adminId = await GetCurrentUserIdAsync();
            if (!adminId.HasValue) return Unauthorized();

            if (!isSystemAdmin)
            {
                if (dto.DuAnId.HasValue)
                {
                    var project = await _context.DuAns.AsNoTracking().FirstOrDefaultAsync(da => da.Id == dto.DuAnId.Value);
                    if (project == null || project.CreatedByUserId != adminId.Value)
                    {
                        return Forbid();
                    }
                }
                else
                {
                    return Forbid();
                }
            }

            try
            {
                var result = await _permissionService.GrantUserPermissionAsync(adminId.Value, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Quản trị viên hoặc Chủ dự án cấp quyền đặc thù cho nhiều Người dùng cùng lúc.
        /// </summary>
        /// <param name="dto">Dữ liệu thông tin phân quyền theo lô cho danh sách người dùng</param>
        /// <returns>Danh sách thông tin quyền vừa được cấp</returns>
        /// <response code="200">Cấp quyền thành công</response>
        /// <response code="403">Yêu cầu quyền Quản trị hoặc Chủ dự án</response>
        /// <response code="404">Không tìm thấy người dùng hoặc tính năng</response>
        [HttpPost("batch")]
        [ProducesResponseType(typeof(IEnumerable<UserPermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GrantPermissionBatch([FromBody] CreateBatchUserPermissionsDto dto)
        {
            var isSystemAdmin = await IsAdminAsync();
            var adminId = await GetCurrentUserIdAsync();
            if (!adminId.HasValue) return Unauthorized();

            if (!isSystemAdmin)
            {
                if (dto.DuAnId.HasValue)
                {
                    var project = await _context.DuAns.AsNoTracking().FirstOrDefaultAsync(da => da.Id == dto.DuAnId.Value);
                    if (project == null || project.CreatedByUserId != adminId.Value)
                    {
                        return Forbid();
                    }
                }
                else
                {
                    return Forbid();
                }
            }

            try
            {
                var result = await _permissionService.GrantUserPermissionsBatchAsync(adminId.Value, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Kiểm tra Quyền truy cập và thao tác của Người dùng hiện tại trên một Dự án cụ thể.
        /// </summary>
        /// <param name="duAnId">Mã định danh Dự án (GUID)</param>
        /// <returns>Thông tin các quyền trên Dự án (Xem, Sửa, Xóa, Phê duyệt)</returns>
        /// <response code="200">Kiểm tra quyền thành công</response>
        /// <response code="404">Không tìm thấy Dự án</response>
        [HttpGet("du-an/{duAnId:guid}")]
        [ProducesResponseType(typeof(DuAnPermissionCheckDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDuAnPermission(Guid duAnId)
        {
            var userId = await GetCurrentUserIdAsync();
            if (!userId.HasValue) return Unauthorized();

            try
            {
                var result = await _permissionService.GetDuAnPermissionAsync(userId.Value, duAnId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Thu hồi một Quyền đã cấp cho Người dùng theo ID (GUID).
        /// </summary>
        /// <param name="id">Mã định danh Quyền người dùng (GUID)</param>
        /// <response code="200">Thu hồi quyền thành công</response>
        /// <response code="403">Yêu cầu quyền Quản trị hệ thống</response>
        /// <response code="404">Không tìm thấy bản ghi phân quyền</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokePermission(Guid id)
        {
            var isSystemAdmin = await IsAdminAsync();
            var currentUserId = await GetCurrentUserIdAsync();
            if (!currentUserId.HasValue) return Unauthorized();

            if (!isSystemAdmin)
            {
                var userPerm = await _context.UserPermissions.AsNoTracking().FirstOrDefaultAsync(up => up.Id == id);
                if (userPerm == null) return NotFound(new { Message = "Không tìm thấy quyền người dùng." });

                if (userPerm.DuAnId.HasValue)
                {
                    var project = await _context.DuAns.AsNoTracking().FirstOrDefaultAsync(da => da.Id == userPerm.DuAnId.Value);
                    if (project == null || project.CreatedByUserId != currentUserId.Value)
                    {
                        return Forbid();
                    }
                }
                else
                {
                    return Forbid();
                }
            }

            var success = await _permissionService.RevokeUserPermissionAsync(id);
            if (!success) return NotFound(new { Message = "Không tìm thấy quyền người dùng." });

            return Ok(new { Message = "Thu hồi quyền thành công." });
        }
    }
}
