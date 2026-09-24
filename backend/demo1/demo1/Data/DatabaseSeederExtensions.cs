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

public static class DatabaseSeederExtensions
{
    /// <summary>
    /// Đồng bộ Master Data chuẩn cho hệ thống (Roles, Features, Permissions, Chức vụ, Nguồn vốn, Loại HĐ...).
    /// Chỉ thực hiện nếu Database:SeedMasterData = true.
    /// </summary>
    public static async Task SeedDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("DatabaseSeeder");

        var seedMasterData = configuration.GetValue<bool>("Database:SeedMasterData", true);
        var seedSampleData = configuration.GetValue<bool>("Database:SeedSampleData", false);

        if (!seedMasterData && !seedSampleData)
        {
            logger?.LogInformation("Cả Database:SeedMasterData và SeedSampleData đều tắt. Bỏ qua Seeding.");
            return;
        }

        try
        {
            if (!await context.Database.CanConnectAsync())
            {
                logger?.LogWarning("Không thể kết nối CSDL để thực hiện Seeding.");
                return;
            }

            // 1. Chuẩn hóa/chuyển đổi dữ liệu cũ nếu có
            await NormalizeLegacyDataAsync(context, logger);

            // 2. Đồng bộ Master Data chuẩn cho Production (Nguồn vốn, Loại dự án, Loại HĐ, Nhóm dự án, Chức vụ, Phòng ban, Features, Roles, Permissions)
            if (seedMasterData)
            {
                await ProductionSeeder.SeedProductionDataAsync(context, logger);
                await SeedDefaultCatalogsAsync(context, logger);
            }

            // 3. Seed dữ liệu mẫu nếu được bật (dành riêng cho Dev/Demo)
            if (seedSampleData)
            {
                logger?.LogInformation("Database:SeedSampleData = true. Tiến hành khởi tạo dữ liệu mẫu...");
                // Thêm các logic dữ liệu mẫu nếu cần
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Đã xảy ra ngoại lệ khi khởi tạo / đồng bộ dữ liệu danh mục Master Data.");
        }
    }

    private static async Task NormalizeLegacyDataAsync(AppDbContext context, ILogger? logger)
    {
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

        var allNotifications = await context.Notifications.ToListAsync();
        bool hasCleanedContent = false;
        foreach (var n in allNotifications)
        {
            var cleaned = demo1.Controllers.NotificationController.CleanNotificationContent(n.Content);
            if (cleaned != n.Content)
            {
                n.Content = cleaned;
                hasCleanedContent = true;
            }
        }
        if (hasCleanedContent)
        {
            await context.SaveChangesAsync();
            logger?.LogInformation("Đã làm sạch trường Content của các thông báo.");
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
    }

    private static async Task SeedDefaultCatalogsAsync(AppDbContext context, ILogger? logger)
    {
        // 1. Seed Admin Users mặc định nếu chưa tồn tại
        if (!await context.Users.AnyAsync(u => u.Username == "admin"))
        {
            context.Users.Add(new User
            {
                Username = "admin",
                FullName = "System Administrator",
                IsActive = true,
                IsSystemAdmin = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        if (!await context.Users.AnyAsync(u => u.Username == "quangmd"))
        {
            var normalUser = new User
            {
                Username = "quangmd",
                FullName = "Mai Duy Quang",
                IsActive = true,
                IsSystemAdmin = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(normalUser);
            await context.SaveChangesAsync();

            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole != null)
            {
                context.UserRoles.Add(new UserRole
                {
                    UserId = normalUser.Id,
                    RoleId = adminRole.Id
                });
            }
        }
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
        }
        if (!context.Users.Any(u => u.Username == "anhlt"))
        {
            var anhltUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "anhlt",
                FullName = "Lê Tuấn Anh",
                IsActive = true,
                IsSystemAdmin = true,
                IsTwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(anhltUser);
        }
        await context.SaveChangesAsync();

        // 2. Seed Đơn vị tính
        var defaultDonViTinhs = new List<(string Code, string Name)>
        {
            ("CAI", "Cái"),
            ("BO", "Bộ"),
            ("GOI", "Gói"),
            ("NAM", "Năm"),
            ("THANG", "Tháng"),
            ("LUOT", "Lượt"),
            ("HE_THONG", "Hệ thống"),
            ("LICENSE", "License"),
            ("CHIEC", "Chiếc"),
            ("THIET_BI", "Thiết bị")
        };
        foreach (var dvt in defaultDonViTinhs)
        {
            if (!await context.DonViTinhs.AnyAsync(x => x.Code == dvt.Code || x.Name == dvt.Name))
            {
                context.DonViTinhs.Add(new demo1.Entity.DanhMuc.DonViTinh
                {
                    Id = Guid.NewGuid(),
                    Code = dvt.Code,
                    Name = dvt.Name,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // 3. Seed Hãng sản xuất
        var defaultHangSanXuats = new List<(string Code, string Name)>
        {
            ("MICROSOFT", "Microsoft"),
            ("ORACLE", "Oracle"),
            ("CISCO", "Cisco Systems"),
            ("DELL", "Dell Technologies"),
            ("HP", "HP Inc / HPE"),
            ("VMWARE", "VMware"),
            ("IBM", "IBM"),
            ("FORTINET", "Fortinet"),
            ("PALO_ALTO", "Palo Alto Networks"),
            ("APPLE", "Apple"),
            ("SAMSUNG", "Samsung"),
            ("LENOVO", "Lenovo")
        };
        foreach (var hsx in defaultHangSanXuats)
        {
            if (!await context.HangSanXuats.AnyAsync(x => x.Code == hsx.Code || x.Name == hsx.Name))
            {
                context.HangSanXuats.Add(new demo1.Entity.DanhMuc.HangSanXuat
                {
                    Id = Guid.NewGuid(),
                    Code = hsx.Code,
                    Name = hsx.Name,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // 4. Seed Xuất xứ
        var defaultXuatXus = new List<(string Code, string Name)>
        {
            ("VN", "Việt Nam"),
            ("US", "Mỹ (USA)"),
            ("JP", "Nhật Bản"),
            ("KR", "Hàn Quốc"),
            ("DE", "Đức"),
            ("SG", "Singapore"),
            ("CN", "Trung Quốc"),
            ("TW", "Đài Loan")
        };
        foreach (var xx in defaultXuatXus)
        {
            if (!await context.XuatXus.AnyAsync(x => x.Code == xx.Code || x.Name == xx.Name))
            {
                context.XuatXus.Add(new demo1.Entity.DanhMuc.XuatXu
                {
                    Id = Guid.NewGuid(),
                    Code = xx.Code,
                    Name = xx.Name,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
