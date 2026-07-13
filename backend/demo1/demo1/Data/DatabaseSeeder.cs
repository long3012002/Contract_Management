using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.Entity;
using Microsoft.EntityFrameworkCore;

namespace demo1.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var now = DateTime.UtcNow;
        var projectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var bidPackageId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var investorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var contractorId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var contractId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var resolutionId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        if (!await context.DuAns.AnyAsync(da => da.Code == "PRJ-DEMO-001"))
        {
            await context.DuAns.AddAsync(new DuAn
            {
                Id = projectId,
                Code = "PRJ-DEMO-001",
                Name = "Demo project",
                Description = "Sample project for frontend integration.",
                DuToanPheDuyet = 500000000,
                TrangThai = 4,
                LoaiDuAn = 1,
                DaKetThuc = false,
                CreatedAt = now
            });
        }

        if (!await context.GoiThaus.AnyAsync(gt => gt.Code == "BID-DEMO-001"))
        {
            await context.GoiThaus.AddAsync(new GoiThau
            {
                Id = bidPackageId,
                DuAnId = projectId,
                Code = "BID-DEMO-001",
                Name = "Demo bid package",
                Description = "Sample bid package for API testing.",
                GiaTriGoiThau = 300000000,
                NguongCanhBaoPercent = 90,
                CreatedAt = now
            });
        }

        if (!await context.DoiTacs.AnyAsync(dt => dt.Code == "PAR-DEMO-001"))
        {
            await context.DoiTacs.AddAsync(new DoiTac
            {
                Id = investorId,
                Code = "PAR-DEMO-001",
                Name = "Co-op Bank Việt Nam (Chủ đầu tư)",
                Description = "Ngân hàng Hợp tác xã Việt Nam.",
                TaxCode = "0106008888",
                Phone = "02439744181",
                Email = "contact@coopbank.coop.vn",
                Address = "Tòa nhà N04 Hoàng Đạo Thúy, Cầu Giấy, Hà Nội",
                Account = "111000222333",
                Representative = "Nguyễn Văn A",
                Position = "Tổng Giám Đốc",
                CreatedAt = now
            });
        }

        if (!await context.DoiTacs.AnyAsync(dt => dt.Code == "PAR-DEMO-002"))
        {
            await context.DoiTacs.AddAsync(new DoiTac
            {
                Id = contractorId,
                Code = "PAR-DEMO-002",
                Name = "Công ty Cổ phần Công nghệ ABC (Nhà thầu)",
                Description = "Nhà thầu giải pháp CNTT chuyên nghiệp.",
                TaxCode = "0102030405",
                Phone = "02439999999",
                Email = "info@abc-tech.com.vn",
                Address = "Số 1 Duy Tân, Cầu Giấy, Hà Nội",
                Account = "999888777666",
                Representative = "Trần Văn B",
                Position = "Giám đốc Dự án",
                CreatedAt = now
            });
        }

        if (!await context.HopDongs.AnyAsync(hd => hd.Code == "CTR-DEMO-001"))
        {
            var hopDong = new HopDong
            {
                Id = contractId,
                GoiThauId = bidPackageId,
                ChuDauTuId = investorId,
                NhaThauId = contractorId,
                Code = "CTR-DEMO-001",
                Name = "Hợp đồng cung cấp phần mềm Demo",
                Description = "Hợp đồng mẫu tích hợp hệ thống.",
                LoaiHopDong = 1,
                ThoiHanThucHien = "12 tháng",
                DiaDiemThucHien = "Hà Nội",
                GiaTriHopDong = 280000000,
                HinhThucThanhToan = 2, // Chuyển khoản
                NgayHieuLuc = now.Date.AddDays(-5),
                ExpiredDate = now.Date.AddDays(25),
                RenewalReminderDate = now.Date.AddDays(10),
                IsRenewalRequired = true,
                CreatedAt = now
            };

            hopDong.DotThanhToans.Add(new DotThanhToan
            {
                Id = Guid.NewGuid(),
                HopDongId = contractId,
                TenDot = "Tạm ứng đợt 1",
                TyLeThanhToan = 30.00m,
                GiaTriThanhToan = 84000000.00m,
                CreatedAt = now
            });

            hopDong.DotThanhToans.Add(new DotThanhToan
            {
                Id = Guid.NewGuid(),
                HopDongId = contractId,
                TenDot = "Thanh toán đợt 2",
                TyLeThanhToan = 70.00m,
                GiaTriThanhToan = 196000000.00m,
                CreatedAt = now
            });

            await context.HopDongs.AddAsync(hopDong);
        }

        if (!await context.Resolutions.AnyAsync(resolution => resolution.Code == "RES-DEMO-001"))
        {
            await context.Resolutions.AddAsync(new Resolution
            {
                Id = resolutionId,
                Code = "RES-DEMO-001",
                Name = "Demo resolution",
                Description = "Sample resolution for API testing.",
                IssuedDate = now.Date.AddDays(-20),
                EffectiveDate = now.Date.AddDays(-15),
                FileUrl = "https://example.com/demo-resolution.pdf",
                CreatedAt = now
            });
        }

        await context.SaveChangesAsync();
    }
}
