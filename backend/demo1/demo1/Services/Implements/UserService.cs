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

            var dbRoles = await _dbContext.Roles.ToListAsync();
            var roleMap = dbRoles.ToDictionary(r => r.Name.ToLower(), r => r);

            // Bước 1: Validate dữ liệu từng dòng (Check trùng mã user theo PGD / Phòng ban)
            var errors = new List<UserImportErrorDto>();
            var userPgdKeysInInput = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < dtos.Count; i++)
            {
                var dto = dtos[i];
                var rowNum = i + 1;
                var rowErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(dto.Username))
                {
                    rowErrors.Add("Username không được để trống.");
                }
                else
                {
                    var trimmedUsername = dto.Username.Trim();
                    var phongBanKey = dto.TenPhongBan?.Trim() ?? string.Empty;
                    var combinedKey = $"{trimmedUsername.ToLower()}@{phongBanKey.ToLower()}";

                    if (userPgdKeysInInput.Contains(combinedKey))
                    {
                        rowErrors.Add($"Mã user '{trimmedUsername}' thuộc PGD/Phòng ban '{phongBanKey}' bị trùng lặp trong danh sách.");
                    }
                    else
                    {
                        userPgdKeysInInput.Add(combinedKey);
                    }
                }

                if (rowErrors.Any())
                {
                    errors.Add(new UserImportErrorDto
                    {
                        RowIndex = rowNum,
                        Username = dto?.Username ?? string.Empty,
                        ErrorMessages = rowErrors
                    });
                }
            }

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

            // Bước 2: Tự động xử lý Phòng ban, Chức vụ và Vai trò (tạo mới nếu chưa tồn tại)
            var inputPhongBans = dtos
                .Where(d => !string.IsNullOrWhiteSpace(d.TenPhongBan))
                .Select(d => d.TenPhongBan!.Trim())
                .Distinct()
                .ToList();

            var inputChucVus = dtos
                .Where(d => !string.IsNullOrWhiteSpace(d.TenChucVu))
                .Select(d => d.TenChucVu!.Trim())
                .Distinct()
                .ToList();

            var inputDonVis = dtos
                .Where(d => !string.IsNullOrWhiteSpace(d.TenDonVi))
                .Select(d => d.TenDonVi!.Trim())
                .Distinct()
                .ToList();

            var inputRoles = dtos
                .Where(d => !string.IsNullOrWhiteSpace(d.Role))
                .Select(d => d.Role!.Trim())
                .Distinct()
                .ToList();

            var dbPhongBans = await _dbContext.PhongBans.ToListAsync();
            var dbChucVus = await _dbContext.ChucVus.ToListAsync();
            var dbDonVis = await _dbContext.DonVis.ToListAsync();

            var phongBanMap = dbPhongBans.ToDictionary(pb => pb.TenPhongBan.ToLower(), pb => pb);
            var chucVuMap = dbChucVus.ToDictionary(cv => cv.TenChucVu.ToLower(), cv => cv);
            var donViMap = dbDonVis.ToDictionary(dv => dv.TenDonVi.ToLower(), dv => dv);

            var newPhongBans = new List<PhongBan>();
            var newChucVus = new List<ChucVu>();
            var newDonVis = new List<DonVi>();
            var newRoles = new List<Role>();

            foreach (var pbName in inputPhongBans)
            {
                var lowerName = pbName.ToLower();
                if (!phongBanMap.ContainsKey(lowerName))
                {
                    var pb = new PhongBan
                    {
                        Id = Guid.NewGuid(),
                        TenPhongBan = pbName,
                        CreatedAt = DateTime.UtcNow
                    };
                    newPhongBans.Add(pb);
                    phongBanMap[lowerName] = pb;
                }
            }

            foreach (var cvName in inputChucVus)
            {
                var lowerName = cvName.ToLower();
                if (!chucVuMap.ContainsKey(lowerName))
                {
                    var cv = new ChucVu
                    {
                        Id = Guid.NewGuid(),
                        TenChucVu = cvName,
                        CreatedAt = DateTime.UtcNow
                    };
                    newChucVus.Add(cv);
                    chucVuMap[lowerName] = cv;
                }
            }

            foreach (var dvName in inputDonVis)
            {
                var lowerName = dvName.ToLower();
                if (!donViMap.ContainsKey(lowerName))
                {
                    var dv = new DonVi
                    {
                        Id = Guid.NewGuid(),
                        TenDonVi = dvName,
                        CreatedAt = DateTime.UtcNow
                    };
                    newDonVis.Add(dv);
                    donViMap[lowerName] = dv;
                }
            }

            foreach (var rName in inputRoles)
            {
                var lowerName = rName.ToLower();
                if (!roleMap.ContainsKey(lowerName))
                {
                    var role = new Role
                    {
                        Id = Guid.NewGuid(),
                        Name = rName,
                        Description = $"Mô tả cho vai trò {rName} (Tạo tự động khi import)",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    newRoles.Add(role);
                    roleMap[lowerName] = role;
                }
            }

            if (newPhongBans.Any())
            {
                _dbContext.PhongBans.AddRange(newPhongBans);
            }

            if (newChucVus.Any())
            {
                _dbContext.ChucVus.AddRange(newChucVus);
            }

            if (newDonVis.Any())
            {
                _dbContext.DonVis.AddRange(newDonVis);
            }

            if (newRoles.Any())
            {
                _dbContext.Roles.AddRange(newRoles);
            }

            if (newPhongBans.Any() || newChucVus.Any() || newDonVis.Any() || newRoles.Any())
            {
                await _dbContext.SaveChangesAsync();
            }

            // Bước 3: Tiến hành Upsert các User (Check trùng mã user theo PGD / Phòng ban)
            var inputUsernamesList = dtos.Select(d => d.Username.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var existingUsers = await _dbContext.Users
                .Where(u => inputUsernamesList.Contains(u.Username))
                .ToListAsync();

            var existingUserMap = existingUsers
                .GroupBy(u => $"{u.Username.Trim().ToLower()}@{(u.TenPhongBan?.Trim()?.ToLower() ?? "")}")
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

                    Guid? idPhongBan = dto.IdPhongBan;
                    string? tenPhongBan = null;
                    if (!string.IsNullOrWhiteSpace(dto.TenPhongBan))
                    {
                        var key = dto.TenPhongBan.Trim().ToLower();
                        if (phongBanMap.TryGetValue(key, out var pb))
                        {
                            idPhongBan = pb.Id;
                            tenPhongBan = pb.TenPhongBan;
                        }
                    }

                    var phongBanName = tenPhongBan ?? dto.TenPhongBan?.Trim() ?? string.Empty;
                    var userPgdKey = $"{lowerUsername}@{phongBanName.ToLower()}";

                    // Format email theo username: username + @co-opbank.vn
                    var email = string.IsNullOrWhiteSpace(dto.Email) 
                        ? $"{lowerUsername}@co-opbank.vn" 
                        : dto.Email.Trim();

                    Guid? idChucVu = dto.IdChucVu;
                    string? tenChucVu = null;
                    if (!string.IsNullOrWhiteSpace(dto.TenChucVu))
                    {
                        var key = dto.TenChucVu.Trim().ToLower();
                        if (chucVuMap.TryGetValue(key, out var cv))
                        {
                            idChucVu = cv.Id;
                            tenChucVu = cv.TenChucVu;
                        }
                    }

                    Guid? idDonVi = dto.IdDonVi;
                    string? tenDonVi = null;
                    if (!string.IsNullOrWhiteSpace(dto.TenDonVi))
                    {
                        var key = dto.TenDonVi.Trim().ToLower();
                        if (donViMap.TryGetValue(key, out var dv))
                        {
                            idDonVi = dv.Id;
                            tenDonVi = dv.TenDonVi;
                        }
                    }

                    Role? targetRole = null;
                    if (!string.IsNullOrWhiteSpace(dto.Role))
                    {
                        var roleKey = dto.Role.Trim().ToLower();
                        if (roleMap.TryGetValue(roleKey, out var role))
                        {
                            targetRole = role;
                        }
                    }

                    if (existingUserMap.TryGetValue(userPgdKey, out var user))
                    {
                        // Update
                        user.FullName = dto.FullName?.Trim() ?? string.Empty;
                        user.Email = email;
                        user.Phone = dto.Phone?.Trim();
                        user.IdPhongBan = idPhongBan;
                        user.TenPhongBan = phongBanName;
                        user.IdChucVu = idChucVu;
                        user.TenChucVu = tenChucVu;
                        user.IdDonVi = idDonVi;
                        user.TenDonVi = tenDonVi ?? dto.TenDonVi?.Trim();
                        user.IsActive = dto.IsActive;
                        user.IsSystemAdmin = dto.IsSystemAdmin;
                        user.UpdatedAt = DateTime.UtcNow;

                        // Cập nhật Role chỉ khi targetRole không null
                        if (targetRole != null)
                        {
                            var currentRoles = existingUserRoles.Where(ur => ur.UserId == user.Id).ToList();
                            if (currentRoles.Any())
                            {
                                _dbContext.UserRoles.RemoveRange(currentRoles);
                            }
                            _dbContext.UserRoles.Add(new UserRole
                            {
                                UserId = user.Id,
                                RoleId = targetRole.Id,
                                CreatedAt = DateTime.UtcNow
                            });
                        }

                        updatedCount++;
                    }
                    else
                    {
                        // Insert
                        var newUser = new User
                        {
                            Id = Guid.NewGuid(),
                            Username = trimmedUsername,
                            FullName = dto.FullName?.Trim() ?? string.Empty,
                            Email = email,
                            Phone = dto.Phone?.Trim(),
                            IdPhongBan = idPhongBan,
                            TenPhongBan = phongBanName,
                            IdChucVu = idChucVu,
                            TenChucVu = tenChucVu,
                            IdDonVi = idDonVi,
                            TenDonVi = tenDonVi ?? dto.TenDonVi?.Trim(),
                            IsActive = dto.IsActive,
                            IsSystemAdmin = dto.IsSystemAdmin,
                            IsTwoFactorEnabled = false,
                            CreatedAt = DateTime.UtcNow
                        };

                        _dbContext.Users.Add(newUser);
                        existingUserMap[userPgdKey] = newUser;

                        if (targetRole != null)
                        {
                            _dbContext.UserRoles.Add(new UserRole
                            {
                                UserId = newUser.Id,
                                RoleId = targetRole.Id,
                                CreatedAt = DateTime.UtcNow
                            });
                        }

                        addedCount++;
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
