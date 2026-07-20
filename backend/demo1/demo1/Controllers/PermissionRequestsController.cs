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
    [Authorize]
    [ApiController]
    [Route("api/permission-requests")]
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

        [HttpPost]
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

        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = await GetCurrentUserIdAsync();
            if (!userId.HasValue) return Unauthorized();

            var requests = await _permissionService.GetUserRequestsAsync(userId.Value);
            return Ok(requests);
        }

        [HttpGet("admin")]
        public async Task<IActionResult> GetAllRequests(
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!await IsAdminAsync()) return Forbid();

            var result = await _permissionService.GetAllRequestsAsync(status, search, page, pageSize);
            return Ok(result);
        }

        [HttpPost("{id:guid}/review")]
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
