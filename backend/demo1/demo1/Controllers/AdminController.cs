using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using demo1.Data;
using demo1.Entity;

namespace demo1.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public AdminController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private async Task<bool> IsAdminAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            return dbUser?.IsSystemAdmin == true;
        }

        // --- ROLES MANAGEMENT ---

        [HttpGet("roles")]
        [ProducesResponseType(typeof(IEnumerable<Role>), 200)]
        public async Task<IActionResult> GetRoles()
        {
            if (!await IsAdminAsync()) return Forbid();
            var roles = await _dbContext.Roles.OrderBy(r => r.Name).ToListAsync();
            return Ok(roles);
        }

        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Role name is required.");

            var exists = await _dbContext.Roles.AnyAsync(r => r.Name.ToLower() == dto.Name.ToLower());
            if (exists) return Conflict("Role already exists.");

            var role = new Role
            {
                Name = dto.Name,
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Roles.Add(role);
            await _dbContext.SaveChangesAsync();

            return Ok(role);
        }

        [HttpPut("roles/{roleId:guid}")]
        public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] UpdateRoleDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            var role = await _dbContext.Roles.FindAsync(roleId);
            if (role == null) return NotFound("Role not found.");

            role.Name = dto.Name;
            role.Description = dto.Description;
            role.IsActive = dto.IsActive;
            role.UpdatedAt = DateTime.UtcNow;

            _dbContext.Roles.Update(role);
            await _dbContext.SaveChangesAsync();

            return Ok(role);
        }

        // --- PERMISSIONS MANAGEMENT ---

        [HttpGet("features")]
        [ProducesResponseType(typeof(IEnumerable<Feature>), 200)]
        public async Task<IActionResult> GetFeatures()
        {
            if (!await IsAdminAsync()) return Forbid();
            var features = await _dbContext.Features.OrderBy(f => f.Name).ToListAsync();
            return Ok(features);
        }

        [HttpPost("features")]
        public async Task<IActionResult> CreateFeature([FromBody] CreateFeatureDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Feature code is required.");
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Feature name is required.");

            var exists = await _dbContext.Features.AnyAsync(f => f.Code.ToLower() == dto.Code.ToLower());
            if (exists) return Conflict("Feature code already exists.");

            var feature = new Feature
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Features.Add(feature);
            await _dbContext.SaveChangesAsync();

            return Ok(feature);
        }

        [HttpPut("features/{featureId:guid}")]
        public async Task<IActionResult> UpdateFeature(Guid featureId, [FromBody] UpdateFeatureDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            var feature = await _dbContext.Features.FindAsync(featureId);
            if (feature == null) return NotFound("Feature not found.");

            if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Feature code is required.");
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Feature name is required.");

            var exists = await _dbContext.Features.AnyAsync(f => f.Id != featureId && f.Code.ToLower() == dto.Code.ToLower());
            if (exists) return Conflict("Feature code already exists.");

            feature.Code = dto.Code.Trim();
            feature.Name = dto.Name.Trim();
            feature.Description = dto.Description;
            feature.IsActive = dto.IsActive;

            _dbContext.Features.Update(feature);
            await _dbContext.SaveChangesAsync();

            return Ok(feature);
        }

        [HttpDelete("features/{featureId:guid}")]
        public async Task<IActionResult> DeleteFeature(Guid featureId)
        {
            if (!await IsAdminAsync()) return Forbid();
            var feature = await _dbContext.Features.FindAsync(featureId);
            if (feature == null) return NotFound("Feature not found.");

            var relatedPermissions = await _dbContext.RolePermissions.Where(rp => rp.FeatureId == featureId).ToListAsync();
            _dbContext.RolePermissions.RemoveRange(relatedPermissions);

            _dbContext.Features.Remove(feature);
            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Feature deleted successfully." });
        }

        [HttpGet("roles/{roleId:guid}/permissions")]
        [ProducesResponseType(typeof(IEnumerable<RolePermissionDto>), 200)]
        public async Task<IActionResult> GetRolePermissions(Guid roleId)
        {
            if (!await IsAdminAsync()) return Forbid();
            var role = await _dbContext.Roles.FindAsync(roleId);
            if (role == null) return NotFound("Role not found.");

            // Get existing permissions
            var existingPermissions = await _dbContext.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            var features = await _dbContext.Features.Where(f => f.IsActive).ToListAsync();

            var result = features.Select(f =>
            {
                var perm = existingPermissions.FirstOrDefault(p => p.FeatureId == f.Id);
                return new RolePermissionDto
                {
                    FeatureId = f.Id,
                    FeatureCode = f.Code,
                    FeatureName = f.Name,
                    CanAccess = perm?.CanAccess ?? false,
                    Permissions = perm?.Permissions ?? string.Empty
                };
            }).ToList();

            return Ok(result);
        }

        [HttpPut("roles/{roleId:guid}/permissions")]
        public async Task<IActionResult> UpdateRolePermissions(Guid roleId, [FromBody] List<UpdateRolePermissionDto> permissions)
        {
            if (!await IsAdminAsync()) return Forbid();
            var role = await _dbContext.Roles.FindAsync(roleId);
            if (role == null) return NotFound("Role not found.");

            // Remove existing permissions
            var existing = await _dbContext.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
            _dbContext.RolePermissions.RemoveRange(existing);

            // Add new permissions
            foreach (var perm in permissions)
            {
                _dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    FeatureId = perm.FeatureId,
                    CanAccess = perm.CanAccess,
                    Permissions = perm.Permissions ?? string.Empty,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync();
            return Ok(new { Message = "Permissions updated successfully." });
        }

        // --- USER ROLES MANAGEMENT ---

        [HttpGet("users")]
        [ProducesResponseType(typeof(IEnumerable<UserWithRolesDto>), 200)]
        public async Task<IActionResult> GetUsers()
        {
            if (!await IsAdminAsync()) return Forbid();
            
            var users = await _dbContext.Users.OrderBy(u => u.Username).ToListAsync();
            
            var userRoles = await _dbContext.UserRoles
                .Include(ur => ur.Role)
                .ToListAsync();

            var result = users.Select(u => new UserWithRolesDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                IsActive = u.IsActive,
                IsSystemAdmin = u.IsSystemAdmin,
                Roles = userRoles.Where(ur => ur.UserId == u.Id && ur.Role != null).Select(ur => ur.Role!.Name).ToList()
            }).ToList();

            return Ok(result);
        }

        [HttpGet("users/{userId:guid}/roles")]
        [ProducesResponseType(typeof(IEnumerable<Guid>), 200)]
        public async Task<IActionResult> GetUserRoles(Guid userId)
        {
            if (!await IsAdminAsync()) return Forbid();
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            var assignedRoleIds = await _dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            return Ok(assignedRoleIds);
        }

        [HttpPut("users/{userId:guid}/roles")]
        public async Task<IActionResult> UpdateUserRoles(Guid userId, [FromBody] UserRolesUpdateDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            // Remove existing user roles
            var existing = await _dbContext.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
            _dbContext.UserRoles.RemoveRange(existing);

            // Add new user roles
            if (dto.RoleIds != null)
            {
                foreach (var roleId in dto.RoleIds)
                {
                    _dbContext.UserRoles.Add(new UserRole
                    {
                        UserId = userId,
                        RoleId = roleId
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
            return Ok(new { Message = "User roles updated successfully." });
        }
    }

    // --- DTO CLASSES ---

    public class CreateRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class RolePermissionDto
    {
        public Guid FeatureId { get; set; }
        public string FeatureCode { get; set; } = string.Empty;
        public string FeatureName { get; set; } = string.Empty;
        public bool CanAccess { get; set; }
        public string Permissions { get; set; } = string.Empty;
    }

    public class UpdateRolePermissionDto
    {
        public Guid FeatureId { get; set; }
        public bool CanAccess { get; set; }
        public string Permissions { get; set; } = string.Empty;
    }

    public class UserWithRolesDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public bool IsSystemAdmin { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class UserRolesUpdateDto
    {
        public List<Guid> RoleIds { get; set; } = new();
    }

    public class CreateFeatureDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateFeatureDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}

