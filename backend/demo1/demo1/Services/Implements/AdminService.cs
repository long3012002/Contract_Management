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

            var role = new Role
            {
                Name = dto.Name,
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Roles.Add(role);
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

            var features = await _dbContext.Features.Where(f => f.IsActive).ToListAsync();

            return features.Select(f => new RolePermissionDto
            {
                FeatureId = f.Id,
                FeatureCode = f.Code,
                FeatureName = f.Name,
                CanAccess = true,
                Permissions = "All"
            }).ToList();
        }

        public async Task UpdateRolePermissionsAsync(Guid roleId, List<UpdateRolePermissionDto> permissions)
        {
            await Task.CompletedTask;
        }

        public async Task<PagedResult<UserWithRolesDto>> GetUsersWithRolesAsync(string? search, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            IQueryable<User> query = _dbContext.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(u => 
                    EF.Functions.Like(u.Username, $"%{keyword}%") || 
                    EF.Functions.Like(u.FullName, $"%{keyword}%") ||
                    (u.Email != null && EF.Functions.Like(u.Email, $"%{keyword}%"))
                );
            }

            var totalItems = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            var userIds = users.Select(u => u.Id).ToList();
            var userRoles = await _dbContext.UserRoles
                .AsNoTracking()
                .Include(ur => ur.Role)
                .Where(ur => userIds.Contains(ur.UserId))
                .ToListAsync();

            var dtos = users.Select(u => new UserWithRolesDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                IdPhongBan = u.IdPhongBan,
                IdChucVu = u.IdChucVu,
                IdDonVi = u.IdDonVi,
                TenPhongBan = u.TenPhongBan,
                TenChucVu = u.TenChucVu,
                TenDonVi = u.TenDonVi,
                IsActive = u.IsActive,
                IsSystemAdmin = u.IsSystemAdmin,
                Roles = userRoles.Where(ur => ur.UserId == u.Id && ur.Role != null).Select(ur => ur.Role!.Name).ToList()
            }).ToList();

            return new PagedResult<UserWithRolesDto>
            {
                Items = dtos,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };
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

        public async Task<PagedResult<AuditLog>> GetAuditLogsAsync(string? userId, DateTime? date, string? tableName, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            IQueryable<AuditLog> query = _dbContext.AuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            if (date.HasValue)
            {
                var startDate = date.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(a => a.Timestamp >= startDate && a.Timestamp < endDate);
            }

            if (!string.IsNullOrWhiteSpace(tableName))
            {
                var nameLower = tableName.Trim().ToLower();
                query = query.Where(a => a.TableName.ToLower() == nameLower);
            }

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AuditLog>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }
    }
}
