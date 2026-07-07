using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;
using demo1.Data;
using demo1.Entity;

namespace demo1.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public UserController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 1. Hàm Thêm/Cập nhật nhiều (Bulk Upsert Users)
        [HttpPost("bulk-create")]
        public async Task<IActionResult> AddMultiple([FromBody] List<CreateUserDto> dtos)
        {
            if (dtos == null || !dtos.Any())
            {
                return BadRequest(new { Message = "Danh sách người dùng không được để trống." });
            }

            var dbRoles = await _dbContext.Roles.ToListAsync();
            var roleMap = dbRoles.ToDictionary(r => r.Name.ToLower(), r => r);

            // Bước 1: Validate dữ liệu từng dòng
            var errors = new List<object>();
            var usernamesInInput = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
                    if (usernamesInInput.Contains(trimmedUsername))
                    {
                        rowErrors.Add($"Username '{trimmedUsername}' bị trùng lặp trong danh sách import.");
                    }
                    else
                    {
                        usernamesInInput.Add(trimmedUsername);
                    }
                }

                if (string.IsNullOrWhiteSpace(dto.FullName))
                {
                    rowErrors.Add("Họ tên không được để trống.");
                }

                if (!string.IsNullOrWhiteSpace(dto.Role))
                {
                    var roleNameLower = dto.Role.Trim().ToLower();
                    if (!roleMap.ContainsKey(roleNameLower))
                    {
                        rowErrors.Add($"Role '{dto.Role}' không hợp lệ hoặc không tồn tại trong hệ thống.");
                    }
                }
                else
                {
                    rowErrors.Add("Role không được để trống.");
                }

                if (rowErrors.Any())
                {
                    errors.Add(new
                    {
                        RowIndex = rowNum,
                        Username = dto?.Username ?? string.Empty,
                        ErrorMessages = rowErrors
                    });
                }
            }

            if (errors.Any())
            {
                return BadRequest(new
                {
                    Message = "Dữ liệu import không hợp lệ.",
                    AddedCount = 0,
                    UpdatedCount = 0,
                    ErrorCount = errors.Count,
                    Errors = errors
                });
            }

            // Bước 2: Tự động xử lý Phòng ban và Chức vụ (tạo mới nếu chưa tồn tại)
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

            var dbPhongBans = await _dbContext.PhongBans.ToListAsync();
            var dbChucVus = await _dbContext.ChucVus.ToListAsync();

            var phongBanMap = dbPhongBans.ToDictionary(pb => pb.TenPhongBan.ToLower(), pb => pb);
            var chucVuMap = dbChucVus.ToDictionary(cv => cv.TenChucVu.ToLower(), cv => cv);

            var newPhongBans = new List<PhongBan>();
            var newChucVus = new List<ChucVu>();

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

            if (newPhongBans.Any())
            {
                _dbContext.PhongBans.AddRange(newPhongBans);
                
                // Thêm quyền mặc định cho phòng ban mới
                var activeFeatures = await _dbContext.Features.Where(f => f.IsActive).ToListAsync();
                foreach (var pb in newPhongBans)
                {
                    foreach (var feat in activeFeatures)
                    {
                        _dbContext.PhongBanPermissions.Add(new PhongBanPermission
                        {
                            PhongBanId = pb.Id,
                            FeatureId = feat.Id,
                            CanAccess = true,
                            Permissions = "Create;Update;Delete",
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            if (newChucVus.Any())
            {
                _dbContext.ChucVus.AddRange(newChucVus);
            }

            if (newPhongBans.Any() || newChucVus.Any())
            {
                await _dbContext.SaveChangesAsync();
            }

            // Bước 3: Tiến hành Upsert các User
            var inputUsernamesList = usernamesInInput.ToList();
            var existingUsers = await _dbContext.Users
                .Where(u => inputUsernamesList.Contains(u.Username))
                .ToListAsync();
            var existingUserMap = existingUsers.ToDictionary(u => u.Username.ToLower(), u => u);

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

                    // Format email theo username: username + @co-opbank.vn
                    var email = string.IsNullOrWhiteSpace(dto.Email) 
                        ? $"{lowerUsername}@co-opbank.vn" 
                        : dto.Email.Trim();

                    Guid? idPhongBan = null;
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

                    Guid? idChucVu = null;
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

                    var targetRole = roleMap[dto.Role!.Trim().ToLower()];

                    if (existingUserMap.TryGetValue(lowerUsername, out var user))
                    {
                        // Update
                        user.FullName = dto.FullName.Trim();
                        user.Email = email;
                        user.Phone = dto.Phone?.Trim();
                        user.IdPhongBan = idPhongBan;
                        user.TenPhongBan = tenPhongBan;
                        user.IdChucVu = idChucVu;
                        user.TenChucVu = tenChucVu;
                        user.IsActive = dto.IsActive;
                        user.IsSystemAdmin = dto.IsSystemAdmin;
                        user.UpdatedAt = DateTime.UtcNow;

                        _dbContext.Users.Update(user);

                        // Cập nhật Role
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

                        updatedCount++;
                    }
                    else
                    {
                        // Insert
                        var newUser = new User
                        {
                            Id = Guid.NewGuid(),
                            Username = trimmedUsername,
                            FullName = dto.FullName.Trim(),
                            Email = email,
                            Phone = dto.Phone?.Trim(),
                            IdPhongBan = idPhongBan,
                            TenPhongBan = tenPhongBan,
                            IdChucVu = idChucVu,
                            TenChucVu = tenChucVu,
                            IsActive = dto.IsActive,
                            IsSystemAdmin = dto.IsSystemAdmin,
                            IsTwoFactorEnabled = false,
                            CreatedAt = DateTime.UtcNow
                        };

                        _dbContext.Users.Add(newUser);

                        _dbContext.UserRoles.Add(new UserRole
                        {
                            UserId = newUser.Id,
                            RoleId = targetRole.Id,
                            CreatedAt = DateTime.UtcNow
                        });

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

            return Ok(new
            {
                Message = "Import dữ liệu thành công.",
                AddedCount = addedCount,
                UpdatedCount = updatedCount,
                ErrorCount = 0,
                Errors = new List<object>()
            });
        }

        // 1b. Hàm Import từ Excel
        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "File không hợp lệ hoặc trống." });
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
            {
                return BadRequest(new { Message = "Chỉ hỗ trợ file Excel (.xlsx, .xls)." });
            }

            try
            {
                List<CreateUserDto> dtos = new();
                using (var stream = file.OpenReadStream())
                {
                    var rows = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>();
                    foreach (var row in rows)
                    {
                        var dto = new CreateUserDto
                        {
                            Username = row.ContainsKey("Username") ? row["Username"]?.ToString() ?? string.Empty : string.Empty,
                            FullName = row.ContainsKey("Họ tên") ? row["Họ tên"]?.ToString() ?? string.Empty : (row.ContainsKey("FullName") ? row["FullName"]?.ToString() ?? string.Empty : string.Empty),
                            Email = row.ContainsKey("Email") ? row["Email"]?.ToString() : null,
                            Phone = row.ContainsKey("Số điện thoại") ? row["Số điện thoại"]?.ToString() : (row.ContainsKey("Phone") ? row["Phone"]?.ToString() : null),
                            TenPhongBan = row.ContainsKey("Phòng ban") ? row["Phòng ban"]?.ToString() : (row.ContainsKey("TenPhongBan") ? row["TenPhongBan"]?.ToString() : null),
                            TenChucVu = row.ContainsKey("Chức vụ") ? row["Chức vụ"]?.ToString() : (row.ContainsKey("TenChucVu") ? row["TenChucVu"]?.ToString() : null),
                            Role = row.ContainsKey("Role") ? row["Role"]?.ToString() : (row.ContainsKey("Vai trò") ? row["Vai trò"]?.ToString() : null),
                            IsActive = true,
                            IsSystemAdmin = false
                        };

                        if (row.ContainsKey("Trạng thái"))
                        {
                            var activeStr = row["Trạng thái"]?.ToString()?.Trim()?.ToLower();
                            if (activeStr == "khóa" || activeStr == "khoa" || activeStr == "0" || activeStr == "false")
                            {
                                dto.IsActive = false;
                            }
                        }

                        if (row.ContainsKey("Admin"))
                        {
                            var adminStr = row["Admin"]?.ToString()?.Trim()?.ToLower();
                            if (adminStr == "có" || adminStr == "co" || adminStr == "1" || adminStr == "true")
                            {
                                dto.IsSystemAdmin = true;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(dto.Username))
                        {
                            dtos.Add(dto);
                        }
                    }
                }

                if (!dtos.Any())
                {
                    return BadRequest(new { Message = "Không tìm thấy dữ liệu người dùng nào hợp lệ trong file Excel." });
                }

                return await AddMultiple(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Đã xảy ra lỗi khi đọc file Excel.", Detail = ex.Message });
            }
        }

        // 2. Hàm Xoá nhiều (Bulk Delete Users)
        [HttpDelete("bulk-delete")]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<Guid> ids)
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest(new { Message = "Danh sách ID cần xoá không được để trống." });
            }

            // Tìm các user có ID trong danh sách
            var usersToDelete = await _dbContext.Users
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            if (!usersToDelete.Any())
            {
                return NotFound(new { Message = "Không tìm thấy người dùng nào phù hợp để xoá." });
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

            return Ok(new
            {
                Message = $"Đã xoá thành công {usersToDelete.Count} người dùng.",
                DeletedCount = usersToDelete.Count
            });
        }
    }

    public class CreateUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? TenPhongBan { get; set; }
        public string? TenChucVu { get; set; }
        public string? Role { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSystemAdmin { get; set; } = false;
    }
}
