using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace demo1.Data;

public static class CreateFakeDataExtensions
{
    public static async Task CreateFakeDataAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("CreateFakeDataExtensions");

        if (configuration.GetValue<bool>("Database:AutoMigrate") ||
            configuration.GetValue<bool>("Database:SeedSampleData"))
        {
            try
            {
                // Thử kết nối với DB trước (Retry 3 lần)
                int maxRetries = 3;
                int retryDelayMs = 2000;
                bool connected = false;

                for (int i = 1; i <= maxRetries; i++)
                {
                    try
                    {
                        if (await context.Database.CanConnectAsync())
                        {
                            connected = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "Lần {Attempt}/{MaxRetries}: Không thể kết nối Database, thử lại sau {Delay}ms...", i, maxRetries, retryDelayMs);
                    }

                    if (i < maxRetries)
                    {
                        await Task.Delay(retryDelayMs);
                    }
                }

                if (!connected)
                {
                    logger?.LogError("Không thể kết nối Database sau {MaxRetries} lần thử. Server vẫn sẽ tiếp tục khởi chạy mà không thực hiện AutoMigrate/SeedData.", maxRetries);
                    return;
                }

                await context.Database.MigrateAsync();

                // Di chuyển các mã tính năng cũ sang mã Tiếng Việt mới để tránh mất quyền của người dùng hiện có
                var existingFeatures = await context.Features.ToListAsync();
                if (existingFeatures.Any())
                {
                    var updated = false;
                    foreach (var f in existingFeatures)
                    {
                        if (f.Code == "PROJECT") { f.Code = "DU_AN"; updated = true; }
                        else if (f.Code == "BID_PACKAGE") { f.Code = "GOI_THAU"; updated = true; }
                        else if (f.Code == "CONTRACT" || f.Code == "CONTRACT_MANAGEMENT") { f.Code = "QUAN_LY_HOP_DONG"; updated = true; }
                        else if (f.Code == "PARTNER") { f.Code = "DOI_TAC"; updated = true; }
                        else if (f.Code == "RESOLUTION") { f.Code = "NGHI_QUYET"; updated = true; }
                    }
                    if (updated)
                    {
                        await context.SaveChangesAsync();
                        logger?.LogInformation("Đã chuyển đổi mã tính năng cũ sang Tiếng Việt trong bảng Features.");
                    }
                }

                var existingPermissions = await context.UserPermissions.ToListAsync();
                if (existingPermissions.Any())
                {
                    var updated = false;
                    foreach (var up in existingPermissions)
                    {
                        if (up.FeatureCode == "PROJECT") { up.FeatureCode = "DU_AN"; updated = true; }
                        else if (up.FeatureCode == "BID_PACKAGE") { up.FeatureCode = "GOI_THAU"; updated = true; }
                        else if (up.FeatureCode == "CONTRACT" || up.FeatureCode == "CONTRACT_MANAGEMENT") { up.FeatureCode = "QUAN_LY_HOP_DONG"; updated = true; }
                        else if (up.FeatureCode == "PARTNER") { up.FeatureCode = "DOI_TAC"; updated = true; }
                        else if (up.FeatureCode == "RESOLUTION") { up.FeatureCode = "NGHI_QUYET"; updated = true; }
                    }
                    if (updated)
                    {
                        await context.SaveChangesAsync();
                        logger?.LogInformation("Đã chuyển đổi mã tính năng cũ sang Tiếng Việt trong bảng UserPermissions.");
                    }
                }

                var existingRequests = await context.PermissionRequests.ToListAsync();
                if (existingRequests.Any())
                {
                    var updated = false;
                    foreach (var pr in existingRequests)
                    {
                        if (pr.FeatureCode == "PROJECT") { pr.FeatureCode = "DU_AN"; updated = true; }
                        else if (pr.FeatureCode == "BID_PACKAGE") { pr.FeatureCode = "GOI_THAU"; updated = true; }
                        else if (pr.FeatureCode == "CONTRACT" || pr.FeatureCode == "CONTRACT_MANAGEMENT") { pr.FeatureCode = "QUAN_LY_HOP_DONG"; updated = true; }
                        else if (pr.FeatureCode == "PARTNER") { pr.FeatureCode = "DOI_TAC"; updated = true; }
                        else if (pr.FeatureCode == "RESOLUTION") { pr.FeatureCode = "NGHI_QUYET"; updated = true; }
                    }
                    if (updated)
                    {
                        await context.SaveChangesAsync();
                        logger?.LogInformation("Đã chuyển đổi mã tính năng cũ sang Tiếng Việt trong bảng PermissionRequests.");
                    }
                }

                var existingNotifications = await context.Notifications.Where(n => string.IsNullOrEmpty(n.FeatureCode)).ToListAsync();
                if (existingNotifications.Any())
                {
                    foreach (var n in existingNotifications)
                    {
                        var titleUpper = (n.Title ?? string.Empty).ToUpper();
                        var contentUpper = (n.Content ?? string.Empty).ToUpper();
                        var linkUpper = (n.Link ?? string.Empty).ToUpper();

                        if (titleUpper.Contains("HỢP ĐỒNG") || contentUpper.Contains("HỢP ĐỒNG") || linkUpper.Contains("HOP-DONG"))
                        {
                            n.FeatureCode = "QUAN_LY_HOP_DONG";
                        }
                        else if (titleUpper.Contains("THANH TOÁN") || contentUpper.Contains("THANH TOÁN") || linkUpper.Contains("THANH-TOAN"))
                        {
                            n.FeatureCode = "QUAN_LY_HOP_DONG";
                        }
                        else if (titleUpper.Contains("GÓI THẦU") || contentUpper.Contains("GÓI THẦU") || linkUpper.Contains("GOI-THAU"))
                        {
                            n.FeatureCode = "GOI_THAU";
                        }
                        else if (titleUpper.Contains("DỰ ÁN") || contentUpper.Contains("DỰ ÁN") || linkUpper.Contains("DU-AN"))
                        {
                            n.FeatureCode = "DU_AN";
                        }
                        else if (titleUpper.Contains("CÔNG VIỆC") || contentUpper.Contains("CÔNG VIỆC") || linkUpper.Contains("CONG-VIEC"))
                        {
                            n.FeatureCode = "CONG_VIEC";
                        }
                        else if (titleUpper.Contains("QUYỀN") || contentUpper.Contains("QUYỀN") || linkUpper.Contains("PERMISSION"))
                        {
                            n.FeatureCode = "PERMISSION_REQUEST";
                        }
                        else
                        {
                            n.FeatureCode = "SYSTEM";
                        }
                    }
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Đã bổ sung mã tính năng (FeatureCode) cho các thông báo cũ trong CSDL.");
                }

                var existingAttachments = await context.FileAttachments.ToListAsync();
                if (existingAttachments.Any())
                {
                    var updated = false;
                    foreach (var fa in existingAttachments)
                    {
                        if (fa.EntityType == "PROJECT") { fa.EntityType = "DU_AN"; updated = true; }
                        else if (fa.EntityType == "BID_PACKAGE") { fa.EntityType = "GOI_THAU"; updated = true; }
                        else if (fa.EntityType == "CONTRACT" || fa.EntityType == "CONTRACT_MANAGEMENT") { fa.EntityType = "QUAN_LY_HOP_DONG"; updated = true; }
                        else if (fa.EntityType == "PARTNER") { fa.EntityType = "DOI_TAC"; updated = true; }
                        else if (fa.EntityType == "RESOLUTION") { fa.EntityType = "NGHI_QUYET"; updated = true; }
                    }
                    if (updated)
                    {
                        await context.SaveChangesAsync();
                        logger?.LogInformation("Đã chuyển đổi EntityType cũ sang Tiếng Việt trong bảng FileAttachments.");
                    }
                }

                if (!context.Features.Any())
                {
                    // 1. Seed Features
                    var features = new List<Feature>
                    {
                        new() { Code = "DU_AN", Name = "Quản lý dự án", Description = "Chức năng xem, thêm, sửa, xoá dự án" },
                        new() { Code = "GOI_THAU", Name = "Quản lý gói thầu", Description = "Chức năng xem, thêm, sửa, xoá gói thầu" },
                        new() { Code = "QUAN_LY_HOP_DONG", Name = "Quản lý hợp đồng", Description = "Chức năng xem, thêm, sửa, xoá hợp đồng" },
                        new() { Code = "DOI_TAC", Name = "Quản lý đối tác", Description = "Chức năng xem, thêm, sửa, xoá đối tác" },
                        new() { Code = "NGHI_QUYET", Name = "Quản lý nghị quyết/văn bản", Description = "Chức năng xem, thêm, sửa, xoá nghị quyết" }
                    };
                    context.Features.AddRange(features);
                    await context.SaveChangesAsync();

                    // 2. Seed Roles
                    var adminRole = new Role { Name = "Admin", Description = "Quyền quản trị toàn hệ thống" };
                    var managerRole = new Role { Name = "Manager", Description = "Quản lý dự án, hợp đồng" };
                    var staffRole = new Role { Name = "Staff", Description = "Nhân viên xem và cập nhật thông tin" };
                    context.Roles.AddRange(adminRole, managerRole, staffRole);
                    await context.SaveChangesAsync();

                    // 3. Seed Admin User
                    var adminUser = new User
                    {
                        Username = "admin",
                        FullName = "System Administrator",
                        IsActive = true,
                        IsSystemAdmin = true
                    };
                    var normalUser = new User
                    {
                        Username = "quangmd",
                        FullName = "Mai Duy Quang",
                        IsActive = true,
                        IsSystemAdmin = true
                    };
                    context.Users.AddRange(adminUser, normalUser);
                    await context.SaveChangesAsync();

                    context.UserRoles.Add(new UserRole
                    {
                        UserId = normalUser.Id,
                        RoleId = adminRole.Id
                    });
                    await context.SaveChangesAsync();

                    if (!context.Users.Any(u => u.Username == "anhld2"))
                    {
                        var anhldUser = new User
                        {
                            Id = Guid.NewGuid(),
                            Username = "anhld2",
                            FullName = "Lê Đức Anh",
                            IsActive = true,
                            IsSystemAdmin = true,
                            IsTwoFactorEnabled = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        context.Users.Add(anhldUser);
                        await context.SaveChangesAsync();
                    }
                }

                // Seed/Sync Default ChucVus (TGD, GD, PGD, TP, PP, CV)
                var defaultPositions = new List<(string Code, string Name, int Level)>
                {
                    ("TGD", "Tổng giám đốc", 1),
                    ("GD", "Giám đốc", 2),
                    ("PGD", "Phó giám đốc", 3),
                    ("TP", "Trưởng phòng", 4),
                    ("PP", "Phó phòng", 5),
                    ("CV", "Chuyên viên", 6)
                };

                foreach (var pos in defaultPositions)
                {
                    var existingByCode = await context.ChucVus.FirstOrDefaultAsync(cv => cv.Code != null && cv.Code.ToUpper() == pos.Code);
                    if (existingByCode != null)
                    {
                        existingByCode.Level = pos.Level;
                        existingByCode.TenChucVu = pos.Name;
                    }
                    else
                    {
                        var existingByName = await context.ChucVus.FirstOrDefaultAsync(cv => 
                            cv.TenChucVu.ToLower() == pos.Name.ToLower() ||
                            (pos.Code == "TGD" && cv.TenChucVu.ToLower() == "tổng giám đốc") ||
                            (pos.Code == "GD" && cv.TenChucVu.ToLower() == "giám đốc") ||
                            (pos.Code == "PGD" && cv.TenChucVu.ToLower() == "phó giám đốc") ||
                            (pos.Code == "TP" && cv.TenChucVu.ToLower() == "trưởng phòng") ||
                            (pos.Code == "PP" && (cv.TenChucVu.ToLower() == "phó phòng" || cv.TenChucVu.ToLower() == "phó trưởng phòng")) ||
                            (pos.Code == "CV" && cv.TenChucVu.ToLower() == "chuyên viên"));

                        if (existingByName != null)
                        {
                            existingByName.Code = pos.Code;
                            existingByName.Level = pos.Level;
                        }
                        else
                        {
                            context.ChucVus.Add(new ChucVu
                            {
                                Id = Guid.NewGuid(),
                                TenChucVu = pos.Name,
                                Code = pos.Code,
                                Level = pos.Level,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
                await context.SaveChangesAsync();

                if (configuration.GetValue<bool>("Database:SeedSampleData"))
                {
                    await DatabaseSeeder.SeedAsync(context);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Đã xảy ra ngoại lệ khi kết nối hoặc khởi tạo dữ liệu Database. Khởi chạy Server vẫn sẽ tiếp tục.");
            }
        }
    }
}
