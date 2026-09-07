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
            var phanLoaiDuAns = await context.PhanLoaiDuAns.ToListAsync();
            var plCntt = phanLoaiDuAns.FirstOrDefault(p => p.Code == "PL_CNTT");
            var nhomB = await context.NhomDuAns.FirstOrDefaultAsync(n => n.Code == "NHOM_B");

            // Tạo trước một số dự án nguồn (LoaiDuAn = 1)
            var sourceProjects = new List<DuAn>();
            for (int s = 1; s <= 5; s++)
            {
                var sp = new DuAn
                {
                    Id = Guid.NewGuid(),
                    Code = $"SRC-{s:D3}",
                    Name = $"Dự án nguồn quy hoạch hạ tầng và ứng dụng số {s}",
                    Description = $"Dự án nguồn về đầu tư hạ tầng công nghệ và mở rộng hệ thống số {s}.",
                    DuToanPheDuyet = (40 + s * 20) * 1_000_000_000m,
                    TrangThai = 1,
                    LoaiDuAn = 1, // Dự án nguồn
                    DaTrienKhai = true,
                    ChuDauTu = "Ngân hàng Hợp tác xã Việt Nam (Co-op Bank)",
                    PhanLoaiDuAnId = plCntt?.Id ?? phanLoaiDuAns.FirstOrDefault()?.Id,
                    SoQuyetDinh = $"QĐ-NHHT/2024/{100 + s}",
                    NgayBatDau = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                    NgayKetThuc = new DateTime(2027, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc)
                };
                sourceProjects.Add(sp);
            }
            duAns.AddRange(sourceProjects);

            // Tạo 30 dự án triển khai (LoaiDuAn = 2)
            for (int i = 1; i <= 30; i++)
            {
                bool isCntt = i % 2 == 1;
                bool isGroupB = i <= 10; // 10 dự án nhóm B (vốn >= 45 tỷ)
                decimal budget = isGroupB ? (45 + (i * 5)) * 1_000_000_000m : (5 + (i * 1.2m)) * 1_000_000_000m;
                
                // Mốc thời gian đa dạng: một số bắt đầu từ 2024/2025, một số 2026
                int startYear = 2024 + (i % 3); // 2024, 2025, hoặc 2026
                var startDate = new DateTime(startYear, (i % 12) + 1, 10, 0, 0, 0, DateTimeKind.Utc);
                var endDate = startDate.AddMonths(12 + (i % 12));
                bool isCompleted = i % 4 == 0; // Một số dự án đã hoàn thành bàn giao

                var randomSource = sourceProjects[(i - 1) % sourceProjects.Count];
                var selectedPhanLoai = phanLoaiDuAns.Count > 0 ? phanLoaiDuAns[(i - 1) % phanLoaiDuAns.Count] : null;

                duAns.Add(new DuAn
                {
                    Id = Guid.NewGuid(),
                    Code = $"PRJ-{i:D3}",
                    Name = isCntt ? $"Dự án CNTT trang bị hệ thống phần mềm {i}" : $"Dự án Cải tạo nâng cấp trụ sở chi nhánh {i}",
                    Description = $"Dự án triển khai thuộc quy hoạch công nghệ và đầu tư hình thành TSCĐ số {i}.",
                    DuToanPheDuyet = budget,
                    TrangThai = isCompleted ? 2 : 1, // 2: Hoàn thành, 1: Đang triển khai
                    LoaiDuAn = 2, // Dự án triển khai
                    NguonDuAns = new List<DuAnNguonTrienKhai>
                    {
                        new DuAnNguonTrienKhai { NguonProjectId = randomSource.Id, CreatedAt = startDate.AddDays(-15) }
                    },
                    NhomDuAnId = isGroupB ? nhomB?.Id : null,
                    PhanLoaiDuAnId = selectedPhanLoai?.Id,
                    ChuDauTu = "Ngân hàng Hợp tác xã Việt Nam (Co-op Bank)",
                    DiaDiemThucHien = "Tòa nhà N04 Hoàng Đạo Thúy, Cầu Giấy, Hà Nội",
                    ThoiGianThucHien = $"{12 + (i % 12)} tháng",
                    NoiDung = $"Nội dung thực hiện chi tiết cho dự án đầu tư mã {i}.",
                    ToChucThucHien = "Ban Quản lý Dự án CNTT - Co-op Bank",
                    SoQuyetDinh = $"QĐ-NHHT/{startYear}/{200 + i}",
                    NgayBatDau = startDate,
                    NgayKetThuc = endDate,
                    NamBatDau = startDate.Year,
                    NamKetThuc = endDate.Year,
                    DaKetThuc = isCompleted,
                    IsActive = true,
                    CreatedAt = startDate.AddDays(-15)
                });
            }
            await context.DuAns.AddRangeAsync(duAns);
            await context.SaveChangesAsync();
        }
        else
        {
            duAns = await context.DuAns.Include(d => d.NhomDuAn).Include(d => d.PhanLoaiDuAn).ToListAsync();

            // Nếu DB đã có dữ liệu từ trước nhưng chưa có dự án Nhóm B (ngân sách >= 45 tỷ hoặc mã NHOM_B), cập nhật 6 dự án đầu tiên
            var nhomB = await context.NhomDuAns.FirstOrDefaultAsync(n => n.Code == "NHOM_B");
            bool hasGroupB = duAns.Any(d => (d.NhomDuAn != null && d.NhomDuAn.Code == "NHOM_B") || d.DuToanPheDuyet >= 45_000_000_000m);

            if (!hasGroupB && duAns.Any())
            {
                int count = 1;
                foreach (var proj in duAns.Take(6))
                {
                    proj.DuToanPheDuyet = (50 + count * 15) * 1_000_000_000m;
                    if (nhomB != null) proj.NhomDuAnId = nhomB.Id;
                    count++;
                }
                await context.SaveChangesAsync();
            }

            // Gán/cập nhật Phân loại dự án cho các dự án trong DB chưa có Phân loại dự án
            var allPhanLoai = await context.PhanLoaiDuAns.ToListAsync();
            if (allPhanLoai.Any() && duAns.Any(d => d.PhanLoaiDuAnId == null))
            {
                int idx = 0;
                foreach (var proj in duAns)
                {
                    if (proj.PhanLoaiDuAnId == null)
                    {
                        proj.PhanLoaiDuAnId = allPhanLoai[idx % allPhanLoai.Count].Id;
                    }
                    idx++;
                }
                await context.SaveChangesAsync();
            }
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
                    GiaTriDieuChinh = random.Next(1, 10) * 1_000_000_000m,
                    LyDoDieuChinh = $"Thay đổi thiết kế kỹ thuật và bổ sung hạng mục bảo mật trong giai đoạn {i}.",
                    NgayDieuChinh = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc)
                });
            }
            await context.DieuChinhDuAns.AddRangeAsync(dieuchinhList);
            await context.SaveChangesAsync();
        }

        // 7. Seed GoiThaus (Gói thầu) - 30 bản ghi
        var goiThaus = new List<GoiThau>();
        if (!await context.GoiThaus.AnyAsync() && duAns.Any())
        {
            var targetProjects = duAns.Where(d => d.LoaiDuAn == 2).ToList();
            for (int i = 1; i <= Math.Min(30, targetProjects.Count); i++)
            {
                var project = targetProjects[i - 1];
                goiThaus.Add(new GoiThau
                {
                    Id = Guid.NewGuid(),
                    DuAnId = project.Id,
                    Code = $"BID-{i:D3}",
                    Name = $"Gói thầu số {i} cung cấp thiết bị và bản quyền phần mềm cho {project.Code}",
                    Description = $"Gói thầu triển khai các giải pháp phần mềm chuyên dụng và hạ tầng server {i}.",
                    GiaTriGoiThau = project.DuToanPheDuyet * 0.85m,
                    IsActive = true,
                    CreatedAt = project.CreatedAt.AddDays(5)
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
        var hopDongs = new List<HopDong>();
        if (!await context.HopDongs.AnyAsync() && goiThaus.Any())
        {
            var investors = doiTacs.Where(dt => dt.Code.Contains("INV")).ToList();
            var contractors = doiTacs.Where(dt => dt.Code.Contains("CON")).ToList();

            if (!investors.Any()) investors = doiTacs;
            if (!contractors.Any()) contractors = doiTacs;

            var availableGoiThaus = goiThaus.Take(30).ToList();

            for (int i = 0; i < availableGoiThaus.Count; i++)
            {
                var selectedGoiThau = availableGoiThaus[i];
                var randomInvestor = investors[random.Next(investors.Count)];
                var randomContractor = contractors[random.Next(contractors.Count)];
                var contractVal = selectedGoiThau.GiaTriGoiThau * 0.95m;

                hopDongs.Add(new HopDong
                {
                    Id = Guid.NewGuid(),
                    GoiThauId = selectedGoiThau.Id,
                    DuAnId = selectedGoiThau.DuAnId,
                    ChuDauTuId = randomInvestor.Id,
                    NhaThauId = randomContractor.Id,
                    Code = $"CTR-{i+1:D3}",
                    Name = $"Hợp đồng kinh tế số {i+1} về việc triển khai CNTT/XDCB",
                    Description = $"Ký kết triển khai dự án công nghệ thông tin hạng mục số {i+1}.",
                    LoaiHopDong = (i % 2 == 0) ? 1 : 2,
                    ThoiHanThucHien = "12 tháng",
                    DiaDiemThucHien = "Tòa nhà N04 Hoàng Đạo Thúy, Cầu Giấy, Hà Nội",
                    GiaTriHopDong = Math.Round(contractVal, 2),
                    HinhThucThanhToan = 2, // Chuyển khoản
                    NgayHieuLuc = selectedGoiThau.CreatedAt.AddDays(10),
                    ExpiredDate = selectedGoiThau.CreatedAt.AddDays(375),
                    RenewalReminderDate = selectedGoiThau.CreatedAt.AddDays(330),
                    IsRenewalRequired = true,
                    IsActive = true,
                    CreatedAt = selectedGoiThau.CreatedAt.AddDays(10)
                });
            }
            await context.HopDongs.AddRangeAsync(hopDongs);
            await context.SaveChangesAsync();
        }
        else
        {
            hopDongs = await context.HopDongs.ToListAsync();
        }

        // 9. Seed DotThanhToans (Đợt thanh toán) - Phủ đủ Kỳ trước (2024/2025) & Trong kỳ (2026)
        if (!await context.DotThanhToans.AnyAsync() && hopDongs.Any())
        {
            var dotThanhToanList = new List<DotThanhToan>();

            foreach (var hd in hopDongs)
            {
                var dot1Val = Math.Round(hd.GiaTriHopDong * 0.4m, 2);
                var dot2Val = Math.Round(hd.GiaTriHopDong * 0.4m, 2);
                var dot3Val = hd.GiaTriHopDong - dot1Val - dot2Val;

                // Đợt 1: Kỳ trước (Thanh toán năm 2025)
                dotThanhToanList.Add(new DotThanhToan
                {
                    Id = Guid.NewGuid(),
                    HopDongId = hd.Id,
                    TenDot = $"Tạm ứng 40% hợp đồng {hd.Code}",
                    TyLeThanhToan = 40.00m,
                    GiaTriThanhToan = dot1Val,
                    NgayThanhToan = new DateTime(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc),
                    IsPaid = true, // Đã thanh toán
                    DieuKienThanhToan = "Sau khi ký kết hợp đồng và nhận bảo lãnh tạm ứng",
                    CreatedAt = new DateTime(2025, 5, 15, 0, 0, 0, DateTimeKind.Utc)
                });

                // Đợt 2: Trong kỳ (Thanh toán 6T đầu năm 2026)
                dotThanhToanList.Add(new DotThanhToan
                {
                    Id = Guid.NewGuid(),
                    HopDongId = hd.Id,
                    TenDot = $"Nghiệm thu đợt 1 (40%) hợp đồng {hd.Code}",
                    TyLeThanhToan = 40.00m,
                    GiaTriThanhToan = dot2Val,
                    NgayThanhToan = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                    IsPaid = true, // Đã thanh toán trong kỳ 2026
                    DieuKienThanhToan = "Sau khi hoàn thành nghiệm thu giai đoạn 1",
                    CreatedAt = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc)
                });

                // Đợt 3: Thanh toán còn lại
                dotThanhToanList.Add(new DotThanhToan
                {
                    Id = Guid.NewGuid(),
                    HopDongId = hd.Id,
                    TenDot = $"Thanh lý 20% hợp đồng {hd.Code}",
                    TyLeThanhToan = 20.00m,
                    GiaTriThanhToan = dot3Val,
                    NgayThanhToan = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                    IsPaid = false, // Chưa thanh toán
                    DieuKienThanhToan = "Sau khi ký biên bản nghiệm thu tổng thể",
                    CreatedAt = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)
                });
            }
            await context.DotThanhToans.AddRangeAsync(dotThanhToanList);
            await context.SaveChangesAsync();
        }
        else if (hopDongs.Any())
        {
            // Nếu DB đã có dữ liệu đợt thanh toán cũ (chưa được đánh IsPaid = true hoặc thiếu mốc thời gian)
            var existingDots = await context.DotThanhToans.Include(d => d.HopDong).ToListAsync();
            bool hasPaidBefore = existingDots.Any(d => d.IsPaid && d.NgayThanhToan.HasValue && d.NgayThanhToan.Value.Year < 2026);
            bool hasPaidCurrent = existingDots.Any(d => d.IsPaid && d.NgayThanhToan.HasValue && d.NgayThanhToan.Value.Year == 2026);

            if (!hasPaidBefore || !hasPaidCurrent)
            {
                int idx = 0;
                foreach (var dot in existingDots)
                {
                    dot.IsPaid = true;
                    if (dot.HopDong != null && dot.HopDong.GiaTriHopDong > 0 && dot.GiaTriThanhToan == 0)
                    {
                        dot.GiaTriThanhToan = Math.Round(dot.HopDong.GiaTriHopDong * 0.4m, 2);
                    }

                    // Chia phân nửa cho kỳ trước (2025) và phân nửa cho kỳ này (2026)
                    if (idx % 2 == 0)
                    {
                        dot.NgayThanhToan = new DateTime(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc);
                    }
                    else
                    {
                        dot.NgayThanhToan = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
                    }
                    idx++;
                }
                await context.SaveChangesAsync();
            }
        }

        // 10. Seed CongViecGoiThaus (Công việc gói thầu)
        await SeedCongViecGoiThausAsync(context);
    }

    public static async Task SeedCongViecGoiThausAsync(AppDbContext context)
    {
        if (await context.CongViecGoiThaus.AnyAsync()) return;

        var goiThaus = await context.GoiThaus.ToListAsync();
        if (!goiThaus.Any()) return;

        var templateTasks = new[]
        {
            ("Tờ trình v/v xin chủ trương xây dựng hệ thống", "Bản photo", "Đã xong"),
            ("Tờ trình đề xuất chức năng nghiệp vụ, kỹ thuật của hệ thống", "Bản photo", "Đã xong"),
            ("Chứng thư thẩm định giá", "Bản photo", "Đã xong"),
            ("Tờ trình phê duyệt dự toán Dự án", "Bản photo", "Đã xong"),
            ("Quyết định phê duyệt dự toán Dự án", "Bản gốc", "Đã xong"),
            ("Tờ trình phê duyệt kế hoạch lựa chọn nhà thầu", "Bản photo", "Đã xong"),
            ("Quyết định phê duyệt kế hoạch lựa chọn nhà thầu", "Bản gốc", "Đã xong"),
            ("Đơn xin tham gia + báo giá + Hồ sơ năng lực", "Bản gốc", "Đã xong"),
            ("Thư mời thương thảo hợp đồng", "Bản gốc", "Đã xong"),
            ("Công văn xác nhận tham gia thương thảo HĐ", "Bản gốc", "Đã xong"),
            ("Biên bản hoàn thiện hợp đồng", "Bản gốc", "Đã xong"),
            ("Tờ trình phê duyệt KQLCNT Gói thầu", "Bản gốc", "Đã xong"),
            ("Quyết định phê duyệt KQLCNT Gói thầu", "Bản gốc", "Đã xong"),
            ("Hợp đồng kinh tế triển khai gói thầu", "Bản gốc", "Đã xong"),
            ("Bảo lãnh thực hiện HĐ", "Bản gốc", "Đang thực hiện"),
            ("Bảo lãnh tạm ứng", "Bản gốc", "Đang thực hiện"),
            ("Biên bản nghiệm thu hoàn thành triển khai và tích hợp hệ thống", "Bản gốc", "Đang thực hiện"),
            ("Biên bản nghiệm thu đào tạo", "Bản gốc", "Đang thực hiện"),
            ("Biên bản nghiệm thu tổng thể", "Bản gốc", "Đang thực hiện"),
            ("Biên bản thanh lý Hợp đồng", "Bản gốc", "Đã xong"),
            ("Bảo lãnh bảo hành", "Bản gốc", "Đang thực hiện"),
            ("Hóa đơn tài chính", "Bản gốc", "Đã xong"),
            ("Giấy đề nghị thanh toán", "Bản gốc", "Đã xong"),
            ("Tờ trình thanh toán", "Bản gốc", "Đã xong")
        };

        var list = new List<CongViecGoiThau>();
        var now = DateTime.UtcNow;

        foreach (var gt in goiThaus)
        {
            for (int i = 0; i < templateTasks.Length; i++)
            {
                var taskInfo = templateTasks[i];
                int stt = i + 1;
                var id = Guid.NewGuid();
                list.Add(new CongViecGoiThau
                {
                    Id = id,
                    GoiThauId = gt.Id,
                    Stt = stt,
                    TenTaiLieu = taskInfo.Item1,
                    NgayKy = now.AddDays(-(30 - stt)),
                    LoaiVanBan = taskInfo.Item2,
                    TinhTrang = taskInfo.Item3,
                    GhiChu = $"Ghi chú công việc {stt} của gói thầu {gt.Code}",
                    Code = $"CVGT-{gt.Code}-{stt:D2}",
                    Name = taskInfo.Item1,
                    Description = $"Mô tả công việc {stt}",
                    IsActive = true,
                    CreatedAt = now.AddDays(-(30 - stt))
                });
            }
        }

        await context.CongViecGoiThaus.AddRangeAsync(list);
        await context.SaveChangesAsync();
    }
}
