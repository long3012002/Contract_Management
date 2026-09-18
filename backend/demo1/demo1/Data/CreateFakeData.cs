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

                // Loại bỏ tiền tố tên chức năng ở đầu trường Content (ví dụ: "[Quản lý Hợp đồng] ...") cho toàn bộ thông báo trong CSDL
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
                    logger?.LogInformation("Đã làm sạch trường Content của các thông báo (loại bỏ tên chức năng ở đầu).");
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

                // 1. Seed/Sync Features & Catalog
                await ProductionSeeder.SeedSystemCatalogAsync(context, logger);

                // 2. Seed/Sync Default Roles
                var defaultRoles = new List<(string Name, string Description)>
                {
                    ("Admin", "Quyền quản trị toàn hệ thống"),
                    ("Manager", "Quản lý dự án, hợp đồng"),
                    ("Staff", "Nhân viên xem và cập nhật thông tin")
                };

                foreach (var r in defaultRoles)
                {
                    if (!await context.Roles.AnyAsync(x => x.Name == r.Name))
                    {
                        context.Roles.Add(new Role
                        {
                            Id = Guid.NewGuid(),
                            Name = r.Name,
                            Description = r.Description
                        });
                    }
                }
                await context.SaveChangesAsync();

                // 2b. Seed/Sync Default LoaiHopDongs
                var defaultLoaiHopDongs = new List<(string Code, string Name, string Description)>
                {
                    ("01", "Bảo trì", "Hợp đồng bảo trì (Mặc định)"),
                    ("02", "Mua sắm phần cứng", "Hợp đồng mua sắm thiết bị, phần cứng"),
                    ("03", "Bản quyền phần mềm", "Hợp đồng mua sắm bản quyền, phần mềm"),
                    ("04", "Tư vấn", "Hợp đồng tư vấn (lập dự án, thẩm định, giám sát)"),
                    ("05", "Thuê dịch vụ", "Hợp đồng thuê dịch vụ (đường truyền, cloud, server)"),
                    ("99", "Khác", "Các loại hợp đồng khác")
                };

                foreach (var lhd in defaultLoaiHopDongs)
                {
                    if (!await context.LoaiHopDongs.AnyAsync(x => x.Code == lhd.Code || x.Name == lhd.Name))
                    {
                        context.LoaiHopDongs.Add(new LoaiHopDong
                        {
                            Id = Guid.NewGuid(),
                            Code = lhd.Code,
                            Name = lhd.Name,
                            Description = lhd.Description,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                await context.SaveChangesAsync();

                // 2c. Seed/Sync Default Permissions
                var defaultPermissions = new List<(string Code, string Name, string Description)>
                {
                    ("VIEW", "Xem", "Quyền xem dữ liệu"),
                    ("CREATE", "Tạo mới", "Quyền tạo mới dữ liệu"),
                    ("EDIT", "Chỉnh sửa", "Quyền chỉnh sửa bản ghi"),
                    ("DELETE", "Xóa", "Quyền xóa bản ghi"),
                    ("APPROVE", "Phê duyệt", "Quyền phê duyệt yêu cầu")
                };

                foreach (var p in defaultPermissions)
                {
                    if (!await context.Permissions.AnyAsync(x => x.Code == p.Code))
                    {
                        context.Permissions.Add(new Permission
                        {
                            Id = Guid.NewGuid(),
                            Code = p.Code,
                            Name = p.Name,
                            Description = p.Description,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                await context.SaveChangesAsync();

                // 3. Seed Admin Users
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
                        await context.SaveChangesAsync();
                    }
                await context.SaveChangesAsync();

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
                // Seed/Sync Default NguonVons
                var defaultNguonVons = new List<(string Code, string Name)>
                {
                    ("NV_NHHT", "Chi phí của NHHT"),
                    ("NV_CN", "Chi phí tại chi nhánh"),
                    ("NV_KHAC", "Nguồn khác"),
                    ("NV_QPL", "Quỹ phúc lợi"),
                    ("NV_QDTPT", "Quỹ đầu tư phát triển"),
                    ("NV_VDL_QDTR", "Vốn điều lệ và Quỹ dự trữ bổ sung vốn điều lệ")
                };

                foreach (var nv in defaultNguonVons)
                {
                    if (!await context.NguonVons.AnyAsync(x => x.Code == nv.Code || x.Name == nv.Name))
                    {
                        context.NguonVons.Add(new demo1.Entity.DanhMuc.NguonVon
                        {
                            Id = Guid.NewGuid(),
                            Code = nv.Code,
                            Name = nv.Name,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                // Seed/Sync Default NhomDuAns
                var defaultNhomDuAns = new List<(string Code, string Name)>
                {
                    ("NHOM_A", "Các dự án nhóm A"),
                    ("NHOM_B", "Các dự án nhóm B"),
                    ("NHOM_C", "Các dự án khác (Nhóm C)")
                };

                foreach (var n in defaultNhomDuAns)
                {
                    if (!await context.NhomDuAns.AnyAsync(x => x.Code == n.Code))
                    {
                        context.NhomDuAns.Add(new demo1.Entity.DanhMuc.NhomDuAn
                        {
                            Id = Guid.NewGuid(),
                            Code = n.Code,
                            Name = n.Name,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                // Seed/Sync Default PhanLoaiDuAns (Loại dự án)
                var defaultPhanLoaiDuAns = new List<(string Code, string Name, string Description)>
                {
                    ("PL_CNTT", "Dự án Công nghệ thông tin", "Dự án đầu tư hạ tầng, phần mềm và giải pháp CNTT"),
                    ("PL_XDCB", "Dự án Xây dựng cơ bản & Bảo trì", "Sửa chữa, cải tạo trụ sở, phòng giao dịch"),
                    ("PL_MSHH_DV", "Mua sắm hàng hóa & Dịch vụ", "Trang thiết bị văn phòng, dịch vụ tư vấn..."),
                    ("PL_DIGITAL_BANKING", "Dự án Ngân hàng số & Thẻ", "Core Banking, eBiz, Chatbot, Thẻ..."),
                    ("PL_SECURITY", "Dự án An toàn thông tin & Bảo mật", "Bảo mật mạng, SOC, An ninh thông tin..."),
                    ("PL_KHAC", "Dự án / Phân loại khác", "Các loại dự án khác")
                };

                foreach (var pl in defaultPhanLoaiDuAns)
                {
                    if (!await context.PhanLoaiDuAns.AnyAsync(x => x.Code == pl.Code))
                    {
                        context.PhanLoaiDuAns.Add(new demo1.Entity.DanhMuc.PhanLoaiDuAn
                        {
                            Id = Guid.NewGuid(),
                            Code = pl.Code,
                            Name = pl.Name,
                            Description = pl.Description,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                await context.SaveChangesAsync();

                // Seed/Sync Default PhongBans
                var defaultPhongBans = new[]
                {
                    "Phòng CNTT",
                    "Phòng Kế hoạch",
                    "Phòng Tài chính",
                    "Phòng Nhân sự",
                    "Phòng Pháp chế",
                    "Phòng Kinh doanh",
                    "Phòng Dự án",
                    "Phòng Kỹ thuật",
                    "Phòng R&D",
                    "Phòng Giám sát"
                };

                foreach (var pbName in defaultPhongBans)
                {
                    if (!await context.PhongBans.AnyAsync(x => x.TenPhongBan == pbName))
                    {
                        context.PhongBans.Add(new demo1.Entity.DanhMuc.PhongBan
                        {
                            Id = Guid.NewGuid(),
                            TenPhongBan = pbName,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                await context.SaveChangesAsync();

                // Seed/Sync Default DonViTinhs
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

                // Seed/Sync Default HangSanXuats
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

                // Seed/Sync Default XuatXus
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
            catch (Exception ex)
            {
                logger?.LogError(ex, "Đã xảy ra ngoại lệ khi kết nối hoặc khởi tạo dữ liệu Database. Khởi chạy Server vẫn sẽ tiếp tục.");
            }
        }
    }
}
