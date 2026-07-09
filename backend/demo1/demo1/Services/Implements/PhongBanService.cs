using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;

namespace demo1.Services.Implements
{
    public class PhongBanService : IPhongBanService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public PhongBanService(AppDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PhongBanDto>> GetAllAsync()
        {
            var items = await _dbContext.PhongBans.OrderBy(pb => pb.TenPhongBan).ToListAsync();
            return _mapper.Map<IEnumerable<PhongBanDto>>(items);
        }

        public async Task<PhongBanDto?> GetByIdAsync(Guid id)
        {
            var item = await _dbContext.PhongBans.FindAsync(id);
            return item == null ? null : _mapper.Map<PhongBanDto>(item);
        }

        public async Task<PhongBanDto> CreateAsync(CreatePhongBanDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenPhongBan))
            {
                throw new ArgumentException("Tên phòng ban là bắt buộc.");
            }

            var exists = await _dbContext.PhongBans.AnyAsync(pb => pb.TenPhongBan.ToLower() == dto.TenPhongBan.Trim().ToLower());
            if (exists)
            {
                throw new InvalidOperationException("Tên phòng ban đã tồn tại.");
            }

            var item = new PhongBan
            {
                TenPhongBan = dto.TenPhongBan.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.PhongBans.Add(item);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<PhongBanDto>(item);
        }

        public async Task<bool> UpdateAsync(Guid id, UpdatePhongBanDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenPhongBan))
            {
                throw new ArgumentException("Tên phòng ban là bắt buộc.");
            }

            var item = await _dbContext.PhongBans.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            var exists = await _dbContext.PhongBans.AnyAsync(pb => pb.Id != id && pb.TenPhongBan.ToLower() == dto.TenPhongBan.Trim().ToLower());
            if (exists)
            {
                throw new InvalidOperationException("Tên phòng ban đã tồn tại.");
            }

            item.TenPhongBan = dto.TenPhongBan.Trim();
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var item = await _dbContext.PhongBans.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            // Remove associated permissions
            var permissions = await _dbContext.PhongBanPermissions.Where(p => p.PhongBanId == id).ToListAsync();
            _dbContext.PhongBanPermissions.RemoveRange(permissions);

            // Set User reference to null or clear it (if users reference this phongban, update them)
            var users = await _dbContext.Users.Where(u => u.IdPhongBan == id).ToListAsync();
            foreach (var user in users)
            {
                user.IdPhongBan = null;
                user.TenPhongBan = null;
            }

            _dbContext.PhongBans.Remove(item);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<PhongBanPermissionDto>> GetPermissionsAsync(Guid phongBanId)
        {
            var exists = await _dbContext.PhongBans.AnyAsync(pb => pb.Id == phongBanId);
            if (!exists)
            {
                throw new KeyNotFoundException("Không tìm thấy phòng ban.");
            }

            var existingPermissions = await _dbContext.PhongBanPermissions
                .Where(p => p.PhongBanId == phongBanId)
                .ToListAsync();

            var features = await _dbContext.Features.Where(f => f.IsActive).ToListAsync();

            var result = features.Select(f =>
            {
                var perm = existingPermissions.FirstOrDefault(p => p.FeatureId == f.Id);
                return new PhongBanPermissionDto
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

        public async Task UpdatePermissionsAsync(Guid phongBanId, List<UpdatePhongBanPermissionDto> permissions)
        {
            var exists = await _dbContext.PhongBans.AnyAsync(pb => pb.Id == phongBanId);
            if (!exists)
            {
                throw new KeyNotFoundException("Không tìm thấy phòng ban.");
            }

            var existing = await _dbContext.PhongBanPermissions.Where(p => p.PhongBanId == phongBanId).ToListAsync();
            _dbContext.PhongBanPermissions.RemoveRange(existing);

            foreach (var perm in permissions)
            {
                _dbContext.PhongBanPermissions.Add(new PhongBanPermission
                {
                    PhongBanId = phongBanId,
                    FeatureId = perm.FeatureId,
                    CanAccess = perm.CanAccess,
                    Permissions = perm.Permissions ?? string.Empty,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
