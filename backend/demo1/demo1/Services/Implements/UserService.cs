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
    public class UserService : IUserService
    {
        private readonly AppDbContext _dbContext;

        public UserService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserImportResultDto> ImportUsersAsync(List<CreateUserDto> dtos)
        {
            if (dtos == null || !dtos.Any())
            {
                return new UserImportResultDto
                {
                    Message = "Danh sách người dùng không được để trống.",
                    Errors = new List<UserImportErrorDto>()
                };
            }

            // Bước 1: Validate dữ liệu từng dòng
            var errors = dtos
                .Select((dto, i) => new { dto, index = i + 1 })
                .Where(x => string.IsNullOrWhiteSpace(x.dto.Username))
                .Select(x => new UserImportErrorDto
                {
                    RowIndex = x.index,
                    Username = x.dto.Username ?? string.Empty,
                    ErrorMessages = new List<string> { "Username không được để trống." }
                })
                .ToList();

            if (errors.Any())
            {
                return new UserImportResultDto
                {
                    Message = "Dữ liệu import không hợp lệ.",
                    AddedCount = 0,
                    UpdatedCount = 0,
                    ErrorCount = errors.Count,
                    Errors = errors
                };
            }

            // Bước 2: Tự động xử lý Phòng ban, Chức vụ, Đơn vị và Vai trò (tạo mới nếu chưa tồn tại)
            var inputPhongBans = GetUniqueTrimmedNames(dtos, d => d.TenPhongBan);
            var inputChucVus = GetUniqueTrimmedNames(dtos, d => d.TenChucVu);
            var inputDonVis = GetUniqueTrimmedNames(dtos, d => d.TenDonVi);
            var inputRoles = GetUniqueTrimmedNames(dtos, d => d.Role);

            var phongBanMap = await EnsureLookupsExistAsync(
                inputPhongBans,
                _dbContext.PhongBans,
                pb => pb.TenPhongBan,
                name => new PhongBan { Id = Guid.NewGuid(), TenPhongBan = name, CreatedAt = DateTime.UtcNow }
            );

            var chucVuMap = await EnsureLookupsExistAsync(
                inputChucVus,
                _dbContext.ChucVus,
                cv => cv.TenChucVu,
                name => new ChucVu { Id = Guid.NewGuid(), TenChucVu = name, CreatedAt = DateTime.UtcNow }
            );

            var donViMap = await EnsureLookupsExistAsync(
                inputDonVis,
                _dbContext.DonVis,
                dv => dv.TenDonVi,
                name => new DonVi { Id = Guid.NewGuid(), TenDonVi = name, CreatedAt = DateTime.UtcNow }
            );

            var roleMap = await EnsureLookupsExistAsync(
                inputRoles,
                _dbContext.Roles,
                r => r.Name,
                name => new Role 
                { 
                    Id = Guid.NewGuid(), 
                    Name = name, 
                    Description = $"Mô tả cho vai trò {name} (Tạo tự động khi import)", 
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow 
                }
            );

            if (_dbContext.ChangeTracker.HasChanges())
            {
                await _dbContext.SaveChangesAsync();
            }

            // Bước 3: Tiến hành Upsert các User (Khớp theo Username thuần túy)
            var inputUsernamesList = dtos.Select(d => d.Username.Trim().ToLower()).Distinct().ToList();
            var existingUsers = await _dbContext.Users
                .Where(u => inputUsernamesList.Contains(u.Username.ToLower()))
                .ToListAsync();

            var existingUserMap = existingUsers
                .GroupBy(u => u.Username.Trim().ToLower())
                .ToDictionary(g => g.Key, g => g.First());

            var existingUserIds = existingUsers.Select(u => u.Id).ToList();
            var existingUserRoles = await _dbContext.UserRoles
                .Where(ur => existingUserIds.Contains(ur.UserId))
                .ToListAsync();

            int addedCount = 0;
            int updatedCount = 0;

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var dto in dtos)
                {
                    var trimmedUsername = dto.Username.Trim();
                    var lowerUsername = trimmedUsername.ToLower();

                    // Phân giải các thực thể tham chiếu
                    var (idPhongBan, tenPhongBan) = ResolveReference(dto.TenPhongBan, dto.IdPhongBan, phongBanMap, pb => pb.Id, pb => pb.TenPhongBan);
                    var (idChucVu, tenChucVu) = ResolveReference(dto.TenChucVu, dto.IdChucVu, chucVuMap, cv => cv.Id, cv => cv.TenChucVu);
                    var (idDonVi, tenDonVi) = ResolveReference(dto.TenDonVi, dto.IdDonVi, donViMap, dv => dv.Id, dv => dv.TenDonVi);

                    Role? targetRole = null;
                    if (!string.IsNullOrWhiteSpace(dto.Role))
                    {
                        roleMap.TryGetValue(dto.Role.Trim().ToLower(), out targetRole);
                    }

                    var email = string.IsNullOrWhiteSpace(dto.Email) 
                        ? $"{lowerUsername}@co-opbank.vn" 
                        : dto.Email.Trim();

                    if (!existingUserMap.TryGetValue(lowerUsername, out var user))
                    {
                        user = new User
                        {
                            Id = Guid.NewGuid(),
                            Username = trimmedUsername,
                            IsTwoFactorEnabled = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        _dbContext.Users.Add(user);
                        existingUserMap[lowerUsername] = user;
                        addedCount++;
                    }
                    else
                    {
                        user.UpdatedAt = DateTime.UtcNow;
                        updatedCount++;
                    }

                    // Ánh xạ các thuộc tính chung
                    user.FullName = dto.FullName?.Trim() ?? string.Empty;
                    user.Email = email;
                    
                    if (!string.IsNullOrWhiteSpace(dto.Phone))
                    {
                        user.Phone = dto.Phone.Trim();
                    }

                    user.IdPhongBan = idPhongBan ?? user.IdPhongBan;
                    if (!string.IsNullOrWhiteSpace(tenPhongBan))
                    {
                        user.TenPhongBan = tenPhongBan;
                    }

                    user.IdChucVu = idChucVu ?? user.IdChucVu;
                    if (!string.IsNullOrWhiteSpace(tenChucVu))
                    {
                        user.TenChucVu = tenChucVu;
                    }

                    user.IdDonVi = idDonVi ?? user.IdDonVi;
                    if (!string.IsNullOrWhiteSpace(tenDonVi))
                    {
                        user.TenDonVi = tenDonVi;
                    }

                    user.IsActive = dto.IsActive;
                    user.IsSystemAdmin = dto.IsSystemAdmin;

                    // Xử lý cập nhật vai trò (Role)
                    if (targetRole != null)
                    {
                        var currentRoles = existingUserRoles.Where(ur => ur.UserId == user.Id).ToList();
                        if (currentRoles.Any())
                        {
                            _dbContext.UserRoles.RemoveRange(currentRoles);
                            existingUserRoles.RemoveAll(ur => ur.UserId == user.Id);
                        }

                        var newUr = new UserRole
                        {
                            UserId = user.Id,
                            RoleId = targetRole.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        _dbContext.UserRoles.Add(newUr);
                        existingUserRoles.Add(newUr);
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            return new UserImportResultDto
            {
                Message = "Import dữ liệu thành công.",
                AddedCount = addedCount,
                UpdatedCount = updatedCount,
                ErrorCount = 0,
                Errors = new List<UserImportErrorDto>()
            };
        }

        private List<string> GetUniqueTrimmedNames(List<CreateUserDto> dtos, Func<CreateUserDto, string?> nameSelector)
        {
            return dtos
                .Select(nameSelector)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!.Trim())
                .Distinct()
                .ToList();
        }

        private async Task<Dictionary<string, TEntity>> EnsureLookupsExistAsync<TEntity>(
            List<string> inputNames,
            DbSet<TEntity> dbSet,
            Func<TEntity, string> getNameFunc,
            Func<string, TEntity> createEntityFunc) where TEntity : class
        {
            var dbEntities = await dbSet.ToListAsync();
            var entityMap = dbEntities.ToDictionary(e => getNameFunc(e).ToLower(), e => e);

            var newEntities = new List<TEntity>();
            foreach (var name in inputNames)
            {
                var lowerName = name.ToLower();
                if (!entityMap.ContainsKey(lowerName))
                {
                    var entity = createEntityFunc(name);
                    newEntities.Add(entity);
                    entityMap[lowerName] = entity;
                }
            }

            if (newEntities.Any())
            {
                dbSet.AddRange(newEntities);
            }

            return entityMap;
        }

        private (Guid? Id, string? Name) ResolveReference<TEntity>(
            string? inputName,
            Guid? inputId,
            Dictionary<string, TEntity> map,
            Func<TEntity, Guid> getId,
            Func<TEntity, string> getName)
        {
            if (string.IsNullOrWhiteSpace(inputName))
            {
                return (inputId, null);
            }

            var key = inputName.Trim().ToLower();
            if (map.TryGetValue(key, out var entity))
            {
                return (getId(entity), getName(entity));
            }

            return (inputId, inputName.Trim());
        }

        public async Task<UserDeleteResultDto> DeleteUsersAsync(List<Guid> ids)
        {
            if (ids == null || !ids.Any())
            {
                throw new ArgumentException("Danh sách ID cần xoá không được để trống.", nameof(ids));
            }

            // Tìm các user có ID trong danh sách
            var usersToDelete = await _dbContext.Users
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            if (!usersToDelete.Any())
            {
                return new UserDeleteResultDto
                {
                    Message = "Không tìm thấy người dùng nào phù hợp để xoá.",
                    DeletedCount = 0
                };
            }

            // Xoá liên kết vai trò của các user này trong bảng UserRoles trước (để tránh lỗi khoá ngoại)
            var userIds = usersToDelete.Select(u => u.Id).ToList();
            var relatedUserRoles = await _dbContext.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .ToListAsync();

            if (relatedUserRoles.Any())
            {
                _dbContext.UserRoles.RemoveRange(relatedUserRoles);
            }

            // Tiến hành xoá các user
            _dbContext.Users.RemoveRange(usersToDelete);
            await _dbContext.SaveChangesAsync();

            return new UserDeleteResultDto
            {
                Message = $"Đã xoá thành công {usersToDelete.Count} người dùng.",
                DeletedCount = usersToDelete.Count
            };
        }
    }
}
