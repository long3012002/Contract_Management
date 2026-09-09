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
                    IsActive = i % 5 != 0, // Mỗi 5 đối tác thì có 1 bị inactive
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
                    IsActive = i % 6 != 0, // Inactive case
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

        // 5. Seed DuAns (Dự án)
        var duAns = new List<DuAn>();
        if (!await context.DuAns.AnyAsync())
        {
            var phanLoaiDuAns = await context.PhanLoaiDuAns.ToListAsync();
            var nhomDuAns = await context.NhomDuAns.ToListAsync();
            var nguonVons = await context.NguonVons.ToListAsync();

            var plCntt = phanLoaiDuAns.FirstOrDefault(p => p.Code == "PL_CNTT");
            var nhomB = nhomDuAns.FirstOrDefault(n => n.Code == "NHOM_B");

            // Danh sách các số tiền đẹp cho dự án nguồn (đơn vị: đồng: từ 1 tỷ đến 5 tỷ)
            var niceSourceBudgets = new decimal[]
            {
                1_000_000_000m, // 1 tỷ
                1_500_000_000m, // 1.5 tỷ
                2_000_000_000m, // 2 tỷ
                2_500_000_000m, // 2.5 tỷ
                3_000_000_000m, // 3 tỷ
                3_500_000_000m, // 3.5 tỷ
                4_000_000_000m, // 4 tỷ
                5_000_000_000m  // 5 tỷ
            };

            // Danh sách các số tiền đẹp cho dự án triển khai (đơn vị: đồng: từ 200 triệu đến 3 tỷ)
            var niceImplBudgets = new decimal[]
            {
                200_000_000m,   // 200 triệu
                350_000_000m,   // 350 triệu
                500_000_000m,   // 500 triệu
                650_000_000m,   // 650 triệu
                800_000_000m,   // 800 triệu
                1_000_000_000m, // 1 tỷ
                1_200_000_000m, // 1.2 tỷ
                1_500_000_000m, // 1.5 tỷ
                1_800_000_000m, // 1.8 tỷ
                2_000_000_000m, // 2 tỷ
                2_500_000_000m, // 2.5 tỷ
                3_000_000_000m  // 3 tỷ
            };

            // --- 5.1 Tạo các dự án nguồn (LoaiDuAn = 1) ---
            var sourceProjects = new List<DuAn>();
            int totalSourceProjects = 8;
            for (int s = 1; s <= totalSourceProjects; s++)
            {
                // Random năm từ 2022-2026
                int startYear = 2022 + random.Next(0, 5); // 2022, 2023, 2024, 2025, 2026
                int duration = random.Next(2, 4); // 2 đến 3 năm
                int endYear = Math.Min(2026, startYear + duration);

                var startDate = new DateTime(startYear, random.Next(1, 4), 15, 0, 0, 0, DateTimeKind.Utc);
                var endDate = new DateTime(endYear, 12, 31, 0, 0, 0, DateTimeKind.Utc);

                var phanLoai = phanLoaiDuAns.Count > 0 ? phanLoaiDuAns[(s - 1) % phanLoaiDuAns.Count] : null;
                var nhom = nhomDuAns.Count > 0 ? nhomDuAns[(s - 1) % nhomDuAns.Count] : null;
                decimal budget = niceSourceBudgets[(s - 1) % niceSourceBudgets.Length];

                // Trạng thái dự án nguồn (1: Đang triển khai, 2: Hoàn thành, 0: Nháp)
                int trangThai = (s % 3 == 0) ? 2 : ((s % 4 == 0) ? 0 : 1);

                var sp = new DuAn
                {
                    Id = Guid.NewGuid(),
                    Code = $"SRC-{s:D3}",
                    Name = $"Dự án nguồn quy hoạch hạ tầng và phát triển hệ thống CNTT số {s}",
                    Description = $"Dự án nguồn về quy hoạch tổng thể đầu tư hạ tầng công nghệ và mở rộng hệ thống số {s}.",
                    DuToanPheDuyet = budget,
                    TrangThai = trangThai,
                    LoaiDuAn = 1, // Dự án nguồn
                    DaTrienKhai = trangThai != 0,
                    ChuDauTu = "Ngân hàng Hợp tác xã Việt Nam (Co-op Bank)",
                    PhanLoaiDuAnId = phanLoai?.Id,
                    NhomDuAnId = nhom?.Id,
                    SoQuyetDinh = $"QĐ-NHHT/{startYear}/{100 + s}",
                    NgayBatDau = startDate,
                    NgayKetThuc = endDate,
                    NamBatDau = startYear,
                    NamKetThuc = endYear,
                    DaKetThuc = trangThai == 2,
                    IsActive = true,
                    CreatedAt = startDate.AddDays(-10)
                };

                // Gán Nguồn vốn ngẫu nhiên cho dự án nguồn (1-3 nguồn vốn)
                if (nguonVons.Any())
                {
                    int nvCount = random.Next(1, Math.Min(4, nguonVons.Count + 1));
                    var selectedNguonVons = nguonVons.OrderBy(_ => random.Next()).Take(nvCount).ToList();
                    decimal partMoney = Math.Round(budget / nvCount, 0);

                    for (int nvIdx = 0; nvIdx < selectedNguonVons.Count; nvIdx++)
                    {
                        var nv = selectedNguonVons[nvIdx];
                        decimal amt = (nvIdx == selectedNguonVons.Count - 1) ? (budget - partMoney * (selectedNguonVons.Count - 1)) : partMoney;
                        sp.DanhSachNguonVon.Add(new DuAnNguonVon
                        {
                            Id = Guid.NewGuid(),
                            DuAnId = sp.Id,
                            NguonVonId = nv.Id,
                            SoTien = amt,
                            GhiChu = $"Nguồn vốn {nv.Name} cho dự án nguồn {sp.Code}",
                            CreatedAt = startDate.AddDays(-10)
                        });
                    }
                }

                sourceProjects.Add(sp);
            }
            duAns.AddRange(sourceProjects);

            // --- 5.2 Tạo các dự án triển khai (LoaiDuAn = 2) ---
            int totalImplProjects = 30;
            for (int i = 1; i <= totalImplProjects; i++)
            {
                int startYear = 2022 + random.Next(0, 5); // 2022, 2023, 2024, 2025, 2026
                int durationYears = random.Next(1, 3);
                int endYear = Math.Min(2026, startYear + durationYears);

                var startDate = new DateTime(startYear, (i % 12) + 1, 10, 0, 0, 0, DateTimeKind.Utc);
                var endDate = new DateTime(endYear, 12, 31, 0, 0, 0, DateTimeKind.Utc);

                // Trạng thái đa dạng: 1: Đang triển khai, 2: Hoàn thành, 0: Nháp
                int trangThai = 1;
                if (i % 4 == 0) trangThai = 2; // Hoàn thành
                else if (i % 7 == 0) trangThai = 0; // Nháp

                var phanLoai = phanLoaiDuAns.Count > 0 ? phanLoaiDuAns[(i - 1) % phanLoaiDuAns.Count] : null;
                var nhom = nhomDuAns.Count > 0 ? nhomDuAns[(i - 1) % nhomDuAns.Count] : null;
                decimal budget = niceImplBudgets[(i - 1) % niceImplBudgets.Length];

                var implProj = new DuAn
                {
                    Id = Guid.NewGuid(),
                    Code = $"PRJ-{i:D3}",
                    Name = (i % 2 == 1) ? $"Dự án triển khai nâng cấp phần mềm & thiết bị số {i}" : $"Dự án cải tạo, xây dựng hạ tầng chi nhánh số {i}",
                    Description = $"Dự án triển khai thuộc quy hoạch công nghệ và đầu tư hình thành TSCĐ số {i}.",
                    DuToanPheDuyet = budget,
                    TrangThai = trangThai,
                    LoaiDuAn = 2, // Dự án triển khai
                    NhomDuAnId = nhom?.Id,
                    PhanLoaiDuAnId = phanLoai?.Id,
                    ChuDauTu = "Ngân hàng Hợp tác xã Việt Nam (Co-op Bank)",
                    DiaDiemThucHien = "Tòa nhà N04 Hoàng Đạo Thúy, Cầu Giấy, Hà Nội",
                    ThoiGianThucHien = $"{12 + (i % 12)} tháng",
                    NoiDung = $"Nội dung thực hiện chi tiết cho dự án đầu tư triển khai mã {i}.",
                    ToChucThucHien = "Ban Quản lý Dự án CNTT - Co-op Bank",
                    SoQuyetDinh = $"QĐ-NHHT/{startYear}/{200 + i}",
                    NgayBatDau = startDate,
                    NgayKetThuc = endDate,
                    NamBatDau = startYear,
                    NamKetThuc = endYear,
                    DaKetThuc = trangThai == 2,
                    IsActive = i % 10 != 0,
                    CreatedAt = startDate.AddDays(-15)
                };

                // Quyết định liên kết với dự án nguồn:
                // Nếu i % 3 == 0 => liên kết 1-1 với 1 dự án nguồn (triển khai trực tiếp)
                // Ngược lại => liên kết với 2 dự án nguồn
                if (i % 3 == 0)
                {
                    var src1 = sourceProjects[(i - 1) % sourceProjects.Count];
                    implProj.NguonDuAns.Add(new DuAnNguonTrienKhai
                    {
                        TrienKhaiProjectId = implProj.Id,
                        NguonProjectId = src1.Id.ToString(),
                        CreatedAt = startDate.AddDays(-15)
                    });
                }
                else
                {
                    var src1 = sourceProjects[(i - 1) % sourceProjects.Count];
                    var src2 = sourceProjects[i % sourceProjects.Count];
                    implProj.NguonDuAns.Add(new DuAnNguonTrienKhai
                    {
                        TrienKhaiProjectId = implProj.Id,
                        NguonProjectId = src1.Id.ToString(),
                        CreatedAt = startDate.AddDays(-15)
                    });
                    if (src1.Id != src2.Id)
                    {
                        implProj.NguonDuAns.Add(new DuAnNguonTrienKhai
                        {
                            TrienKhaiProjectId = implProj.Id,
                            NguonProjectId = src2.Id.ToString(),
                            CreatedAt = startDate.AddDays(-15)
                        });
                    }
                }

                // Gán Nguồn vốn cho dự án triển khai nếu có
                if (nguonVons.Any())
                {
                    var nv = nguonVons[(i - 1) % nguonVons.Count];
                    implProj.DanhSachNguonVon.Add(new DuAnNguonVon
                    {
                        Id = Guid.NewGuid(),
                        DuAnId = implProj.Id,
                        NguonVonId = nv.Id,
                        SoTien = budget,
                        GhiChu = $"Nguồn vốn {nv.Name} cho dự án triển khai {implProj.Code}",
                        CreatedAt = startDate.AddDays(-15)
                    });
                }

                duAns.Add(implProj);
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
                    HinhThucThanhToan = (i % 3 == 0) ? 1 : 2, // 1: Tiền mặt, 2: Chuyển khoản
                    NgayHieuLuc = selectedGoiThau.CreatedAt.AddDays(10),
                    ExpiredDate = (i % 4 == 0) ? now.AddDays(-random.Next(1, 30)) : selectedGoiThau.CreatedAt.AddDays(375), // Một số HĐ đã hết hạn
                    RenewalReminderDate = (i % 5 == 0) ? now.AddDays(random.Next(1, 5)) : selectedGoiThau.CreatedAt.AddDays(330), // Sắp đến hạn gia hạn
                    IsRenewalRequired = i % 6 != 0,
                    IsActive = i % 7 != 0, // Một số inactive
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

        // 9. Seed DotThanhToans (Đợt thanh toán) - Phủ đủ các năm 2024, 2025 (kỳ trước) & 2026 (trong kỳ)
        if (!await context.DotThanhToans.AnyAsync() && hopDongs.Any())
        {
            var dotThanhToanList = new List<DotThanhToan>();
            int hIdx = 0;

            foreach (var hd in hopDongs)
            {
                var dot1Val = Math.Round(hd.GiaTriHopDong * 0.3m, 2);
                var dot2Val = Math.Round(hd.GiaTriHopDong * 0.4m, 2);
                var dot3Val = hd.GiaTriHopDong - dot1Val - dot2Val;

                // Đợt 1: Quyết toán năm 2024
                dotThanhToanList.Add(new DotThanhToan
                {
                    Id = Guid.NewGuid(),
                    HopDongId = hd.Id,
                    TenDot = $"Tạm ứng 30% năm 2024 hợp đồng {hd.Code}",
                    TyLeThanhToan = 30.00m,
                    GiaTriThanhToan = dot1Val,
                    NgayThanhToan = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
                    IsPaid = true, // Đã quyết toán năm 2024
                    DieuKienThanhToan = "Sau khi ký kết hợp đồng và bảo lãnh tạm ứng năm 2024",
                    CreatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
                });

                // Đợt 2: Quyết toán năm 2025
                dotThanhToanList.Add(new DotThanhToan
                {
                    Id = Guid.NewGuid(),
                    HopDongId = hd.Id,
                    TenDot = $"Nghiệm thu đợt 1 (40%) năm 2025 hợp đồng {hd.Code}",
                    TyLeThanhToan = 40.00m,
                    GiaTriThanhToan = dot2Val,
                    NgayThanhToan = new DateTime(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc),
                    IsPaid = true, // Đã quyết toán năm 2025
                    DieuKienThanhToan = "Sau khi hoàn thành nghiệm thu giai đoạn năm 2025",
                    CreatedAt = new DateTime(2025, 5, 15, 0, 0, 0, DateTimeKind.Utc)
                });

                // Đợt 3: Thanh toán năm 2026
                bool isPaidDot3 = (hIdx % 2 == 0);
                dotThanhToanList.Add(new DotThanhToan
                {
                    Id = Guid.NewGuid(),
                    HopDongId = hd.Id,
                    TenDot = $"Thanh toán đợt 2 (30%) năm 2026 hợp đồng {hd.Code}",
                    TyLeThanhToan = 30.00m,
                    GiaTriThanhToan = dot3Val,
                    NgayThanhToan = isPaidDot3 ? new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc) : now.AddDays(-random.Next(5, 30)), // Nếu chưa trả thì đã quá hạn
                    IsPaid = isPaidDot3, // Phân nửa đã thanh toán trong kỳ 2026
                    DieuKienThanhToan = "Sau khi ký biên bản nghiệm thu giai đoạn năm 2026",
                    CreatedAt = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc)
                });
                
                // Đợt 4 (Edge case): Thanh toán giá trị 0 hoặc quá hạn nhưng giá trị nhỏ
                if (hIdx % 5 == 0)
                {
                    dotThanhToanList.Add(new DotThanhToan
                    {
                        Id = Guid.NewGuid(),
                        HopDongId = hd.Id,
                        TenDot = $"Thanh toán bảo hành hợp đồng {hd.Code}",
                        TyLeThanhToan = 0.00m,
                        GiaTriThanhToan = 0,
                        NgayThanhToan = now.AddDays(random.Next(30, 90)), // Tương lai
                        IsPaid = false,
                        DieuKienThanhToan = "Sau khi hết hạn bảo hành",
                        CreatedAt = now
                    });
                }
                hIdx++;
            }
            await context.DotThanhToans.AddRangeAsync(dotThanhToanList);
            await context.SaveChangesAsync();
        }
        else if (hopDongs.Any())
        {
            // Nếu DB đã có dữ liệu đợt thanh toán cũ (chưa được đánh IsPaid = true hoặc thiếu mốc thời gian)
            var existingDots = await context.DotThanhToans.Include(d => d.HopDong).ToListAsync();
            bool hasPaid2024 = existingDots.Any(d => d.IsPaid && d.NgayThanhToan.HasValue && d.NgayThanhToan.Value.Year == 2024);
            bool hasPaid2025 = existingDots.Any(d => d.IsPaid && d.NgayThanhToan.HasValue && d.NgayThanhToan.Value.Year == 2025);
            bool hasPaid2026 = existingDots.Any(d => d.IsPaid && d.NgayThanhToan.HasValue && d.NgayThanhToan.Value.Year == 2026);

            if (!hasPaid2024 || !hasPaid2025 || !hasPaid2026)
            {
                int idx = 0;
                foreach (var dot in existingDots)
                {
                    dot.IsPaid = true;
                    if (dot.HopDong != null && dot.HopDong.GiaTriHopDong > 0 && dot.GiaTriThanhToan == 0)
                    {
                        dot.GiaTriThanhToan = Math.Round(dot.HopDong.GiaTriHopDong * 0.33m, 2);
                    }

                    if (idx % 3 == 0)
                    {
                        dot.NgayThanhToan = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
                    }
                    else if (idx % 3 == 1)
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
                    TinhTrang = (i % 6 == 0 && taskInfo.Item3 != "Đã xong") ? "Chưa bắt đầu" : ((i % 5 == 0 && taskInfo.Item3 == "Đã xong") ? "Đang thực hiện" : taskInfo.Item3), // Xáo trộn tình trạng
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
