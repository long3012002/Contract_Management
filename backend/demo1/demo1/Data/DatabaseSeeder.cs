using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Entity;
using Microsoft.EntityFrameworkCore;

namespace demo1.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var now = DateTime.UtcNow;
        var random = new Random();

        // 1. Seed PhongBans (Phòng ban) - 30 bản ghi
        var phongBans = new List<PhongBan>();
        if (!await context.PhongBans.AnyAsync())
        {
            var tenPhongBans = new[] { "Phòng CNTT", "Phòng Kế hoạch", "Phòng Tài chính", "Phòng Nhân sự", "Phòng Pháp chế", "Phòng Kinh doanh", "Phòng Dự án", "Phòng Kỹ thuật", "Phòng R&D", "Phòng Giám sát" };
            for (int i = 1; i <= 30; i++)
            {
                var tenPb = tenPhongBans[(i - 1) % tenPhongBans.Length] + $" nhóm {((i - 1) / tenPhongBans.Length) + 1}";
                phongBans.Add(new PhongBan
                {
                    Id = Guid.NewGuid(),
                    TenPhongBan = tenPb,
                    CreatedAt = now.AddDays(-random.Next(1, 100))
                });
            }
            await context.PhongBans.AddRangeAsync(phongBans);
            await context.SaveChangesAsync();
        }
        else
        {
            phongBans = await context.PhongBans.ToListAsync();
        }

        // 2. Seed ChucVus (Chức vụ) - 30 bản ghi
        var chucVus = new List<ChucVu>();
        if (!await context.ChucVus.AnyAsync())
        {
            var tenChucVus = new[] { "Giám đốc", "Phó Giám đốc", "Trưởng phòng", "Phó Trưởng phòng", "Chuyên viên cao cấp", "Chuyên viên", "Nhân viên thử việc", "Kỹ sư trưởng", "Kiểm soát viên", "Trưởng nhóm" };
            for (int i = 1; i <= 30; i++)
            {
                var tenCv = tenChucVus[(i - 1) % tenChucVus.Length] + $" bậc {((i - 1) / tenChucVus.Length) + 1}";
                chucVus.Add(new ChucVu
                {
                    Id = Guid.NewGuid(),
                    TenChucVu = tenCv,
                    CreatedAt = now.AddDays(-random.Next(1, 100))
                });
            }
            await context.ChucVus.AddRangeAsync(chucVus);
            await context.SaveChangesAsync();
        }
        else
        {
            chucVus = await context.ChucVus.ToListAsync();
        }

        // 3. Seed DoiTacs (Đối tác) - 40 bản ghi
        var doiTacs = new List<DoiTac>();
        if (!await context.DoiTacs.AnyAsync())
        {
            for (int i = 1; i <= 40; i++)
            {
                var isInvestor = i % 2 == 0;
                var roleStr = isInvestor ? "Chủ đầu tư" : "Nhà thầu";
                doiTacs.Add(new DoiTac
                {
                    Id = Guid.NewGuid(),
                    Code = $"PAR-{(isInvestor ? "INV" : "CON")}-{i:D3}",
                    Name = $"Công ty {(isInvestor ? "Đầu tư" : "Công nghệ")} thành viên số {i} ({roleStr})",
                    Description = $"Đối tác hoạt động trong lĩnh vực {(isInvestor ? "Tài chính và Đầu tư" : "Công nghệ thông tin và tích hợp hệ thống")}.",
                    TaxCode = $"{1000000000 + i}",
                    Phone = $"0243{random.Next(100000, 999999)}",
                    Email = $"contact{i}@partners-group-{i}.com.vn",
                    Address = $"Số {i} Đường Láng, Đống Đa, Hà Nội",
                    Account = $"999000888{i:D3}",
                    Representative = $"Nguyễn Văn Đại diện {i}",
                    Position = isInvestor ? "Tổng Giám Đốc" : "Giám đốc Dự án",
                    IsActive = true,
                    CreatedAt = now.AddDays(-random.Next(50, 200))
                });
            }
            await context.DoiTacs.AddRangeAsync(doiTacs);
            await context.SaveChangesAsync();
        }
        else
        {
            doiTacs = await context.DoiTacs.ToListAsync();
        }

        // 4. Seed Resolutions (Nghị quyết) - 30 bản ghi
        var resolutions = new List<Resolution>();
        if (!await context.Resolutions.AnyAsync())
        {
            for (int i = 1; i <= 30; i++)
            {
                resolutions.Add(new Resolution
                {
                    Id = Guid.NewGuid(),
                    Code = $"RES-{i:D3}",
                    Name = $"Quyết định/Nghị quyết số {100 + i}/NQ-HĐQT phê duyệt chủ trương {i}",
                    Description = $"Nghị quyết Hội đồng quản trị thông qua kế hoạch đầu tư công nghệ dự án số {i}.",
                    IssuedDate = now.Date.AddDays(-random.Next(30, 180)),
                    EffectiveDate = now.Date.AddDays(-random.Next(1, 29)),
                    FileUrl = $"https://coopbank.com/documents/resolutions/res-{i:D3}.pdf",
                    IsActive = true,
                    CreatedAt = now.AddDays(-random.Next(30, 180))
                });
            }
            await context.Resolutions.AddRangeAsync(resolutions);
            await context.SaveChangesAsync();
        }
        else
        {
            resolutions = await context.Resolutions.ToListAsync();
        }

        // 5. Seed DuAns (Dự án) - 35 bản ghi
        var duAns = new List<DuAn>();
        if (!await context.DuAns.AnyAsync())
        {
            for (int i = 1; i <= 35; i++)
            {
                var isSourceProject = i % 3 == 0;
                var startDate = now.Date.AddDays(-random.Next(100, 500));
                var endDate = startDate.AddMonths(random.Next(6, 24));
                duAns.Add(new DuAn
                {
                    Id = Guid.NewGuid(),
                    Code = $"PRJ-{i:D3}",
                    Name = $"Dự án phát triển hệ thống {i}",
                    Description = $"Dự án xây dựng phần mềm quản lý và tối ưu hóa quy trình {i}.",
                    DuToanPheDuyet = random.Next(10, 500) * 10_000_000m,
                    TrangThai = random.Next(1, 5),
                    LoaiDuAn = isSourceProject ? 1 : 2,
                    ChuDauTu = "Ngân hàng Hợp tác xã Việt Nam (Co-op Bank)",
                    DiaDiemThucHien = "Tòa nhà N04 Hoàng Đạo Thúy, Cầu Giấy, Hà Nội",
                    ThoiGianThucHien = $"{random.Next(6, 24)} tháng",
                    NoiDung = $"Chi tiết nội dung thực hiện dự án nâng cấp hệ thống lõi số {i}.",
                    ToChucThucHien = "Ban Quản lý Dự án CNTT - Co-op Bank",
                    NgayBatDau = startDate,
                    NgayKetThuc = endDate,
                    NamBatDau = startDate.Year,
                    NamKetThuc = endDate.Year,
                    DaKetThuc = endDate < now,
                    IsActive = true,
                    CreatedAt = startDate.AddDays(-10)
                });
            }
            await context.DuAns.AddRangeAsync(duAns);
            await context.SaveChangesAsync();
        }
        else
        {
            duAns = await context.DuAns.ToListAsync();
        }

        // 6. Seed DieuChinhDuAns (Điều chỉnh dự án) - 30 bản ghi
        if (!await context.DieuChinhDuAns.AnyAsync() && duAns.Any())
        {
            var dieuchinhList = new List<DieuChinhDuAn>();
            for (int i = 1; i <= 30; i++)
            {
                var randomProject = duAns[random.Next(duAns.Count)];
                dieuchinhList.Add(new DieuChinhDuAn
                {
                    Id = Guid.NewGuid(),
                    DuAnId = randomProject.Id,
                    Code = $"ADJ-{i:D3}",
                    Name = $"Điều chỉnh dự án {randomProject.Code} lần {i}",
                    Description = $"Cập nhật quy mô và bổ sung ngân sách bổ trợ hạng mục {i}.",
                    GiaTriDieuChinh = random.Next(5, 50) * 1_000_000m,
                    LyDoDieuChinh = $"Thay đổi thiết kế kỹ thuật và yêu cầu bảo mật bổ sung trong giai đoạn {i}.",
                    NgayDieuChinh = now.AddDays(-random.Next(1, 60)),
                    IsActive = true,
                    CreatedAt = now.AddDays(-random.Next(1, 60))
                });
            }
            await context.DieuChinhDuAns.AddRangeAsync(dieuchinhList);
            await context.SaveChangesAsync();
        }

        // 7. Seed GoiThaus (Gói thầu) - 30 bản ghi
        var goiThaus = new List<GoiThau>();
        if (!await context.GoiThaus.AnyAsync() && duAns.Any())
        {
            for (int i = 1; i <= 30; i++)
            {
                var randomProject = duAns[random.Next(duAns.Count)];
                goiThaus.Add(new GoiThau
                {
                    Id = Guid.NewGuid(),
                    DuAnId = randomProject.Id,
                    Code = $"BID-{i:D3}",
                    Name = $"Gói thầu số {i} cung cấp thiết bị và bản quyền phần mềm",
                    Description = $"Gói thầu triển khai các giải pháp phần mềm chuyên dụng và hạ tầng server {i}.",
                    GiaTriGoiThau = random.Next(5, 100) * 5_000_000m,
                    NguongCanhBaoPercent = random.Next(80, 100),
                    IsActive = true,
                    CreatedAt = randomProject.CreatedAt.AddDays(5)
                });
            }
            await context.GoiThaus.AddRangeAsync(goiThaus);
            await context.SaveChangesAsync();
        }
        else
        {
            goiThaus = await context.GoiThaus.ToListAsync();
        }

        // 8. Seed HopDongs (Hợp đồng) - 30 bản ghi
        // LƯU Ý: HopDong có ràng buộc Unique Index trên GoiThauId. Mỗi gói thầu chỉ gắn với tối đa 1 hợp đồng.
        var hopDongs = new List<HopDong>();
        if (!await context.HopDongs.AnyAsync() && goiThaus.Any())
        {
            // Lấy danh sách đối tác chủ đầu tư và nhà thầu
            var investors = doiTacs.Where(dt => dt.Code.Contains("INV")).ToList();
            var contractors = doiTacs.Where(dt => dt.Code.Contains("CON")).ToList();

            if (!investors.Any()) investors = doiTacs;
            if (!contractors.Any()) contractors = doiTacs;

            // Lấy tối đa 30 gói thầu không trùng lặp để gán cho 30 hợp đồng
            var availableGoiThaus = goiThaus.Take(30).ToList();

            for (int i = 0; i < availableGoiThaus.Count; i++)
            {
                var selectedGoiThau = availableGoiThaus[i];
                var randomInvestor = investors[random.Next(investors.Count)];
                var randomContractor = contractors[random.Next(contractors.Count)];
                var contractVal = selectedGoiThau.GiaTriGoiThau * (decimal)(0.9 + random.NextDouble() * 0.1); // Giá trị hợp đồng xấp xỉ gói thầu

                hopDongs.Add(new HopDong
                {
                    Id = Guid.NewGuid(),
                    GoiThauId = selectedGoiThau.Id,
                    DuAnId = selectedGoiThau.DuAnId,
                    ChuDauTuId = randomInvestor.Id,
                    NhaThauId = randomContractor.Id,
                    Code = $"CTR-{i+1:D3}",
                    Name = $"Hợp đồng kinh tế số {i+1} về việc triển khai CNTT",
                    Description = $"Ký kết triển khai dự án công nghệ thông tin hạng mục số {i+1}.",
                    LoaiHopDong = random.Next(1, 3), // 1 hoặc 2
                    ThoiHanThucHien = $"{random.Next(3, 18)} tháng",
                    DiaDiemThucHien = "Tòa nhà N04 Hoàng Đạo Thúy, Cầu Giấy, Hà Nội",
                    GiaTriHopDong = Math.Round(contractVal, 2),
                    HinhThucThanhToan = random.Next(1, 3), // 1. Tiền mặt, 2. Chuyển khoản
                    NgayHieuLuc = now.Date.AddDays(-random.Next(10, 60)),
                    ExpiredDate = now.Date.AddDays(random.Next(60, 365)),
                    RenewalReminderDate = now.Date.AddDays(random.Next(30, 59)),
                    IsRenewalRequired = random.Next(0, 2) == 1,
                    IsActive = true,
                    CreatedAt = now.AddDays(-random.Next(10, 60))
                });
            }
            await context.HopDongs.AddRangeAsync(hopDongs);
            await context.SaveChangesAsync();
        }
        else
        {
            hopDongs = await context.HopDongs.ToListAsync();
        }

        // 9. Seed DotThanhToans (Đợt thanh toán) - 50 bản ghi
        if (!await context.DotThanhToans.AnyAsync() && hopDongs.Any())
        {
            var dotThanhToanList = new List<DotThanhToan>();
            int dotCount = 1;
            
            // Chia đều đợt thanh toán cho các hợp đồng hiện có
            foreach (var hd in hopDongs)
            {
                if (dotCount > 50) break;

                // Mỗi hợp đồng tạo 2 đợt thanh toán
                var dot1Val = Math.Round(hd.GiaTriHopDong * 0.4m, 2);
                var dot2Val = hd.GiaTriHopDong - dot1Val;

                dotThanhToanList.Add(new DotThanhToan
                {
                    Id = Guid.NewGuid(),
                    HopDongId = hd.Id,
                    TenDot = $"Tạm ứng 40% hợp đồng {hd.Code}",
                    TyLeThanhToan = 40.00m,
                    GiaTriThanhToan = dot1Val,
                    CreatedAt = hd.CreatedAt.AddDays(3)
                });
                dotCount++;

                if (dotCount <= 50)
                {
                    dotThanhToanList.Add(new DotThanhToan
                    {
                        Id = Guid.NewGuid(),
                        HopDongId = hd.Id,
                        TenDot = $"Nghiệm thu và thanh toán 60% hợp đồng {hd.Code}",
                        TyLeThanhToan = 60.00m,
                        GiaTriThanhToan = dot2Val,
                        CreatedAt = hd.CreatedAt.AddDays(15)
                    });
                    dotCount++;
                }
            }
            await context.DotThanhToans.AddRangeAsync(dotThanhToanList);
            await context.SaveChangesAsync();
        }
    }
}
