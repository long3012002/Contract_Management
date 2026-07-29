using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;

namespace demo1.Controllers
{
    /// <summary>
    /// API Quản trị Hệ thống (Quản lý Vai trò/Role, Tính năng/Feature, Phân quyền Vai trò và Nhật ký hoạt động/Audit Logs).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/HeThong/admin")]
    public class AdminController(IAdminService adminService) : ControllerBase
    {
        private async Task<bool> IsAdminAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            return await adminService.IsSystemAdminAsync(username);
        }

        private async Task<bool> CanViewUserPermissionsAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            return await adminService.CanViewUserPermissionsAsync(username);
        }

        // --- ROLES MANAGEMENT ---

        /// <summary>
        /// Lấy danh sách tất cả các Vai trò (Roles) trong hệ thống.
        /// </summary>
        /// <returns>Danh sách vai trò</returns>
        /// <response code="200">Lấy danh sách vai trò thành công</response>
        /// <response code="403">Không có quyền xem vai trò</response>
        [HttpGet("roles")]
        [ProducesResponseType(typeof(IEnumerable<Role>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRoles()
        {
            if (!await CanViewUserPermissionsAsync()) return Forbid();
            var roles = await adminService.GetRolesAsync();
            return Ok(roles);
        }

        /// <summary>
        /// Tạo mới một Vai trò (Role).
        /// </summary>
        /// <param name="dto">Thông tin vai trò mới</param>
        /// <returns>Thông tin vai trò vừa tạo</returns>
        /// <response code="200">Tạo mới vai trò thành công</response>
        /// <response code="403">Chỉ Quản trị viên mới được phép tạo</response>
        [HttpPost("roles")]
        [ProducesResponseType(typeof(Role), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            var role = await adminService.CreateRoleAsync(dto);
            return Ok(role);
        }

        /// <summary>
        /// Cập nhật thông tin Vai trò theo ID.
        /// </summary>
        /// <param name="roleId">Mã định danh Vai trò (GUID)</param>
        /// <param name="dto">Thông tin cập nhật</param>
        /// <returns>Vai trò sau khi cập nhật</returns>
        /// <response code="200">Cập nhật vai trò thành công</response>
        /// <response code="404">Không tìm thấy vai trò</response>
        [HttpPut("roles/{roleId:guid}")]
        [ProducesResponseType(typeof(Role), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] UpdateRoleDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var role = await adminService.UpdateRoleAsync(roleId, dto);
                return Ok(role);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        // --- PERMISSIONS MANAGEMENT ---

        /// <summary>
        /// Lấy danh sách các Tính năng (Features) của ứng dụng.
        /// </summary>
        /// <returns>Danh sách tính năng</returns>
        /// <response code="200">Lấy danh sách tính năng thành công</response>
        [HttpGet("features")]
        [ProducesResponseType(typeof(IEnumerable<Feature>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFeatures()
        {
            if (!await CanViewUserPermissionsAsync()) return Forbid();
            var features = await adminService.GetFeaturesAsync();
            return Ok(features);
        }

        /// <summary>
        /// Tạo mới một Tính năng (Feature).
        /// </summary>
        /// <param name="dto">Thông tin tính năng mới</param>
        /// <returns>Tính năng vừa tạo</returns>
        /// <response code="200">Tạo tính năng thành công</response>
        [HttpPost("features")]
        [ProducesResponseType(typeof(Feature), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateFeature([FromBody] CreateFeatureDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            var feature = await adminService.CreateFeatureAsync(dto);
            return Ok(feature);
        }

        /// <summary>
        /// Cập nhật thông tin Tính năng theo ID.
        /// </summary>
        /// <param name="featureId">Mã định danh Tính năng (GUID)</param>
        /// <param name="dto">Dữ liệu cập nhật</param>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="404">Không tìm thấy tính năng</response>
        [HttpPut("features/{featureId:guid}")]
        [ProducesResponseType(typeof(Feature), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateFeature(Guid featureId, [FromBody] UpdateFeatureDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var feature = await adminService.UpdateFeatureAsync(featureId, dto);
                return Ok(feature);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa một Tính năng theo ID.
        /// </summary>
        /// <param name="featureId">Mã định danh Tính năng (GUID)</param>
        /// <response code="200">Xóa thành công</response>
        /// <response code="404">Không tìm thấy tính năng</response>
        [HttpDelete("features/{featureId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteFeature(Guid featureId)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                await adminService.DeleteFeatureAsync(featureId);
                return Ok(new { Message = "Feature deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách quyền hạn chi tiết gắn với một Vai trò (Role).
        /// </summary>
        /// <param name="roleId">Mã định danh Vai trò (GUID)</param>
        /// <returns>Danh sách quyền hạn của vai trò</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        /// <response code="404">Không tìm thấy vai trò</response>
        [HttpGet("roles/{roleId:guid}/permissions")]
        [ProducesResponseType(typeof(IEnumerable<RolePermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRolePermissions(Guid roleId)
        {
            if (!await CanViewUserPermissionsAsync()) return Forbid();
            try
            {
                var result = await adminService.GetRolePermissionsAsync(roleId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật danh sách quyền hạn cho một Vai trò (Role).
        /// </summary>
        /// <param name="roleId">Mã định danh Vai trò (GUID)</param>
        /// <param name="permissions">Danh sách quyền hạn cập nhật</param>
        /// <response code="200">Cập nhật quyền hạn thành công</response>
        /// <response code="404">Không tìm thấy vai trò</response>
        [HttpPut("roles/{roleId:guid}/permissions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRolePermissions(Guid roleId, [FromBody] List<UpdateRolePermissionDto> permissions)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                await adminService.UpdateRolePermissionsAsync(roleId, permissions);
                return Ok(new { Message = "Permissions updated successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        // --- USER ROLES MANAGEMENT ---

        /// <summary>
        /// Lấy danh sách Người dùng kèm thông tin các Vai trò được gán.
        /// </summary>
        /// <param name="filter">Bộ lọc tìm kiếm và phân trang người dùng</param>
        /// <returns>Danh sách người dùng phân trang kèm danh sách Vai trò</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet("users")]
        [ProducesResponseType(typeof(PagedResult<UserWithRolesDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers([FromQuery] UserFilterDto filter)
        {
            if (!await CanViewUserPermissionsAsync()) return Forbid();
            var result = await adminService.GetUsersWithRolesAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách ID Vai trò đã gán cho một Người dùng.
        /// </summary>
        /// <param name="userId">Mã định danh Người dùng (GUID)</param>
        /// <returns>Danh sách GUID các Vai trò</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        /// <response code="404">Không tìm thấy người dùng</response>
        [HttpGet("users/{userId:guid}/roles")]
        [ProducesResponseType(typeof(IEnumerable<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserRoles(Guid userId)
        {
            if (!await CanViewUserPermissionsAsync()) return Forbid();
            try
            {
                var assignedRoleIds = await adminService.GetUserRolesAsync(userId);
                return Ok(assignedRoleIds);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật danh sách Vai trò cho Người dùng.
        /// </summary>
        /// <param name="userId">Mã định danh Người dùng (GUID)</param>
        /// <param name="dto">Danh sách ID các vai trò gán mới</param>
        /// <response code="200">Gán vai trò thành công</response>
        /// <response code="404">Không tìm thấy người dùng</response>
        [HttpPut("users/{userId:guid}/roles")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserRoles(Guid userId, [FromBody] UserRolesUpdateDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                await adminService.UpdateUserRolesAsync(userId, dto);
                return Ok(new { Message = "User roles updated successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Xem Nhật ký Hệ thống (Audit Logs) có phân trang và lọc theo Người dùng / Ngày / Tên bảng.
        /// </summary>
        /// <param name="userId">ID Người dùng thực hiện thao tác (tùy chọn)</param>
        /// <param name="date">Ngày thao tác (tùy chọn)</param>
        /// <param name="tableName">Tên bảng bị ảnh hưởng (tùy chọn)</param>
        /// <param name="page">Trang hiện tại (Mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (Mặc định: 20)</param>
        /// <returns>Danh sách Audit Logs phân trang</returns>
        /// <response code="200">Lấy nhật ký hệ thống thành công</response>
        [HttpGet("audit-logs")]
        [ProducesResponseType(typeof(PagedResult<AuditLog>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] string? userId,
            [FromQuery] DateTime? date,
            [FromQuery] string? tableName,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!await IsAdminAsync()) return Forbid();
            var result = await adminService.GetAuditLogsAsync(userId, date, tableName, page, pageSize);
            return Ok(result);
        }
    }
}
