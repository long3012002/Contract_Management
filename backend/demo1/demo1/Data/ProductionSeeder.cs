using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Entity;
using demo1.Entity.DanhMuc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace demo1.Data;

public static class ProductionSeeder
{
    /// <summary>
    /// Hàm Seeder chuẩn cho môi trường Production.
    /// Khởi tạo và đồng bộ danh mục chuẩn theo màn hình thực tế: Nguồn vốn, Loại dự án, Loại hợp đồng,
    /// Nhóm dự án, Chức vụ, Phòng ban, Features, Roles, và Permissions.
    /// </summary>
    public static async Task SeedProductionDataAsync(AppDbContext context, ILogger? logger = null)
    {
        logger?.LogInformation("=== [PROD SEEDER] Bắt đầu khởi tạo dữ liệu danh mục chuẩn cho Production ===");

        // 1. Seed / Sync Nguồn vốn
        await SeedNguonVonsAsync(context, logger);

        // 2. Seed / Sync Phân loại dự án (Loại dự án)
        await SeedPhanLoaiDuAnsAsync(context, logger);

        // 3. Seed / Sync Loại hợp đồng
        await SeedLoaiHopDongsAsync(context, logger);

        // 4. Seed / Sync Nhóm dự án
        await SeedNhomDuAnsAsync(context, logger);

        // 5. Seed / Sync Chức vụ
        await SeedChucVusAsync(context, logger);

        // 6. Seed / Sync Phòng ban
        await SeedPhongBansAsync(context, logger);

        // 7. Seed / Sync System Features & Roles & Permissions
        await SeedSystemCatalogAsync(context, logger);

        logger?.LogInformation("=== [PROD SEEDER] Hoàn tất khởi tạo dữ liệu Production ===");
    }

    /// <summary>
    /// 1. Danh mục Nguồn vốn (Chính xác theo hình ảnh màn hình)
    /// </summary>
    public static async Task SeedNguonVonsAsync(AppDbContext context, ILogger? logger = null)
    {
        var defaultNguonVons = new List<(string Code, string Name, string Description)>
        {
            ("NV_VDL_QDTR", "Vốn điều lệ và Quỹ dự trữ bổ sung vốn điều lệ", "Vốn điều lệ và Quỹ dự trữ bổ sung vốn điều lệ"),
            ("NV_QDTPT", "Quỹ đầu tư phát triển", "Quỹ đầu tư phát triển"),
            ("NV_QPL", "Quỹ phúc lợi", "Quỹ phúc lợi"),
            ("NV_KHAC", "Nguồn khác", "Nguồn khác"),
            ("NV_CN", "Chi phí tại chi nhánh", "Chi phí tại chi nhánh"),
            ("NV_NHHT", "Chi phí của NHHT", "Chi phí của NHHT")
        };

        int countAdded = 0;
        foreach (var item in defaultNguonVons)
        {
            var existing = await context.NguonVons.FirstOrDefaultAsync(x => x.Code == item.Code || x.Name == item.Name);
            if (existing == null)
            {
                context.NguonVons.Add(new NguonVon
                {
                    Id = Guid.NewGuid(),
                    Code = item.Code,
                    Name = item.Name,
                    Description = item.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                countAdded++;
            }
            else
            {
                existing.Name = item.Name;
                if (!string.IsNullOrEmpty(item.Description)) existing.Description = item.Description;
                existing.IsActive = true;
            }
        }
        await context.SaveChangesAsync();
        logger?.LogInformation("Đã đồng bộ {Count} bản ghi Nguồn vốn.", countAdded);
    }

    /// <summary>
    /// 2. Danh mục Phân loại dự án (Chính xác theo hình ảnh màn hình)
    /// </summary>
    public static async Task SeedPhanLoaiDuAnsAsync(AppDbContext context, ILogger? logger = null)
    {
        var defaultPhanLoaiDuAns = new List<(string Code, string Name, string Description)>
        {
            ("PL_DIGITAL_BANKING", "Ngân hàng số & Thẻ", "Dự án Ngân hàng số & Thẻ"),
            ("PL_CNTT", "Công nghệ thông tin", "Dự án Công nghệ thông tin"),
            ("PL_HTPM", "Hệ thống phần mềm", "Dự án Hệ thống phần mềm"),
            ("PL_KHAC", "Khác", "Các loại dự án khác"),
            ("PL_SECURITY", "Mạng & An ninh bảo mật", "Dự án Mạng & An ninh bảo mật"),
            ("PL_MSHH_DV", "Mua sắm hàng hóa & Dịch vụ", "Mua sắm hàng hóa & Dịch vụ"),
            ("PL_XDCB", "Xây dựng & Bảo trì", "Dự án Xây dựng & Bảo trì"),
            ("PL_HA_TANG", "Hạ tầng", "Dự án Hạ tầng")
        };

        int countAdded = 0;
        foreach (var item in defaultPhanLoaiDuAns)
        {
            var existing = await context.PhanLoaiDuAns.FirstOrDefaultAsync(x => x.Code == item.Code || x.Name == item.Name);
            if (existing == null)
            {
                context.PhanLoaiDuAns.Add(new PhanLoaiDuAn
                {
                    Id = Guid.NewGuid(),
                    Code = item.Code,
                    Name = item.Name,
                    Description = item.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                countAdded++;
            }
            else
            {
                existing.Name = item.Name;
                existing.Description = item.Description;
                existing.IsActive = true;
            }
        }
        await context.SaveChangesAsync();
        logger?.LogInformation("Đã đồng bộ {Count} bản ghi Phân loại dự án (Loại dự án).", countAdded);
    }

    /// <summary>
    /// 3. Danh mục Loại hợp đồng (Chính xác theo hình ảnh màn hình)
    /// </summary>
    public static async Task SeedLoaiHopDongsAsync(AppDbContext context, ILogger? logger = null)
    {
        var defaultLoaiHopDongs = new List<(string Code, string Name, string Description)>
        {
            ("03", "Bản quyền phần mềm", "Hợp đồng bản quyền phần mềm"),
            ("07", "Phần mềm", "Hợp đồng phần mềm"),
            ("99", "Khác", "Hợp đồng loại khác"),
            ("05", "Thuê dịch vụ", "Hợp đồng thuê dịch vụ"),
            ("04", "Tư vấn", "Hợp đồng tư vấn"),
            ("02", "Mua sắm phần cứng", "Hợp đồng mua sắm phần cứng"),
            ("01", "Bảo trì", "Hợp đồng bảo trì")
        };

        int countAdded = 0;
        foreach (var item in defaultLoaiHopDongs)
        {
            var existing = await context.LoaiHopDongs.FirstOrDefaultAsync(x => x.Code == item.Code || x.Name == item.Name);
            if (existing == null)
            {
                context.LoaiHopDongs.Add(new LoaiHopDong
                {
                    Id = Guid.NewGuid(),
                    Code = item.Code,
                    Name = item.Name,
                    Description = item.Description,
                    CreatedAt = DateTime.UtcNow
                });
                countAdded++;
            }
            else
            {
                existing.Name = item.Name;
                existing.Description = item.Description;
            }
        }
        await context.SaveChangesAsync();
        logger?.LogInformation("Đã đồng bộ {Count} bản ghi Loại hợp đồng.", countAdded);
    }

    /// <summary>
    /// 4. Danh mục Nhóm dự án
    /// </summary>
    public static async Task SeedNhomDuAnsAsync(AppDbContext context, ILogger? logger = null)
    {
        var defaultNhomDuAns = new List<(string Code, string Name, string Description)>
        {
            ("NHOM_A", "Các dự án nhóm A", "Dự án quy mô lớn nhóm A theo quy định pháp luật"),
            ("NHOM_B", "Các dự án nhóm B", "Dự án quy mô nhóm B (tổng mức đầu tư từ 45 tỷ đến dưới 800 tỷ)"),
            ("NHOM_C", "Các dự án nhóm C", "Dự án quy mô nhóm C và các dự án nhỏ khác")
        };

        int countAdded = 0;
        foreach (var item in defaultNhomDuAns)
        {
            var existing = await context.NhomDuAns.FirstOrDefaultAsync(x => x.Code == item.Code);
            if (existing == null)
            {
                context.NhomDuAns.Add(new NhomDuAn
                {
                    Id = Guid.NewGuid(),
                    Code = item.Code,
                    Name = item.Name,
                    Description = item.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                countAdded++;
            }
            else
            {
                existing.Name = item.Name;
                existing.Description = item.Description;
                existing.IsActive = true;
            }
        }
        await context.SaveChangesAsync();
        logger?.LogInformation("Đã đồng bộ {Count} bản ghi Nhóm dự án.", countAdded);
    }

    /// <summary>
    /// 5. Danh mục Chức vụ
    /// </summary>
    public static async Task SeedChucVusAsync(AppDbContext context, ILogger? logger = null)
    {
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
                var existingByName = await context.ChucVus.FirstOrDefaultAsync(cv => cv.TenChucVu.ToLower() == pos.Name.ToLower());
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
    }

    /// <summary>
    /// 6. Danh mục Phòng ban
    /// </summary>
    public static async Task SeedPhongBansAsync(AppDbContext context, ILogger? logger = null)
    {
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
                context.PhongBans.Add(new PhongBan
                {
                    Id = Guid.NewGuid(),
                    TenPhongBan = pbName,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// 7. Features, Roles, và Permissions
    /// </summary>
    public static async Task SeedSystemCatalogAsync(AppDbContext context, ILogger? logger = null)
    {
        var defaultFeatures = new List<(string Code, string Name, string Description, string? ParentCode, int SortOrder)>
        {
            ("DU_AN", "Quản lý dự án", "Chức năng xem, thêm, sửa, xoá dự án", null, 10),
            ("GOI_THAU", "Quản lý gói thầu", "Chức năng xem, thêm, sửa, xoá gói thầu", null, 20),
            ("QUAN_LY_HOP_DONG", "Quản lý hợp đồng", "Chức năng xem, thêm, sửa, xoá hợp đồng", null, 30),
            ("CONG_VIEC", "Quản lý công việc", "Chức năng xem, thêm, sửa, xoá công việc gói thầu", null, 35),
            ("DOI_TAC", "Quản lý đối tác", "Chức năng xem, thêm, sửa, xoá đối tác", null, 40),
            ("KE_HOACH_VON", "Kế hoạch vốn", "Chức năng quản lý kế hoạch vốn đầu tư và phân kỳ vốn", null, 45),
            ("LICENSE", "Quản lý Bản quyền / License", "Chức năng quản lý license và bản quyền phần mềm", null, 46),
            ("DANH_MUC", "Quản lý Danh mục dữ liệu", "Quản lý các loại dự án, nguồn vốn, loại hợp đồng, nhóm dự án", null, 50),
            ("BAO_CAO", "Báo cáo & Thống kê", "Nhóm chức năng báo cáo tổng hợp & chi tiết", null, 60),
            ("BAO_CAO_TIEN_DO", "Báo cáo 1: Tiến độ Dự án", "Báo cáo trình tự thực hiện các công việc thuộc gói thầu và dự án", "BAO_CAO", 61),
            ("BAO_CAO_VON", "Báo cáo 2: Phân bổ & Vốn", "Báo cáo kế hoạch vốn đầu tư, mua sắm và phân kỳ vốn CNTT", "BAO_CAO", 62),
            ("BAO_CAO_DAU_THAU", "Báo cáo 3: Nhà thầu (LCNT)", "Báo cáo kế hoạch và kết quả lựa chọn nhà thầu", "BAO_CAO", 63),
            ("BAO_CAO_HOP_DONG", "Báo cáo 4: Quản lý Hợp đồng", "Báo cáo theo dõi chi tiết tình hình thực hiện hợp đồng", "BAO_CAO", 64),
            ("BAO_CAO_THANH_TOAN", "Báo cáo 5: Đợt thanh toán", "Báo cáo theo dõi giải ngân và các đợt thanh toán hợp đồng", "BAO_CAO", 65),
            ("BAO_CAO_DU_AN_THAU", "Báo cáo 6: TT Dự án thầu", "Báo cáo tiến độ thanh toán tổng hợp các dự án thầu", "BAO_CAO", 66),
            ("BAO_CAO_DAU_TU", "Báo cáo Tổng hợp Đầu tư", "Báo cáo tổng hợp tình hình thực hiện kinh phí đầu tư", "BAO_CAO", 67),
            ("BAO_CAO_PHE_DUYET", "Danh mục Dự án phê duyệt", "Báo cáo danh mục dự án phê duyệt và hạn License / SLA", "BAO_CAO", 68)
        };

        foreach (var f in defaultFeatures)
        {
            var existing = await context.Features.FirstOrDefaultAsync(x => x.Code == f.Code);
            if (existing == null)
            {
                context.Features.Add(new Feature
                {
                    Code = f.Code,
                    Name = f.Name,
                    Description = f.Description,
                    ParentCode = f.ParentCode,
                    SortOrder = f.SortOrder,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Name = f.Name;
                existing.Description = f.Description;
                existing.ParentCode = f.ParentCode;
                existing.SortOrder = f.SortOrder;
            }
        }

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
    }
}
