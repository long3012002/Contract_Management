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
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController(IAdminService adminService) : ControllerBase
    {
        private async Task<bool> IsAdminAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            return await adminService.IsSystemAdminAsync(username);
        }

        // --- ROLES MANAGEMENT ---

        [HttpGet("roles")]
        [ProducesResponseType(typeof(IEnumerable<Role>), 200)]
        public async Task<IActionResult> GetRoles()
        {
            if (!await IsAdminAsync()) return Forbid();
            var roles = await adminService.GetRolesAsync();
            return Ok(roles);
        }

        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            var role = await adminService.CreateRoleAsync(dto);
            return Ok(role);
        }

        [HttpPut("roles/{roleId:guid}")]
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

        [HttpGet("features")]
        [ProducesResponseType(typeof(IEnumerable<Feature>), 200)]
        public async Task<IActionResult> GetFeatures()
        {
            if (!await IsAdminAsync()) return Forbid();
            var features = await adminService.GetFeaturesAsync();
            return Ok(features);
        }

        [HttpPost("features")]
        public async Task<IActionResult> CreateFeature([FromBody] CreateFeatureDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            var feature = await adminService.CreateFeatureAsync(dto);
            return Ok(feature);
        }

        [HttpPut("features/{featureId:guid}")]
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

        [HttpDelete("features/{featureId:guid}")]
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

        [HttpGet("roles/{roleId:guid}/permissions")]
        [ProducesResponseType(typeof(IEnumerable<RolePermissionDto>), 200)]
        public async Task<IActionResult> GetRolePermissions(Guid roleId)
        {
            if (!await IsAdminAsync()) return Forbid();
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

        [HttpPut("roles/{roleId:guid}/permissions")]
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

        [HttpGet("users")]
        [ProducesResponseType(typeof(PagedResult<UserWithRolesDto>), 200)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!await IsAdminAsync()) return Forbid();
            var result = await adminService.GetUsersWithRolesAsync(search, page, pageSize);
            return Ok(result);
        }

        [HttpGet("users/{userId:guid}/roles")]
        [ProducesResponseType(typeof(IEnumerable<Guid>), 200)]
        public async Task<IActionResult> GetUserRoles(Guid userId)
        {
            if (!await IsAdminAsync()) return Forbid();
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

        [HttpPut("users/{userId:guid}/roles")]
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

        [HttpGet("audit-logs")]
        [ProducesResponseType(typeof(PagedResult<AuditLog>), 200)]
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
