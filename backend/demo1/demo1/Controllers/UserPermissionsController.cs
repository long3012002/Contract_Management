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
    [Authorize]
    [ApiController]
    [Route("api/user-permissions")]
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

        [HttpGet]
        public async Task<IActionResult> GetUserPermissions([FromQuery] Guid? userId, [FromQuery] string? featureCode)
        {
            var permissions = await _permissionService.GetUserPermissionsAsync(userId, featureCode);
            return Ok(permissions);
        }

        [HttpGet("catalog")]
        [ProducesResponseType(typeof(IEnumerable<PermissionCatalogDto>), 200)]
        public async Task<IActionResult> GetCatalog()
        {
            var catalog = await _permissionService.GetPermissionCatalogAsync();
            return Ok(catalog);
        }

        [HttpPost]
        public async Task<IActionResult> GrantPermission([FromBody] CreateUserPermissionDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();

            var adminId = await GetCurrentUserIdAsync();
            if (!adminId.HasValue) return Unauthorized();

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

        [HttpGet("du-an/{duAnId:guid}")]
        [ProducesResponseType(typeof(DuAnPermissionCheckDto), 200)]
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

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> RevokePermission(Guid id)
        {
            if (!await IsAdminAsync()) return Forbid();

            var success = await _permissionService.RevokeUserPermissionAsync(id);
            if (!success) return NotFound(new { Message = "Không tìm thấy quyền người dùng." });

            return Ok(new { Message = "Thu hồi quyền thành công." });
        }
    }
}
