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
    /// API Quản lý Vai trò (Roles) và Phân quyền Vai trò.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/HeThong/admin/roles")]
    public class RolesController(IAdminService adminService) : ControllerBase
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

        /// <summary>
        /// Lấy danh sách tất cả các Vai trò (Roles) trong hệ thống.
        /// </summary>
        /// <returns>Danh sách vai trò</returns>
        /// <response code="200">Lấy danh sách vai trò thành công</response>
        /// <response code="403">Không có quyền xem vai trò</response>
        [HttpGet]
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
        [HttpPost]
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
        [HttpPut("{roleId:guid}")]
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

        /// <summary>
        /// Lấy danh sách quyền hạn chi tiết gắn với một Vai trò (Role).
        /// </summary>
        /// <param name="roleId">Mã định danh Vai trò (GUID)</param>
        /// <returns>Danh sách quyền hạn của vai trò</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        /// <response code="404">Không tìm thấy vai trò</response>
        [HttpGet("{roleId:guid}/permissions")]
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
        [HttpPut("{roleId:guid}/permissions")]
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

        /// <summary>
        /// Lấy danh sách ID Vai trò đã gán cho một Người dùng.
        /// </summary>
        /// <param name="userId">Mã định danh Người dùng (GUID)</param>
        /// <returns>Danh sách GUID các Vai trò</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        /// <response code="404">Không tìm thấy người dùng</response>
        [HttpGet("~/api/HeThong/admin/users/{userId:guid}/roles")]
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
        [HttpPut("~/api/HeThong/admin/users/{userId:guid}/roles")]
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
    }
}
