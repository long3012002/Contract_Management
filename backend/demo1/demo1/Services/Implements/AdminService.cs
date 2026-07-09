using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;

namespace demo1.Services.Implements
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _dbContext;

        public AdminService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> IsSystemAdminAsync(string username)
        {
            if (string.IsNullOrEmpty(username)) return false;
            var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            return dbUser?.IsSystemAdmin == true;
        }

        public async Task<IEnumerable<Role>> GetRolesAsync()
        {
            return await _dbContext.Roles.OrderBy(r => r.Name).ToListAsync();
        }

        public async Task<Role> CreateRoleAsync(CreateRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Tên vai trò là bắt buộc.");
            }

            var exists = await _dbContext.Roles.AnyAsync(r => r.Name.ToLower() == dto.Name.ToLower());
            if (exists)
            {
                throw new InvalidOperationException("Vai trò đã tồn tại.");
            }

            if (dto.IsInherit == true)
            {
                if (!dto.InheritRoleId.HasValue)
                {
                    throw new ArgumentException("InheritRoleId là bắt buộc khi chọn kế thừa.");
                }
                var parentExists = await _dbContext.Roles.AnyAsync(r => r.Id == dto.InheritRoleId.Value);
                if (!parentExists)
                {
                    throw new ArgumentException("Vai trò kế thừa không tồn tại.");
                }
            }

            var role = new Role
            {
                Name = dto.Name,
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Roles.Add(role);

            if (dto.IsInherit == true && dto.InheritRoleId.HasValue)
            {
                var parentPermissions = await _dbContext.RolePermissions
                    .Where(rp => rp.RoleId == dto.InheritRoleId.Value)
                    .ToListAsync();

                foreach (var parentPermission in parentPermissions)
                {
                    _dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        FeatureId = parentPermission.FeatureId,
                        CanAccess = parentPermission.CanAccess,
                        Permissions = parentPermission.Permissions,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
            return role;
        }

        public async Task<Role> UpdateRoleAsync(Guid roleId, UpdateRoleDto dto)
        {
            var role = await _dbContext.Roles.FindAsync(roleId);
            if (role == null)
            {
                throw new KeyNotFoundException("Không tìm thấy vai trò.");
            }

            role.Name = dto.Name;
            role.Description = dto.Description;
            role.IsActive = dto.IsActive;
            role.UpdatedAt = DateTime.UtcNow;

            _dbContext.Roles.Update(role);
            await _dbContext.SaveChangesAsync();
            return role;
        }

        public async Task<IEnumerable<Feature>> GetFeaturesAsync()
        {
            return await _dbContext.Features.OrderBy(f => f.Name).ToListAsync();
        }

        public async Task<Feature> CreateFeatureAsync(CreateFeatureDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
            {
                throw new ArgumentException("Mã tính năng là bắt buộc.");
            }
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Tên tính năng là bắt buộc.");
            }

            var exists = await _dbContext.Features.AnyAsync(f => f.Code.ToLower() == dto.Code.ToLower());
            if (exists)
            {
                throw new InvalidOperationException("Mã tính năng đã tồn tại.");
            }

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
            return feature;
        }

        public async Task<Feature> UpdateFeatureAsync(Guid featureId, UpdateFeatureDto dto)
        {
            var feature = await _dbContext.Features.FindAsync(featureId);
            if (feature == null)
            {
                throw new KeyNotFoundException("Không tìm thấy tính năng.");
            }

            if (string.IsNullOrWhiteSpace(dto.Code))
            {
                throw new ArgumentException("Mã tính năng là bắt buộc.");
            }
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Tên tính năng là bắt buộc.");
            }

            var exists = await _dbContext.Features.AnyAsync(f => f.Id != featureId && f.Code.ToLower() == dto.Code.ToLower());
            if (exists)
            {
                throw new InvalidOperationException("Mã tính năng đã tồn tại.");
            }

            feature.Code = dto.Code.Trim();
            feature.Name = dto.Name.Trim();
            feature.Description = dto.Description;
            feature.IsActive = dto.IsActive;

            _dbContext.Features.Update(feature);
            await _dbContext.SaveChangesAsync();
            return feature;
        }

        public async Task DeleteFeatureAsync(Guid featureId)
        {
            var feature = await _dbContext.Features.FindAsync(featureId);
            if (feature == null)
            {
                throw new KeyNotFoundException("Không tìm thấy tính năng.");
            }

            var relatedPermissions = await _dbContext.RolePermissions.Where(rp => rp.FeatureId == featureId).ToListAsync();
            _dbContext.RolePermissions.RemoveRange(relatedPermissions);

            _dbContext.Features.Remove(feature);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<RolePermissionDto>> GetRolePermissionsAsync(Guid roleId)
        {
            var role = await _dbContext.Roles.FindAsync(roleId);
            if (role == null)
            {
                throw new KeyNotFoundException("Không tìm thấy vai trò.");
            }

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

            return result;
        }

        public async Task UpdateRolePermissionsAsync(Guid roleId, List<UpdateRolePermissionDto> permissions)
        {
            var role = await _dbContext.Roles.FindAsync(roleId);
            if (role == null)
            {
                throw new KeyNotFoundException("Không tìm thấy vai trò.");
            }

            var existing = await _dbContext.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
            _dbContext.RolePermissions.RemoveRange(existing);

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
        }

        public async Task<IEnumerable<UserWithRolesDto>> GetUsersWithRolesAsync()
        {
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

            return result;
        }

        public async Task<IEnumerable<Guid>> GetUserRolesAsync(Guid userId)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("Không tìm thấy người dùng.");
            }

            var assignedRoleIds = await _dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            return assignedRoleIds;
        }

        public async Task UpdateUserRolesAsync(Guid userId, UserRolesUpdateDto dto)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("Không tìm thấy người dùng.");
            }

            var existing = await _dbContext.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
            _dbContext.UserRoles.RemoveRange(existing);

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
        }
    }
}
