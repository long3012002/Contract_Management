using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using demo1.DTOs;
using demo1.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace demo1.Services.Implements;

public partial class ReportService
{
    public async Task<ReportResponseDto> GetInvestmentReportAsync(int year, int period, string? donViTinh = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (conversionFactor, unitName) = ParseUnit(donViTinh);

        // 1. Tính toán thời gian báo cáo
        var (startOfPeriod, endOfPeriod, periodDisplayName, periodName) = CalculateReportPeriod(year, period, fromDate, toDate);

        // 2. Tải danh sách dự án hợp lệ (lọc phân quyền, bỏ qua dự án đã xóa, loại bỏ dự án gộp/Merged)
        var query = _context.DuAns
            .AsNoTracking()
            .Where(da => da.IsActive && !da.IsDeleted && da.TrangThai != (int)TrangThaiDuAn.Merged);

        // Lọc dự án khởi tạo/bắt đầu trước hoặc trong kỳ báo cáo
        query = query.Where(da => 
            (da.NgayBatDau.HasValue && da.NgayBatDau.Value <= endOfPeriod) ||
            (!da.NgayBatDau.HasValue && da.NamBatDau.HasValue && da.NamBatDau.Value <= endOfPeriod.Year) ||
            (!da.NgayBatDau.HasValue && !da.NamBatDau.HasValue && da.CreatedAt.Year <= endOfPeriod.Year)
        );

        // Lọc bỏ các dự án đã kết thúc trước khi bắt đầu kỳ báo cáo (ngày kết thúc < startOfPeriod)
        query = query.Where(da => 
            (da.NgayKetThucThucTe.HasValue ? da.NgayKetThucThucTe.Value >= startOfPeriod :
             da.NgayKetThuc.HasValue ? da.NgayKetThuc.Value >= startOfPeriod :
             !da.NamKetThuc.HasValue || da.NamKetThuc.Value >= startOfPeriod.Year)
        );

        if (_currentUserService != null)
        {
            var currentUsername = _currentUserService.GetUsername();
            if (!string.IsNullOrEmpty(currentUsername))
            {
                var currentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);
                if (currentUser != null && !currentUser.IsSystemAdmin)
                {
                    query = query.Where(da => da.CreatedByUserId == currentUser.Id || da.ChuDuAnId == currentUser.Id
                        || _context.UserPermissions.Any(up => up.UserId == currentUser.Id && up.DuAnId == da.Id)
                        || _context.CongViecNguoiLienQuans.Any(nlq => nlq.UserId == currentUser.Id && nlq.CongViecGoiThau != null && nlq.CongViecGoiThau.GoiThau != null && nlq.CongViecGoiThau.GoiThau.DuAnId == da.Id)
                        || (currentUser.CanViewHopDong && _context.HopDongs.Any(h => h.DuAnId == da.Id && h.LoaiHopDongNavigation != null && h.LoaiHopDongNavigation.Code == "01")));
                }
            }
        }

        var projectsData = await query
            .Select(da => new
            {
                da.Id,
                da.Name,
                da.Code,
                da.DuToanPheDuyet,
                da.SoQuyetDinh,
                da.NgayBatDau,
                da.NgayKetThuc,
                da.NgayKetThucThucTe,
                da.UpdatedAt,
                da.TrangThai,
                da.DaKetThuc,
                da.ToChucThucHien,
                da.ChuDauTu,
                PmPhuTrach = da.ChuDuAn != null ? da.ChuDuAn.FullName : null,
                NhomDuAnCode = da.NhomDuAn != null ? da.NhomDuAn.Code : null,
                da.NhomDuAnId,
                da.PhanLoaiDuAnId,
                PhanLoaiDuAnCode = da.PhanLoaiDuAn != null ? da.PhanLoaiDuAn.Code : null,
                PhanLoaiDuAnName = da.PhanLoaiDuAn != null ? da.PhanLoaiDuAn.Name : null,
                DanhSachNguonVon = da.DanhSachNguonVon.Select(nv => new
                {
                    nv.Id,
                    nv.SoTien,
                    nv.NguonVonId,
                    NguonVonCode = nv.NguonVon != null ? nv.NguonVon.Code : null,
                    NguonVonName = nv.NguonVon != null ? nv.NguonVon.Name : null
                }).ToList()
            })
            .ToListAsync();

        // 4. Tính toán giá trị lũy kế đã thanh toán (chỉ tính đợt đã thanh toán IsPaid = true, hợp đồng active & chưa xóa, dùng ngày thanh toán thực tế)
        var targetDuAnIds = projectsData.Select(p => p.Id).ToList();
        var performedValues = targetDuAnIds.Any()
            ? await _context.DotThanhToans
                .AsNoTracking()
                .Where(dt => dt.IsPaid 
                    && dt.HopDong != null 
                    && dt.HopDong.IsActive 
                    && !dt.HopDong.IsDeleted 
                    && dt.HopDong.DuAnId.HasValue
                    && targetDuAnIds.Contains(dt.HopDong.DuAnId.Value))
                .Select(dt => new
                {
                    DuAnId = dt.HopDong.DuAnId!.Value,
                    PaymentDate = dt.NgayThanhToanThucTe ?? dt.NgayThanhToan ?? dt.CreatedAt,
                    dt.GiaTriThanhToan
                })
                .GroupBy(x => x.DuAnId)
                .Select(g => new
                {
                    DuAnId = g.Key,
                    KyTruoc = g.Where(x => x.PaymentDate < startOfPeriod).Sum(x => x.GiaTriThanhToan),
                    TrongKy = g.Where(x => x.PaymentDate >= startOfPeriod && x.PaymentDate <= endOfPeriod).Sum(x => x.GiaTriThanhToan)
                })
                .ToDictionaryAsync(x => x.DuAnId, x => x)
            : new();

        // 5. Tải danh mục Phân loại dự án để phân nhóm động
        var dbCategories = await _context.PhanLoaiDuAns
            .AsNoTracking()
            .Where(pl => pl.IsActive && !pl.IsDeleted)
            .OrderBy(pl => pl.Code)
            .Select(pl => new { pl.Id, pl.Code, pl.Name })
            .ToListAsync();

        var categoryList = dbCategories.Select(c => new
        {
            Id = c.Id,
            Code = c.Code ?? string.Empty,
            Name = c.Name ?? "Chưa phân loại"
        }).ToList();

        // Kiểm tra xem có dự án nào không thuộc danh mục chủ động không
        Guid GetCategoryIdForProject(Guid? pId, string? pCode, string? pName)
        {
            if (pId.HasValue && pId.Value != Guid.Empty)
            {
                var match = categoryList.FirstOrDefault(c => c.Id == pId.Value);
                if (match != null) return match.Id;
            }
            if (!string.IsNullOrWhiteSpace(pCode))
            {
                var match = categoryList.FirstOrDefault(c => string.Equals(c.Code, pCode, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match.Id;
            }
            if (!string.IsNullOrWhiteSpace(pName))
            {
                var match = categoryList.FirstOrDefault(c => string.Equals(c.Name, pName, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match.Id;
            }
            var khacMatch = categoryList.FirstOrDefault(c => c.Code.Equals("PL_KHAC", StringComparison.OrdinalIgnoreCase) || c.Name.ToLower().Contains("khác"));
            if (khacMatch != null) return khacMatch.Id;

            return Guid.Empty;
        }

        bool hasUncategorized = projectsData.Any(p => GetCategoryIdForProject(p.PhanLoaiDuAnId, p.PhanLoaiDuAnCode, p.PhanLoaiDuAnName) == Guid.Empty);
        if (hasUncategorized && !categoryList.Any(c => c.Id == Guid.Empty))
        {
            categoryList.Add(new { Id = Guid.Empty, Code = "PL_KHAC_FALLBACK", Name = "Dự án / Phân loại khác" });
        }

        var categoryProjectsMap = categoryList.ToDictionary(c => c.Id, _ => new List<ReportRowDto>());

        foreach (var project in projectsData)
        {
            // 1. Tính toán Tổng mức vốn đầu tư từ Tổng mức đầu tư (DuToanPheDuyet) và Danh sách nguồn vốn của dự án
            decimal totalNguonVonVnd = project.DanhSachNguonVon != null && project.DanhSachNguonVon.Any()
                ? project.DanhSachNguonVon.Sum(nv => nv.SoTien)
                : 0m;
            decimal totalBudgetVnd = project.DuToanPheDuyet > 0 ? project.DuToanPheDuyet : totalNguonVonVnd;

            decimal rawVcshVnd = 0;
            decimal rawVayVnd = 0;
            decimal rawKhacVnd = 0;

            if (project.DanhSachNguonVon != null && project.DanhSachNguonVon.Any())
            {
                foreach (var nv in project.DanhSachNguonVon)
                {
                    var code = (nv.NguonVonCode ?? string.Empty).ToLowerInvariant();
                    var name = (nv.NguonVonName ?? string.Empty).ToLowerInvariant();

                    if (code.Contains("vay") || name.Contains("vay") || name.Contains("tín dụng") || name.Contains("tin dung"))
                    {
                        rawVayVnd += nv.SoTien;
                    }
                    else if (code.Contains("khac") || code.Contains("nv_khac") || name.Contains("khác") || name.Contains("khac"))
                    {
                        rawKhacVnd += nv.SoTien;
                    }
                    else
                    {
                        rawVcshVnd += nv.SoTien;
                    }
                }
            }
            else
            {
                rawVcshVnd = totalBudgetVnd;
            }

            if (rawVcshVnd == 0 && rawVayVnd == 0 && rawKhacVnd == 0 && totalBudgetVnd > 0)
            {
                rawVcshVnd = totalBudgetVnd;
            }

            // Phân giải các giá trị lũy kế thanh toán từ map tổng hợp ở DB
            performedValues.TryGetValue(project.Id, out var perf);
            decimal performedKyTruocVnd = perf?.KyTruoc ?? 0;
            decimal performedTrongKyVnd = perf?.TrongKy ?? 0;
            decimal performedLuyKeVnd = performedKyTruocVnd + performedTrongKyVnd;

            // Giá trị tài sản bàn giao đưa vào sử dụng
            decimal taiSanBanGiaoVnd = 0;
            if (project.TrangThai == (int)TrangThaiDuAn.HoanThanh || project.DaKetThuc)
            {
                bool isCompletedBeforeEnd = false;
                var effectiveEndDate = project.NgayKetThucThucTe ?? project.NgayKetThuc;
                if (effectiveEndDate.HasValue && effectiveEndDate.Value <= endOfPeriod)
                {
                    isCompletedBeforeEnd = true;
                }
                else if (!effectiveEndDate.HasValue && project.UpdatedAt.HasValue && project.UpdatedAt.Value <= endOfPeriod)
                {
                    isCompletedBeforeEnd = true;
                }
                else if (!effectiveEndDate.HasValue && !project.UpdatedAt.HasValue)
                {
                    isCompletedBeforeEnd = true; // Giá trị mặc định nếu trạng thái đã hoàn thành
                }

                if (isCompletedBeforeEnd)
                {
                    taiSanBanGiaoVnd = performedLuyKeVnd;
                }
            }

            // Quy đổi theo đơn vị tính
            decimal budgetTotal = totalBudgetVnd / conversionFactor;
            decimal budgetVcsh = rawVcshVnd / conversionFactor;
            decimal budgetVay = rawVayVnd / conversionFactor;
            decimal budgetKhac = rawKhacVnd / conversionFactor;

            decimal kLuongKyTruoc = performedKyTruocVnd / conversionFactor;
            decimal kLuongTrongKy = performedTrongKyVnd / conversionFactor;
            decimal kLuongLuyKe = performedLuyKeVnd / conversionFactor;

            decimal gNganKyTruoc = kLuongKyTruoc;
            decimal gNganTrongKy = kLuongTrongKy;
            decimal gNganLuyKe = kLuongLuyKe;

            decimal tsBanGiao = taiSanBanGiaoVnd / conversionFactor;

            // Xác định mô tả quyết định
            string approvalDecision = project.SoQuyetDinh ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(approvalDecision) && !approvalDecision.Contains("ngày") && project.NgayBatDau.HasValue)
            {
                approvalDecision = $"{approvalDecision} ngày {project.NgayBatDau.Value.ToString("dd/MM/yyyy")} V/v phê duyệt dự án {project.Name}";
            }

            var row = new ReportRowDto
            {
                RowType = "ProjectRow",
                ProjectName = project.Name,
                ApprovalDecision = approvalDecision,
                TongMucDauTuTong = budgetTotal,
                TongMucDauTuVCSH = budgetVcsh,
                TongMucDauTuVay = budgetVay,
                TongMucDauTuKhac = budgetKhac,
                KhoiLuongKyTruoc = kLuongKyTruoc,
                KhoiLuongTrongKy = kLuongTrongKy,
                KhoiLuongLuyKe = kLuongLuyKe,
                GiaiNganKyTruoc = gNganKyTruoc,
                GiaiNganTrongKy = gNganTrongKy,
                GiaiNganLuyKe = gNganLuyKe,
                TaiSanBanGiao = tsBanGiao,

                // Mẫu Báo cáo 1
                MaDuAn = project.Code,
                DonViChuTri = !string.IsNullOrWhiteSpace(project.ToChucThucHien) ? project.ToChucThucHien : project.ChuDauTu,
                PmPhuTrach = project.PmPhuTrach,
                LoaiDuAn = !string.IsNullOrWhiteSpace(project.PhanLoaiDuAnName) ? project.PhanLoaiDuAnName : "Dự án CNTT",
                NgayBatDau = project.NgayBatDau,
                NgayKetThuc = project.NgayKetThuc,
                ThoiGianConLaiNgay = project.NgayKetThuc.HasValue ? (int?)Math.Max(0, (project.NgayKetThuc.Value.Date - DateTime.UtcNow.Date).Days) : null,
                TrangThaiThucTe = project.DaKetThuc || project.TrangThai == 2 ? "Đã hoàn thành" :
                                  project.TrangThai == 1 ? "Đang triển khai" : "Chuẩn bị đầu tư",
                TienDo = project.DaKetThuc || project.TrangThai == 2 ? 1.0 :
                         (!project.NgayBatDau.HasValue || !project.NgayKetThuc.HasValue) ? (double?)null :
                         DateTime.UtcNow.Date <= project.NgayBatDau.Value.Date ? 0.0 :
                         DateTime.UtcNow.Date >= project.NgayKetThuc.Value.Date ? 1.0 :
                         Math.Round((DateTime.UtcNow.Date - project.NgayBatDau.Value.Date).TotalDays / (project.NgayKetThuc.Value.Date - project.NgayBatDau.Value.Date).TotalDays, 2),
                CanhBaoRuiRo = (project.DaKetThuc || project.TrangThai == 2) ? "🟢 Hoàn thành" :
                               !project.NgayKetThuc.HasValue ? "⚪ Đang lập kế hoạch" :
                               (project.NgayKetThuc.Value.Date - DateTime.UtcNow.Date).Days < 0 ? "🔴 Trễ tiến độ" :
                               (project.NgayKetThuc.Value.Date - DateTime.UtcNow.Date).Days <= 30 ? "🟡 Nguy cơ trễ hạn" :
                               "🟢 Đúng tiến độ"
            };

            var catId = GetCategoryIdForProject(project.PhanLoaiDuAnId, project.PhanLoaiDuAnCode, project.PhanLoaiDuAnName);
            if (categoryProjectsMap.TryGetValue(catId, out var pList))
            {
                pList.Add(row);
            }
            else
            {
                categoryProjectsMap[categoryList.First().Id].Add(row);
            }
        }

        // Chuyển đổi số nguyên sang Chữ số La Mã (I, II, III, IV, V...)
        static string ToRomanNumber(int number)
        {
            if (number < 1) return string.Empty;
            var map = new (int val, string sym)[]
            {
                (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
                (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
                (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
            };
            var sb = new System.Text.StringBuilder();
            foreach (var (val, sym) in map)
            {
                while (number >= val)
                {
                    sb.Append(sym);
                    number -= val;
                }
            }
            return sb.ToString();
        }

        // 6. Danh sách dự án (Đã bỏ/comment dòng gom nhóm và dòng tổng cộng theo yêu cầu)
        var rows = new List<ReportRowDto>();
        // var subHeaders = new List<ReportRowDto>();
        // int catIdx = 1;
        int pIdx = 1;

        foreach (var cat in categoryList)
        {
            var pList = categoryProjectsMap.GetValueOrDefault(cat.Id, new List<ReportRowDto>());
            if (!pList.Any()) continue;

            // [COMMENTED] Bỏ dòng gom nhóm phân loại dự án (SubGroupHeader)
            /*
            var subHeader = new ReportRowDto
            {
                Stt = ToRomanNumber(catIdx++),
                RowType = "SubGroupHeader",
                ProjectName = cat.Name
            };
            PopulateSubGroupSummary(subHeader, pList);
            subHeaders.Add(subHeader);
            rows.Add(subHeader);
            */

            foreach (var pRow in pList)
            {
                pRow.Stt = pIdx++.ToString();
            }

            rows.AddRange(pList);
        }

        // --- GRAND TOTAL (Dòng tổng cộng cho các cột giá trị số tiền) ---
        if (rows.Any())
        {
            var grandTotal = new ReportRowDto
            {
                Stt = "",
                RowType = "GrandTotal",
                ProjectName = "TỔNG CỘNG"
            };
            PopulateSubGroupSummary(grandTotal, rows);
            rows.Add(grandTotal);
        }

        return new ReportResponseDto
        {
            Title = $"TÌNH HÌNH ĐẦU TƯ VÀ HUY ĐỘNG VỐN ĐỂ ĐẦU TƯ VÀO CÁC DỰ ÁN HÌNH THÀNH TSCĐ VÀ XDCB ({periodDisplayName.ToUpper()})",
            Unit = unitName,
            Year = year,
            Period = period,
            PeriodName = periodName,
            FromDate = startOfPeriod,
            ToDate = endOfPeriod,
            Rows = rows
        };
    }

    private void PopulateSubGroupSummary(ReportRowDto summaryRow, List<ReportRowDto> projectRows)
    {
        if (projectRows == null || !projectRows.Any()) return;

        summaryRow.TongMucDauTuTong = projectRows.Sum(r => r.TongMucDauTuTong);
        summaryRow.TongMucDauTuVCSH = projectRows.Sum(r => r.TongMucDauTuVCSH);
        summaryRow.TongMucDauTuVay = projectRows.Sum(r => r.TongMucDauTuVay);
        summaryRow.TongMucDauTuKhac = projectRows.Sum(r => r.TongMucDauTuKhac);

        summaryRow.KhoiLuongKyTruoc = projectRows.Sum(r => r.KhoiLuongKyTruoc);
        summaryRow.KhoiLuongTrongKy = projectRows.Sum(r => r.KhoiLuongTrongKy);
        summaryRow.KhoiLuongLuyKe = projectRows.Sum(r => r.KhoiLuongLuyKe);

        summaryRow.GiaiNganKyTruoc = projectRows.Sum(r => r.GiaiNganKyTruoc);
        summaryRow.GiaiNganTrongKy = projectRows.Sum(r => r.GiaiNganTrongKy);
        summaryRow.GiaiNganLuyKe = projectRows.Sum(r => r.GiaiNganLuyKe);

        summaryRow.TaiSanBanGiao = projectRows.Sum(r => r.TaiSanBanGiao);
    }

    private void PopulateGroupSummary(ReportRowDto summaryRow, List<ReportRowDto> subgroupRows)
    {
        if (subgroupRows == null || !subgroupRows.Any()) return;

        summaryRow.TongMucDauTuTong = subgroupRows.Sum(r => r.TongMucDauTuTong);
        summaryRow.TongMucDauTuVCSH = subgroupRows.Sum(r => r.TongMucDauTuVCSH);
        summaryRow.TongMucDauTuVay = subgroupRows.Sum(r => r.TongMucDauTuVay);
        summaryRow.TongMucDauTuKhac = subgroupRows.Sum(r => r.TongMucDauTuKhac);

        summaryRow.KhoiLuongKyTruoc = subgroupRows.Sum(r => r.KhoiLuongKyTruoc);
        summaryRow.KhoiLuongTrongKy = subgroupRows.Sum(r => r.KhoiLuongTrongKy);
        summaryRow.KhoiLuongLuyKe = subgroupRows.Sum(r => r.KhoiLuongLuyKe);

        summaryRow.GiaiNganKyTruoc = subgroupRows.Sum(r => r.GiaiNganKyTruoc);
        summaryRow.GiaiNganTrongKy = subgroupRows.Sum(r => r.GiaiNganTrongKy);
        summaryRow.GiaiNganLuyKe = subgroupRows.Sum(r => r.GiaiNganLuyKe);

        summaryRow.TaiSanBanGiao = subgroupRows.Sum(r => r.TaiSanBanGiao);
    }

    public async Task<byte[]> ExportInvestmentReportExcelAsync(int year, int period, string? donViTinh = null, int version = 1, DateTime? fromDate = null, DateTime? toDate = null)
    {
        if (version == 2)
        {
            return await ExportBaoCao1TienDoDuAnV2Async(year, period, donViTinh, fromDate, toDate);
        }

        var report = await GetInvestmentReportAsync(year, period, donViTinh, fromDate, toDate);
        
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Bao cao");
            
            // Font family
            worksheet.Style.Font.FontName = "Times New Roman";
            worksheet.Style.Font.FontSize = 11;

            // Left Header
            worksheet.Cell("A1").Value = "NGÂN HÀNG HỢP TÁC XÃ VIỆT NAM";
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 10;
            
            worksheet.Cell("A2").Value = "TRUNG TÂM CÔNG NGHỆ THÔNG TIN";
            worksheet.Cell("A2").Style.Font.Bold = true;
            worksheet.Cell("A2").Style.Font.Underline = XLFontUnderlineValues.Single;
            worksheet.Cell("A2").Style.Font.FontSize = 10;

            // Right Header
            worksheet.Cell("L1").Value = "Biểu số 02.A";
            worksheet.Cell("L1").Style.Font.Bold = true;
            worksheet.Cell("L1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            worksheet.Cell("L1").Style.Font.FontSize = 10;

            // Title
            worksheet.Cell("A4").Value = "TÌNH HÌNH ĐẦU TƯ VÀ HUY ĐỘNG VỐN ĐỂ ĐẦU TƯ VÀO CÁC DỰ ÁN";
            worksheet.Cell("A4").Style.Font.Bold = true;
            worksheet.Cell("A4").Style.Font.FontSize = 14;
            worksheet.Cell("A4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A4:L4").Merge();

            worksheet.Cell("A5").Value = "HÌNH THÀNH TSCĐ VÀ XDCB";
            worksheet.Cell("A5").Style.Font.Bold = true;
            worksheet.Cell("A5").Style.Font.FontSize = 14;
            worksheet.Cell("A5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A5:L5").Merge();

            worksheet.Cell("A6").Value = "(Ban hành kèm theo Thông tư số 200/2015/TT-BTC ngày 15/12/2015 của Bộ Tài chính)";
            worksheet.Cell("A6").Style.Font.Italic = true;
            worksheet.Cell("A6").Style.Font.FontSize = 10;
            worksheet.Cell("A6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A6:L6").Merge();

            string periodText = (report.FromDate.HasValue && report.ToDate.HasValue && (period == 0 || fromDate.HasValue || toDate.HasValue))
                ? $"Từ {report.FromDate:dd/MM/yyyy} đến {report.ToDate:dd/MM/yyyy}"
                : (period == 2 ? $"Trong kỳ báo cáo 6T đầu năm {year}" : (period == 3 ? $"Trong kỳ báo cáo 6T cuối năm {year}" : $"Trong kỳ báo cáo năm {year}"));
            worksheet.Cell("A7").Value = $"( {periodText} )";
            worksheet.Cell("A7").Style.Font.Bold = true;
            worksheet.Cell("A7").Style.Font.FontSize = 12;
            worksheet.Cell("A7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A7:L7").Merge();

            // Unit
            worksheet.Cell("L8").Value = $"Đơn vị tính: {report.Unit}";
            worksheet.Cell("L8").Style.Font.Italic = true;
            worksheet.Cell("L8").Style.Font.FontSize = 11;
            worksheet.Cell("L8").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Dates for headers
            string dateStr = report.ToDate.HasValue ? report.ToDate.Value.ToString("dd/MM/yyyy") : (period == 2 ? $"30/06/{year}" : $"31/12/{year}");

            // Merged Headers row 10-12
            worksheet.Cell("A10").Value = "TT";
            worksheet.Range("A10:A12").Merge();

            worksheet.Cell("B10").Value = "Tên dự án";
            worksheet.Range("B10:B12").Merge();

            worksheet.Cell("C10").Value = "Quyết định phê duyệt";
            worksheet.Range("C10:C12").Merge();

            worksheet.Cell("D10").Value = "Tổng mức vốn đầu tư";
            worksheet.Range("D10:E11").Merge();
            worksheet.Cell("D12").Value = "Tổng";
            worksheet.Cell("E12").Value = "Vốn chủ sở hữu";

            worksheet.Cell("F10").Value = $"Giá trị khối lượng thực hiện đến ngày {dateStr}";
            worksheet.Range("F10:H11").Merge();
            worksheet.Cell("F12").Value = "Kỳ trước chuyển sang";
            worksheet.Cell("G12").Value = "Thực hiện trong kỳ";
            worksheet.Cell("H12").Value = $"Thực hiện đến hết ngày {dateStr}";

            worksheet.Cell("I10").Value = $"Giải ngân đến ngày {dateStr}";
            worksheet.Range("I10:K11").Merge();
            worksheet.Cell("I12").Value = "Kỳ trước chuyển sang";
            worksheet.Cell("J12").Value = "Thực hiện trong kỳ";
            worksheet.Cell("K12").Value = $"Thực hiện đến hết ngày {dateStr}";

            worksheet.Cell("L10").Value = "Giá trị tài sản đã hoàn thành và đưa vào sử dụng";
            worksheet.Range("L10:L12").Merge();

            // Format Header Range A10:L12
            var headerRange = worksheet.Range("A10:L12");
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Alignment.WrapText = true;
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Column numbering row 13
            worksheet.Cell("A13").Value = "(1)";
            worksheet.Cell("B13").Value = "(2)";
            worksheet.Cell("C13").Value = "(3)";
            worksheet.Cell("D13").Value = "(4)";
            worksheet.Cell("E13").Value = "(5)";
            worksheet.Cell("F13").Value = "(13)";
            worksheet.Cell("G13").Value = "(14)";
            worksheet.Cell("H13").Value = "(15)";
            worksheet.Cell("I13").Value = "(16)";
            worksheet.Cell("J13").Value = "(17)";
            worksheet.Cell("K13").Value = "(18)";
            worksheet.Cell("L13").Value = "(19)";

            var numRange = worksheet.Range("A13:L13");
            numRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            numRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            numRange.Style.Font.Italic = true;
            numRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            numRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Data Rows
            int currentRow = 14;
            foreach (var row in report.Rows)
            {
                worksheet.Cell(currentRow, 1).Value = row.Stt;
                
                // Indent project name or empty placeholders
                string displayName = row.ProjectName;
                if (row.RowType == "ProjectRow" || row.RowType == "EmptyPlaceholder")
                {
                    displayName = "   " + row.ProjectName;
                }
                worksheet.Cell(currentRow, 2).Value = displayName;
                worksheet.Cell(currentRow, 3).Value = row.ApprovalDecision;

                if (row.RowType == "GroupHeader" || row.RowType == "SubGroupHeader" || row.RowType == "EmptyPlaceholder")
                {
                    // Set empty values for formula/summary columns to match template design
                    for (int col = 4; col <= 12; col++)
                    {
                        worksheet.Cell(currentRow, col).Value = string.Empty;
                    }
                }
                else
                {
                    worksheet.Cell(currentRow, 4).Value = row.TongMucDauTuTong;
                    worksheet.Cell(currentRow, 5).Value = row.TongMucDauTuVCSH;
                    worksheet.Cell(currentRow, 6).Value = row.KhoiLuongKyTruoc;
                    worksheet.Cell(currentRow, 7).Value = row.KhoiLuongTrongKy;
                    worksheet.Cell(currentRow, 8).Value = row.KhoiLuongLuyKe;
                    worksheet.Cell(currentRow, 9).Value = row.GiaiNganKyTruoc;
                    worksheet.Cell(currentRow, 10).Value = row.GiaiNganTrongKy;
                    worksheet.Cell(currentRow, 11).Value = row.GiaiNganLuyKe;
                    worksheet.Cell(currentRow, 12).Value = row.TaiSanBanGiao;
                }

                // Format Row
                var rowRange = worksheet.Range(currentRow, 1, currentRow, 12);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                if (row.RowType == "GroupHeader" || row.RowType == "SubGroupHeader" || row.RowType == "GroupFooter" || row.RowType == "GrandTotal")
                {
                    rowRange.Style.Font.Bold = true;
                }

                if (row.RowType == "SubGroupHeader")
                {
                    rowRange.Style.Font.Italic = true;
                }

                // Alignment
                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                if (row.RowType != "GroupHeader" && row.RowType != "SubGroupHeader" && row.RowType != "EmptyPlaceholder")
                {
                    for (int col = 4; col <= 12; col++)
                    {
                        var cell = worksheet.Cell(currentRow, col);
                        cell.Style.NumberFormat.Format = "#,##0";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                }

                currentRow++;
            }

            // Adjust column widths nicely
            worksheet.Column(1).Width = 6;   // TT
            worksheet.Column(2).Width = 45;  // Tên dự án
            worksheet.Column(3).Width = 35;  // Quyết định phê duyệt
            for (int col = 4; col <= 12; col++)
            {
                worksheet.Column(col).Width = 15; // Numeric columns
            }

            using (var memoryStream = new MemoryStream())
            {
                workbook.SaveAs(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }

    public async Task<byte[]> ExportInvestmentReportCsvAsync(int year, int period, string? donViTinh = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var report = await GetInvestmentReportAsync(year, period, donViTinh, fromDate, toDate);
        string dateStr = report.ToDate.HasValue ? report.ToDate.Value.ToString("dd/MM/yyyy") : (period == 2 ? $"30/06/{year}" : $"31/12/{year}");

        using (var memoryStream = new MemoryStream())
        {
            using (var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8))
            {
                // Write UTF-8 BOM
                writer.Write('\uFEFF');

                // Header rows
                await writer.WriteLineAsync($"\"NGÂN HÀNG HỢP TÁC XÃ VIỆT NAM\"");
                await writer.WriteLineAsync($"\"TRUNG TÂM CÔNG NGHỆ THÔNG TIN\"");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"\"TÌNH HÌNH ĐẦU TƯ VÀ HUY ĐỘNG VỐN ĐỂ ĐẦU TƯ VÀO CÁC DỰ ÁN HÌNH THÀNH TSCĐ VÀ XDCB\"");
                string periodText = (report.FromDate.HasValue && report.ToDate.HasValue && (period == 0 || fromDate.HasValue || toDate.HasValue))
                    ? $"Từ {report.FromDate:dd/MM/yyyy} đến {report.ToDate:dd/MM/yyyy}"
                    : (period == 2 ? $"Trong kỳ báo cáo 6T đầu năm {year}" : (period == 3 ? $"Trong kỳ báo cáo 6T cuối năm {year}" : $"Trong kỳ báo cáo năm {year}"));
                await writer.WriteLineAsync($"\"( {periodText} )\"");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"\"Đơn vị tính: {report.Unit}\"");
                await writer.WriteLineAsync();

                // Table Columns
                await writer.WriteLineAsync($"\"TT\",\"Tên dự án\",\"Quyết định phê duyệt\",\"Tổng mức vốn đầu tư - Tổng\",\"Tổng mức vốn đầu tư - Vốn chủ sở hữu\",\"Giá trị khối lượng thực hiện đến ngày {dateStr} - Kỳ trước chuyển sang\",\"Giá trị khối lượng thực hiện đến ngày {dateStr} - Thực hiện trong kỳ\",\"Giá trị khối lượng thực hiện đến ngày {dateStr} - Thực hiện đến hết ngày\",\"Giải ngân đến ngày {dateStr} - Kỳ trước chuyển sang\",\"Giải ngân đến ngày {dateStr} - Thực hiện trong kỳ\",\"Giải ngân đến ngày {dateStr} - Thực hiện đến hết ngày\",\"Giá trị tài sản đã hoàn thành và đưa vào sử dụng\"");
                await writer.WriteLineAsync($"\"(1)\",\"(2)\",\"(3)\",\"(4)\",\"(5)\",\"(13)\",\"(14)\",\"(15)\",\"(16)\",\"(17)\",\"(18)\",\"(19)\"");

                foreach (var row in report.Rows)
                {
                    string stt = EscapeCsvField(row.Stt);
                    string projName = EscapeCsvField(row.ProjectName);
                    string decision = EscapeCsvField(row.ApprovalDecision);

                    if (row.RowType == "GroupHeader" || row.RowType == "SubGroupHeader" || row.RowType == "EmptyPlaceholder")
                    {
                        await writer.WriteLineAsync($"\"{stt}\",\"{projName}\",\"{decision}\",\"\",\"\",\"\",\"\",\"\",\"\",\"\",\"\",\"\"");
                    }
                    else
                    {
                        await writer.WriteLineAsync($"\"{stt}\",\"{projName}\",\"{decision}\",\"{row.TongMucDauTuTong}\",\"{row.TongMucDauTuVCSH}\",\"{row.KhoiLuongKyTruoc}\",\"{row.KhoiLuongTrongKy}\",\"{row.KhoiLuongLuyKe}\",\"{row.GiaiNganKyTruoc}\",\"{row.GiaiNganTrongKy}\",\"{row.GiaiNganLuyKe}\",\"{row.TaiSanBanGiao}\"");
                    }
                }

                await writer.FlushAsync();
            }
            return memoryStream.ToArray();
        }
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;
        return field.Replace("\"", "\"\"");
    }

    public async Task<byte[]> ExportInvestmentReportHtmlAsync(int year, int period, string? donViTinh = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var report = await GetInvestmentReportAsync(year, period, donViTinh, fromDate, toDate);
        string dateStr = report.ToDate.HasValue ? report.ToDate.Value.ToString("dd/MM/yyyy") : (period == 2 ? $"30/06/{year}" : $"31/12/{year}");
        string periodText = (report.FromDate.HasValue && report.ToDate.HasValue && (period == 0 || fromDate.HasValue || toDate.HasValue))
            ? $"Từ {report.FromDate:dd/MM/yyyy} đến {report.ToDate:dd/MM/yyyy}"
            : (period == 2 ? $"Trong kỳ báo cáo 6T đầu năm {year}" : (period == 3 ? $"Trong kỳ báo cáo 6T cuối năm {year}" : $"Trong kỳ báo cáo năm {year}"));

        var htmlBuilder = new System.Text.StringBuilder();
        htmlBuilder.AppendLine("<!DOCTYPE html>");
        htmlBuilder.AppendLine("<html>");
        htmlBuilder.AppendLine("<head>");
        htmlBuilder.AppendLine("<meta charset=\"utf-8\" />");
        htmlBuilder.AppendLine("<title>" + System.Web.HttpUtility.HtmlEncode(report.Title) + "</title>");
        htmlBuilder.AppendLine("<style>");
        htmlBuilder.AppendLine("  body { font-family: 'Times New Roman', Times, serif; margin: 30px; font-size: 13px; color: #333; }");
        htmlBuilder.AppendLine("  .header-table { width: 100%; border: none; margin-bottom: 25px; }");
        htmlBuilder.AppendLine("  .header-table td { border: none; padding: 2px; }");
        htmlBuilder.AppendLine("  .title-section { text-align: center; margin-bottom: 25px; }");
        htmlBuilder.AppendLine("  .title-section h2 { margin: 5px 0; font-size: 16px; font-weight: bold; }");
        htmlBuilder.AppendLine("  .title-section h3 { margin: 5px 0; font-size: 13px; font-weight: normal; font-style: italic; }");
        htmlBuilder.AppendLine("  .title-section h4 { margin: 5px 0; font-size: 14px; font-weight: bold; }");
        htmlBuilder.AppendLine("  .unit-line { text-align: right; font-style: italic; margin-bottom: 10px; font-size: 12px; }");
        htmlBuilder.AppendLine("  table.data-table { width: 100%; border-collapse: collapse; margin-top: 10px; }");
        htmlBuilder.AppendLine("  table.data-table th, table.data-table td { border: 1px solid #000; padding: 6px 8px; vertical-align: middle; }");
        htmlBuilder.AppendLine("  table.data-table th { background-color: #F2F2F2; font-weight: bold; text-align: center; }");
        htmlBuilder.AppendLine("  .text-center { text-align: center; }");
        htmlBuilder.AppendLine("  .text-left { text-align: left; }");
        htmlBuilder.AppendLine("  .text-right { text-align: right; }");
        htmlBuilder.AppendLine("  .bold { font-weight: bold; }");
        htmlBuilder.AppendLine("  .italic { font-style: italic; }");
        htmlBuilder.AppendLine("  .indent { padding-left: 20px !important; }");
        htmlBuilder.AppendLine("</style>");
        htmlBuilder.AppendLine("</head>");
        htmlBuilder.AppendLine("<body>");

        // Top Metadata Headers
        htmlBuilder.AppendLine("<table class=\"header-table\">");
        htmlBuilder.AppendLine("  <tr>");
        htmlBuilder.AppendLine("    <td style=\"width: 50%; font-weight: bold; font-size: 12px;\">");
        htmlBuilder.AppendLine("      NGÂN HÀNG HỢP TÁC XÃ VIỆT NAM<br/>");
        htmlBuilder.AppendLine("      <span style=\"text-decoration: underline;\">TRUNG TÂM CÔNG NGHỆ THÔNG TIN</span>");
        htmlBuilder.AppendLine("    </td>");
        htmlBuilder.AppendLine("    <td style=\"width: 50%; text-align: right; font-weight: bold; font-size: 12px; vertical-align: top;\">");
        htmlBuilder.AppendLine("      Biểu số 02.A");
        htmlBuilder.AppendLine("    </td>");
        htmlBuilder.AppendLine("  </tr>");
        htmlBuilder.AppendLine("</table>");

        // Title Section
        htmlBuilder.AppendLine("<div class=\"title-section\">");
        htmlBuilder.AppendLine("  <h2>TÌNH HÌNH ĐẦU TƯ VÀ HUY ĐỘNG VỐN ĐỂ ĐẦU TƯ VÀO CÁC DỰ ÁN</h2>");
        htmlBuilder.AppendLine("  <h2>HÌNH THÀNH TSCĐ VÀ XDCB</h2>");
        htmlBuilder.AppendLine("  <h3>(Ban hành kèm theo Thông tư số 200/2015/TT-BTC ngày 15/12/2015 của Bộ Tài chính)</h3>");
        htmlBuilder.AppendLine("  <h4>( " + periodText + " )</h4>");
        htmlBuilder.AppendLine("</div>");

        // Unit
        htmlBuilder.AppendLine($"<div class=\"unit-line\">Đơn vị tính: {System.Web.HttpUtility.HtmlEncode(report.Unit)}</div>");

        // Data Table Headers
        htmlBuilder.AppendLine("<table class=\"data-table\">");
        htmlBuilder.AppendLine("  <thead>");
        htmlBuilder.AppendLine("    <tr>");
        htmlBuilder.AppendLine("      <th rowspan=\"3\" style=\"width: 4%;\">TT</th>");
        htmlBuilder.AppendLine("      <th rowspan=\"3\" style=\"width: 25%;\">Tên dự án</th>");
        htmlBuilder.AppendLine("      <th rowspan=\"3\" style=\"width: 15%;\">Quyết định phê duyệt</th>");
        htmlBuilder.AppendLine("      <th colspan=\"2\" style=\"width: 14%;\">Tổng mức vốn đầu tư</th>");
        htmlBuilder.AppendLine("      <th colspan=\"3\" style=\"width: 21%;\">Giá trị khối lượng thực hiện đến ngày " + dateStr + "</th>");
        htmlBuilder.AppendLine("      <th colspan=\"3\" style=\"width: 21%;\">Giải ngân đến ngày " + dateStr + "</th>");
        htmlBuilder.AppendLine("      <th rowspan=\"3\" style=\"width: 10%;\">Giá trị tài sản đã hoàn thành và đưa vào sử dụng</th>");
        htmlBuilder.AppendLine("    </tr>");
        htmlBuilder.AppendLine("    <tr>");
        htmlBuilder.AppendLine("      <th rowspan=\"2\">Tổng</th>");
        htmlBuilder.AppendLine("      <th rowspan=\"2\">Vốn chủ sở hữu</th>");
        htmlBuilder.AppendLine("      <th rowspan=\"1\">Kỳ trước chuyển sang</th>");
        htmlBuilder.AppendLine("      <th rowspan=\"1\">Thực hiện trong kỳ</th>");
        htmlBuilder.AppendLine("      <th rowspan=\"1\">Thực hiện đến hết ngày " + dateStr + "</th>");
        htmlBuilder.AppendLine("      <th rowspan=\"1\">Kỳ trước chuyển sang</th>");
        htmlBuilder.AppendLine("      <th rowspan=\"1\">Thực hiện trong kỳ</th>");
        htmlBuilder.AppendLine("      <th rowspan=\"1\">Thực hiện đến hết ngày " + dateStr + "</th>");
        htmlBuilder.AppendLine("    </tr>");
        htmlBuilder.AppendLine("    <tr>");
        htmlBuilder.AppendLine("      <th>(13)</th>");
        htmlBuilder.AppendLine("      <th>(14)</th>");
        htmlBuilder.AppendLine("      <th>(15)</th>");
        htmlBuilder.AppendLine("      <th>(16)</th>");
        htmlBuilder.AppendLine("      <th>(17)</th>");
        htmlBuilder.AppendLine("      <th>(18)</th>");
        htmlBuilder.AppendLine("    </tr>");
        htmlBuilder.AppendLine("    <tr style=\"font-style: italic; font-size: 11px;\">");
        htmlBuilder.AppendLine("      <th class=\"text-center\">(1)</th>");
        htmlBuilder.AppendLine("      <th class=\"text-center\">(2)</th>");
        htmlBuilder.AppendLine("      <th class=\"text-center\">(3)</th>");
        htmlBuilder.AppendLine("      <th class=\"text-center\">(4)</th>");
        htmlBuilder.AppendLine("      <th class=\"text-center\">(5)</th>");
        htmlBuilder.AppendLine("      <th class=\"text-center\">(13)</th>");
        htmlBuilder.AppendLine("      <th class=\"text-center\">(14)</th>");
        htmlBuilder.AppendLine("      <th class=\"text-center\">(15)</th>");
        htmlBuilder.AppendLine("      <th class=\"text-center\">(16)</th>");
        htmlBuilder.AppendLine("      <th class=\"text-center\">(17)</th>");
        htmlBuilder.AppendLine("      <th class=\"text-center\">(18)</th>");
        htmlBuilder.AppendLine("      <th class=\"text-center\">(19)</th>");
        htmlBuilder.AppendLine("    </tr>");
        htmlBuilder.AppendLine("  </thead>");
        htmlBuilder.AppendLine("  <tbody>");

        // Data Rows
        foreach (var row in report.Rows)
        {
            string rowClass = "";
            if (row.RowType == "GroupHeader" || row.RowType == "SubGroupHeader" || row.RowType == "GroupFooter" || row.RowType == "GrandTotal")
            {
                rowClass += " bold";
            }
            if (row.RowType == "SubGroupHeader")
            {
                rowClass += " italic";
            }

            string nameClass = "";
            if (row.RowType == "ProjectRow" || row.RowType == "EmptyPlaceholder")
            {
                nameClass = "class=\"indent\"";
            }

            htmlBuilder.AppendLine("    <tr class=\"" + rowClass + "\">");
            htmlBuilder.AppendLine("      <td class=\"text-center\">" + System.Web.HttpUtility.HtmlEncode(row.Stt) + "</td>");
            htmlBuilder.AppendLine("      <td " + nameClass + ">" + System.Web.HttpUtility.HtmlEncode(row.ProjectName) + "</td>");
            htmlBuilder.AppendLine("      <td>" + System.Web.HttpUtility.HtmlEncode(row.ApprovalDecision) + "</td>");

            if (row.RowType == "GroupHeader" || row.RowType == "SubGroupHeader" || row.RowType == "EmptyPlaceholder")
            {
                for (int col = 4; col <= 12; col++)
                {
                    htmlBuilder.AppendLine("      <td></td>");
                }
            }
            else
            {
                htmlBuilder.AppendLine("      <td class=\"text-right\">" + (row.TongMucDauTuTong == 0 ? "-" : row.TongMucDauTuTong.ToString("#,##0")) + "</td>");
                htmlBuilder.AppendLine("      <td class=\"text-right\">" + (row.TongMucDauTuVCSH == 0 ? "-" : row.TongMucDauTuVCSH.ToString("#,##0")) + "</td>");
                htmlBuilder.AppendLine("      <td class=\"text-right\">" + (row.KhoiLuongKyTruoc == 0 ? "-" : row.KhoiLuongKyTruoc.ToString("#,##0")) + "</td>");
                htmlBuilder.AppendLine("      <td class=\"text-right\">" + (row.KhoiLuongTrongKy == 0 ? "-" : row.KhoiLuongTrongKy.ToString("#,##0")) + "</td>");
                htmlBuilder.AppendLine("      <td class=\"text-right\">" + (row.KhoiLuongLuyKe == 0 ? "-" : row.KhoiLuongLuyKe.ToString("#,##0")) + "</td>");
                htmlBuilder.AppendLine("      <td class=\"text-right\">" + (row.GiaiNganKyTruoc == 0 ? "-" : row.GiaiNganKyTruoc.ToString("#,##0")) + "</td>");
                htmlBuilder.AppendLine("      <td class=\"text-right\">" + (row.GiaiNganTrongKy == 0 ? "-" : row.GiaiNganTrongKy.ToString("#,##0")) + "</td>");
                htmlBuilder.AppendLine("      <td class=\"text-right\">" + (row.GiaiNganLuyKe == 0 ? "-" : row.GiaiNganLuyKe.ToString("#,##0")) + "</td>");
                htmlBuilder.AppendLine("      <td class=\"text-right\">" + (row.TaiSanBanGiao == 0 ? "-" : row.TaiSanBanGiao.ToString("#,##0")) + "</td>");
            }

            htmlBuilder.AppendLine("    </tr>");
        }

        htmlBuilder.AppendLine("  </tbody>");
        htmlBuilder.AppendLine("</table>");
        htmlBuilder.AppendLine("</body>");
        htmlBuilder.AppendLine("</html>");

        return System.Text.Encoding.UTF8.GetBytes(htmlBuilder.ToString());
    }



    private async Task<byte[]> ExportBaoCao1TienDoDuAnV2Async(int year, int period, string? donViTinh, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var report = await GetInvestmentReportAsync(year, period, donViTinh, fromDate, toDate);
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Tiến độ Dự án");
        worksheet.Style.Font.FontName = "Times New Roman";
        worksheet.Style.Font.FontSize = 11;

        // Title Row 2
        worksheet.Cell("B2").Value = "BÁO CÁO TIẾN ĐỘ DỰ ÁN";
        worksheet.Cell("B2").Style.Font.Bold = true;
        worksheet.Cell("B2").Style.Font.FontSize = 14;
        worksheet.Cell("B2").Style.Font.FontColor = XLColor.FromHtml("#1F4E78");
        worksheet.Cell("B2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range("B2:M2").Merge();

        // Subtitle Row 3
        string periodText = (report.FromDate.HasValue && report.ToDate.HasValue && (period == 0 || fromDate.HasValue || toDate.HasValue))
            ? $"Từ {report.FromDate:dd/MM/yyyy} đến {report.ToDate:dd/MM/yyyy}"
            : (period == 2 ? $"6 tháng đầu năm {year}" : (period == 3 ? $"6 tháng cuối năm {year}" : $"Cả năm {year}"));
        worksheet.Cell("B3").Value = $"(Kỳ báo cáo: {periodText} - Đơn vị tính: {report.Unit})";
        worksheet.Cell("B3").Style.Font.Italic = true;
        worksheet.Cell("B3").Style.Font.FontSize = 10;
        worksheet.Cell("B3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range("B3:M3").Merge();

        // Header Row 5
        string[] headers = [
            "STT", "Mã dự án", "Tên dự án triển khai", "Đơn vị chủ trì", "PM phụ trách",
            "Loại dự án", "Ngày bắt đầu", "Ngày kết thúc", "Tiến độ", "Thời gian còn lại (ngày)",
            "Trạng thái thực tế", "Cảnh báo rủi ro (RAG)"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(5, 2 + i);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 12;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        int currentRow = 6;
        int stt = 1;
        var projectRows = report.Rows.Where(r => r.RowType == "ProjectRow").ToList();

        foreach (var p in projectRows)
        {
            worksheet.Cell(currentRow, 2).Value = stt++;
            worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 3).Value = p.MaDuAn ?? string.Empty;
            worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 4).Value = p.ProjectName;
            worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            worksheet.Cell(currentRow, 5).Value = p.DonViChuTri ?? string.Empty;
            worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            worksheet.Cell(currentRow, 6).Value = p.PmPhuTrach ?? string.Empty;
            worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            worksheet.Cell(currentRow, 7).Value = p.LoaiDuAn ?? string.Empty;
            worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            if (p.NgayBatDau.HasValue)
            {
                worksheet.Cell(currentRow, 8).Value = p.NgayBatDau.Value;
                worksheet.Cell(currentRow, 8).Style.DateFormat.Format = "dd/MM/yyyy";
                worksheet.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            else
            {
                worksheet.Cell(currentRow, 8).Value = string.Empty;
            }

            if (p.NgayKetThuc.HasValue)
            {
                worksheet.Cell(currentRow, 9).Value = p.NgayKetThuc.Value;
                worksheet.Cell(currentRow, 9).Style.DateFormat.Format = "dd/MM/yyyy";
                worksheet.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            else
            {
                worksheet.Cell(currentRow, 9).Value = string.Empty;
            }

            if (p.TienDo.HasValue)
            {
                worksheet.Cell(currentRow, 10).Value = p.TienDo.Value;
                worksheet.Cell(currentRow, 10).Style.NumberFormat.Format = "0.0%";
                worksheet.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }
            else
            {
                worksheet.Cell(currentRow, 10).Value = 0.0;
                worksheet.Cell(currentRow, 10).Style.NumberFormat.Format = "0.0%";
                worksheet.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }

            if (p.ThoiGianConLaiNgay.HasValue)
            {
                worksheet.Cell(currentRow, 11).Value = p.ThoiGianConLaiNgay.Value;
                worksheet.Cell(currentRow, 11).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }
            else
            {
                worksheet.Cell(currentRow, 11).Value = string.Empty;
            }

            worksheet.Cell(currentRow, 12).Value = p.TrangThaiThucTe ?? string.Empty;
            worksheet.Cell(currentRow, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 13).Value = p.CanhBaoRuiRo ?? string.Empty;
            worksheet.Cell(currentRow, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var rowRange = worksheet.Range(currentRow, 2, currentRow, 13);
            rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            currentRow++;
        }

        worksheet.Columns(2, 13).AdjustToContents(10.0, 50.0);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    #region Biểu số 02.A (Đơn vị tính cố định: Tỷ đồng)

    public async Task<ReportResponseDto> GetBieuMau02AReportAsync(int year, int period, DateTime? fromDate = null, DateTime? toDate = null)
    {
        const decimal conversionFactor = 1_000_000_000m; // Cố định đơn vị tính: Tỷ đồng
        const string unitName = "Tỷ đồng";

        // 1. Tính toán thời gian kỳ báo cáo
        var (startOfPeriod, endOfPeriod, periodDisplayName, periodName) = CalculateReportPeriod(year, period, fromDate, toDate);

        // 2. Tải danh sách dự án hợp lệ
        // QUY TẮC BẮT BUỘC: Chỉ lấy các dự án ĐÃ CÓ QUYẾT ĐỊNH THÀNH LẬP DỰ ÁN
        var query = _context.DuAns
            .AsNoTracking()
            .Where(da => da.IsActive && !da.IsDeleted && da.TrangThai != (int)TrangThaiDuAn.Merged)
            .Where(da => !string.IsNullOrWhiteSpace(da.SoQuyetDinhThanhLap) || da.NgayQuyetDinhThanhLap.HasValue);

        // Phân quyền người dùng
        if (_currentUserService != null)
        {
            var currentUsername = _currentUserService.GetUsername();
            if (!string.IsNullOrEmpty(currentUsername))
            {
                var currentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);
                if (currentUser != null && !currentUser.IsSystemAdmin)
                {
                    query = query.Where(da => da.CreatedByUserId == currentUser.Id || da.ChuDuAnId == currentUser.Id
                        || _context.UserPermissions.Any(up => up.UserId == currentUser.Id && up.DuAnId == da.Id)
                        || _context.CongViecNguoiLienQuans.Any(nlq => nlq.UserId == currentUser.Id && nlq.CongViecGoiThau != null && nlq.CongViecGoiThau.GoiThau != null && nlq.CongViecGoiThau.GoiThau.DuAnId == da.Id)
                        || (currentUser.CanViewHopDong && _context.HopDongs.Any(h => h.DuAnId == da.Id && h.LoaiHopDongNavigation != null && h.LoaiHopDongNavigation.Code == "01")));
                }
            }
        }

        var rawProjects = await query
            .Select(da => new
            {
                da.Id,
                da.Name,
                da.Code,
                da.DuToanPheDuyet,
                da.SoQuyetDinh,
                da.SoQuyetDinhPheDuyetDuToan,
                da.SoQuyetDinhThanhLap,
                da.NgayQuyetDinhThanhLap,
                da.ThoiGianThucHien,
                da.NgayBatDau,
                da.NgayKetThuc,
                da.NgayKetThucThucTe,
                da.NamBatDau,
                da.NamKetThuc,
                da.UpdatedAt,
                da.CreatedAt,
                da.TrangThai,
                da.DaKetThuc,
                da.ToChucThucHien,
                da.ChuDauTu,
                PhanLoaiDuAnId = da.PhanLoaiDuAnId,
                PhanLoaiDuAnCode = da.PhanLoaiDuAn != null ? da.PhanLoaiDuAn.Code : null,
                PhanLoaiDuAnName = da.PhanLoaiDuAn != null ? da.PhanLoaiDuAn.Name : null,
                DanhSachNguonVon = da.DanhSachNguonVon.Select(nv => new
                {
                    nv.Id,
                    nv.SoTien,
                    nv.NguonVonId,
                    NguonVonCode = nv.NguonVon != null ? nv.NguonVon.Code : null,
                    NguonVonName = nv.NguonVon != null ? nv.NguonVon.Name : null
                }).ToList()
            })
            .ToListAsync();

        // 3. Tải số liệu thanh toán giải ngân thực tế từ CSDL
        var targetDuAnIds = rawProjects.Select(p => p.Id).ToList();
        var performedValues = targetDuAnIds.Any()
            ? await _context.DotThanhToans
                .AsNoTracking()
                .Where(dt => dt.IsPaid 
                    && dt.HopDong != null 
                    && dt.HopDong.IsActive 
                    && !dt.HopDong.IsDeleted 
                    && dt.HopDong.DuAnId.HasValue
                    && targetDuAnIds.Contains(dt.HopDong.DuAnId.Value))
                .Select(dt => new
                {
                    DuAnId = dt.HopDong.DuAnId!.Value,
                    PaymentDate = dt.NgayThanhToanThucTe ?? dt.NgayThanhToan ?? dt.CreatedAt,
                    dt.GiaTriThanhToan
                })
                .GroupBy(x => x.DuAnId)
                .Select(g => new
                {
                    DuAnId = g.Key,
                    KyTruoc = g.Where(x => x.PaymentDate < startOfPeriod).Sum(x => x.GiaTriThanhToan),
                    TrongKy = g.Where(x => x.PaymentDate >= startOfPeriod && x.PaymentDate <= endOfPeriod).Sum(x => x.GiaTriThanhToan)
                })
                .ToDictionaryAsync(x => x.DuAnId, x => x)
            : new();

        // 4. Chuẩn bị danh sách dự án kèm thông số đã tính toán
        var mappedProjects = rawProjects.Select(p =>
        {
            // Xác định ngày thành lập
            DateTime ngayThanhLap = p.NgayQuyetDinhThanhLap 
                ?? p.NgayBatDau 
                ?? p.CreatedAt;

            decimal totalNguonVonVnd = p.DanhSachNguonVon?.Sum(nv => nv.SoTien) ?? 0m;
            decimal totalBudgetVnd = p.DuToanPheDuyet > 0 ? p.DuToanPheDuyet : totalNguonVonVnd;

            decimal rawVcshVnd = 0;
            decimal rawVayVnd = 0;
            decimal rawKhacVnd = 0;

            if (p.DanhSachNguonVon != null && p.DanhSachNguonVon.Any())
            {
                foreach (var nv in p.DanhSachNguonVon)
                {
                    var code = (nv.NguonVonCode ?? string.Empty).ToLowerInvariant();
                    var name = (nv.NguonVonName ?? string.Empty).ToLowerInvariant();

                    if (code.Contains("vay") || name.Contains("vay") || name.Contains("tín dụng") || name.Contains("tin dung"))
                    {
                        rawVayVnd += nv.SoTien;
                    }
                    else if (code.Contains("khac") || code.Contains("nv_khac") || name.Contains("khác") || name.Contains("khac"))
                    {
                        rawKhacVnd += nv.SoTien;
                    }
                    else
                    {
                        rawVcshVnd += nv.SoTien;
                    }
                }
            }
            else
            {
                rawVcshVnd = totalBudgetVnd;
            }

            if (rawVcshVnd == 0 && rawVayVnd == 0 && rawKhacVnd == 0 && totalBudgetVnd > 0)
            {
                rawVcshVnd = totalBudgetVnd;
            }

            performedValues.TryGetValue(p.Id, out var perf);
            decimal performedKyTruocVnd = perf?.KyTruoc ?? 0;
            decimal performedTrongKyVnd = perf?.TrongKy ?? 0;
            decimal performedLuyKeVnd = performedKyTruocVnd + performedTrongKyVnd;

            // Tài sản bàn giao
            decimal taiSanBanGiaoVnd = 0;
            if (p.TrangThai == (int)TrangThaiDuAn.HoanThanh || p.DaKetThuc)
            {
                var effectiveEndDate = p.NgayKetThucThucTe ?? p.NgayKetThuc ?? p.UpdatedAt;
                if (!effectiveEndDate.HasValue || effectiveEndDate.Value <= endOfPeriod)
                {
                    taiSanBanGiaoVnd = performedLuyKeVnd;
                }
            }

            // Văn bản quyết định thành lập / phê duyệt
            string approvalDecision = !string.IsNullOrWhiteSpace(p.SoQuyetDinhThanhLap)
                ? p.SoQuyetDinhThanhLap
                : (!string.IsNullOrWhiteSpace(p.SoQuyetDinh) ? p.SoQuyetDinh : p.SoQuyetDinhPheDuyetDuToan ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(approvalDecision) && !approvalDecision.Contains("ngày"))
            {
                var dDate = p.NgayQuyetDinhThanhLap ?? p.NgayBatDau;
                if (dDate.HasValue)
                {
                    approvalDecision = $"{approvalDecision} ngày {dDate.Value:dd/MM/yyyy}";
                }
            }

            // Thời gian đầu tư theo KH
            string thoiGianKh = !string.IsNullOrWhiteSpace(p.ThoiGianThucHien)
                ? p.ThoiGianThucHien
                : (p.NamBatDau.HasValue && p.NamKetThuc.HasValue
                    ? $"{p.NamBatDau} - {p.NamKetThuc}"
                    : (p.NgayBatDau.HasValue && p.NgayKetThuc.HasValue
                        ? $"{p.NgayBatDau:dd/MM/yyyy} - {p.NgayKetThuc:dd/MM/yyyy}"
                        : string.Empty));

            // Xác định Nhóm quy mô theo Tổng mức đầu tư
            // Nhóm A: > 800 tỷ, Nhóm B: 45 - 800 tỷ, Nhóm C: < 45 tỷ
            string nhomQuyMo = totalBudgetVnd > 800_000_000_000m ? "A"
                : (totalBudgetVnd >= 45_000_000_000m ? "B" : "C");

            // Xác định Lĩnh vực: I. XDCB, II. CNTT, III. Khác
            string linhVuc = "Khac";
            var plCode = (p.PhanLoaiDuAnCode ?? string.Empty).ToLowerInvariant();
            var plName = (p.PhanLoaiDuAnName ?? string.Empty).ToLowerInvariant();
            if (plCode.Contains("xdcb") || plName.Contains("xây dựng") || plName.Contains("xay dung"))
            {
                linhVuc = "XDCB";
            }
            else if (plCode.Contains("cntt") || plCode.Contains("it") || plName.Contains("công nghệ") || plName.Contains("cntt") || plName.Contains("phần mềm"))
            {
                linhVuc = "CNTT";
            }

            return new
            {
                p.Id,
                p.Name,
                p.Code,
                NgayThanhLap = ngayThanhLap,
                IsTruocKy = ngayThanhLap < startOfPeriod,
                IsTrongKy = ngayThanhLap >= startOfPeriod && ngayThanhLap <= endOfPeriod,
                IsSauKy = ngayThanhLap > endOfPeriod,
                NhomQuyMo = nhomQuyMo,
                LinhVuc = linhVuc,
                ThoiGianKh = thoiGianKh,
                ApprovalDecision = approvalDecision,
                DuToanPheDuyetVnd = totalBudgetVnd,
                BudgetTotal = totalBudgetVnd / conversionFactor,
                BudgetVcsh = rawVcshVnd / conversionFactor,
                BudgetVay = rawVayVnd / conversionFactor,
                BudgetKhac = rawKhacVnd / conversionFactor,
                KhoiLuongKyTruoc = performedKyTruocVnd / conversionFactor,
                KhoiLuongTrongKy = performedTrongKyVnd / conversionFactor,
                KhoiLuongLuyKe = performedLuyKeVnd / conversionFactor,
                GiaiNganKyTruoc = performedKyTruocVnd / conversionFactor, // Cột 16 = Cột 13
                GiaiNganTrongKy = performedTrongKyVnd / conversionFactor, // Cột 17 = Cột 14
                GiaiNganLuyKe = performedLuyKeVnd / conversionFactor,     // Cột 18 = Cột 15
                TaiSanBanGiao = taiSanBanGiaoVnd / conversionFactor
            };
        })
        .Where(x => !x.IsSauKy) // Loại bỏ các dự án thành lập sau kỳ báo cáo
        .ToList();

        // 5. Xây dựng cấu trúc danh sách dòng trả về (Phần A và Phần B)
        var resultRows = new List<ReportRowDto>();

        void BuildBlock(string blockLetter, string blockTitle, IEnumerable<dynamic> blockProjects)
        {
            var pList = blockProjects.ToList();

            // 1. Dòng Tiêu đề Phần
            resultRows.Add(new ReportRowDto
            {
                Stt = blockLetter,
                RowType = "BlockHeader",
                ProjectName = blockTitle
            });

            var groupKeys = new[] { ("A", "I", "Nhóm A (Tổng mức đầu tư > 800 tỷ đồng)"), 
                                   ("B", "II", "Nhóm B (Tổng mức đầu tư từ 45 tỷ đến 800 tỷ đồng)"), 
                                   ("C", "III", "Nhóm C (Tổng mức đầu tư < 45 tỷ đồng)") };

            int projectIndexInBlock = 1;

            foreach (var (gKey, roman, gTitle) in groupKeys)
            {
                var gProjects = pList.Where(p => p.NhomQuyMo == gKey).ToList();

                // Dòng tiêu đề Nhóm quy mô
                resultRows.Add(new ReportRowDto
                {
                    Stt = roman,
                    RowType = "GroupHeader",
                    ProjectName = $"{roman}. {gTitle}"
                });

                var domains = new[] { ("XDCB", "1. Dự án Xây dựng cơ bản"), 
                                     ("CNTT", "2. Dự án Công nghệ thông tin"), 
                                     ("Khac", "3. Dự án Khác") };

                foreach (var (dKey, dTitle) in domains)
                {
                    var dProjects = gProjects.Where(p => p.LinhVuc == dKey).ToList();
                    if (!dProjects.Any()) continue;

                    // Dòng tiêu đề Lĩnh vực
                    resultRows.Add(new ReportRowDto
                    {
                        Stt = string.Empty,
                        RowType = "SubGroupHeader",
                        ProjectName = $"   {dTitle}"
                    });

                    // Các dòng dự án
                    foreach (var p in dProjects)
                    {
                        resultRows.Add(new ReportRowDto
                        {
                            Stt = projectIndexInBlock.ToString(),
                            RowType = "ProjectRow",
                            ProjectName = p.Name,
                            MaDuAn = p.Code,
                            ApprovalDecision = p.ApprovalDecision,
                            ThoiGianThucHien = p.ThoiGianKh,
                            TongMucDauTuTong = p.BudgetTotal,
                            TongMucDauTuVCSH = p.BudgetVcsh,
                            TongMucDauTuVay = p.BudgetVay,
                            TongMucDauTuKhac = p.BudgetKhac,
                            KhoiLuongKyTruoc = p.KhoiLuongKyTruoc,
                            KhoiLuongTrongKy = p.KhoiLuongTrongKy,
                            KhoiLuongLuyKe = p.KhoiLuongLuyKe,
                            GiaiNganKyTruoc = p.GiaiNganKyTruoc,
                            GiaiNganTrongKy = p.GiaiNganTrongKy,
                            GiaiNganLuyKe = p.GiaiNganLuyKe,
                            TaiSanBanGiao = p.TaiSanBanGiao
                        });
                        projectIndexInBlock++;
                    }
                }
            }

            // Dòng Cộng của Phần
            resultRows.Add(new ReportRowDto
            {
                Stt = $"Cộng {blockLetter}",
                RowType = "BlockFooter",
                ProjectName = $"Cộng {blockTitle}",
                TongMucDauTuTong = pList.Sum(p => (decimal)p.BudgetTotal),
                TongMucDauTuVCSH = pList.Sum(p => (decimal)p.BudgetVcsh),
                TongMucDauTuVay = pList.Sum(p => (decimal)p.BudgetVay),
                TongMucDauTuKhac = pList.Sum(p => (decimal)p.BudgetKhac),
                KhoiLuongKyTruoc = pList.Sum(p => (decimal)p.KhoiLuongKyTruoc),
                KhoiLuongTrongKy = pList.Sum(p => (decimal)p.KhoiLuongTrongKy),
                KhoiLuongLuyKe = pList.Sum(p => (decimal)p.KhoiLuongLuyKe),
                GiaiNganKyTruoc = pList.Sum(p => (decimal)p.GiaiNganKyTruoc),
                GiaiNganTrongKy = pList.Sum(p => (decimal)p.GiaiNganTrongKy),
                GiaiNganLuyKe = pList.Sum(p => (decimal)p.GiaiNganLuyKe),
                TaiSanBanGiao = pList.Sum(p => (decimal)p.TaiSanBanGiao)
            });
        }

        // Khối PHẦN A: Dự án có QĐ thành lập trước kỳ báo cáo
        var truocKyProjects = mappedProjects.Where(p => p.IsTruocKy).ToList();
        BuildBlock("A", "PHẦN A: CÁC DỰ ÁN CÓ QUYẾT ĐỊNH THÀNH LẬP TRƯỚC KỲ BÁO CÁO", truocKyProjects);

        // Khối PHẦN B: Dự án có QĐ thành lập trong kỳ báo cáo
        var trongKyProjects = mappedProjects.Where(p => p.IsTrongKy).ToList();
        BuildBlock("B", "PHẦN B: CÁC DỰ ÁN CÓ QUYẾT ĐỊNH THÀNH LẬP TRONG KỲ BÁO CÁO", trongKyProjects);

        // Dòng TỔNG CỘNG TOÀN BỘ (PHẦN A + PHẦN B)
        resultRows.Add(new ReportRowDto
        {
            Stt = string.Empty,
            RowType = "GrandTotal",
            ProjectName = "TỔNG CỘNG TOÀN BỘ (PHẦN A + PHẦN B)",
            TongMucDauTuTong = mappedProjects.Sum(p => (decimal)p.BudgetTotal),
            TongMucDauTuVCSH = mappedProjects.Sum(p => (decimal)p.BudgetVcsh),
            TongMucDauTuVay = mappedProjects.Sum(p => (decimal)p.BudgetVay),
            TongMucDauTuKhac = mappedProjects.Sum(p => (decimal)p.BudgetKhac),
            KhoiLuongKyTruoc = mappedProjects.Sum(p => (decimal)p.KhoiLuongKyTruoc),
            KhoiLuongTrongKy = mappedProjects.Sum(p => (decimal)p.KhoiLuongTrongKy),
            KhoiLuongLuyKe = mappedProjects.Sum(p => (decimal)p.KhoiLuongLuyKe),
            GiaiNganKyTruoc = mappedProjects.Sum(p => (decimal)p.GiaiNganKyTruoc),
            GiaiNganTrongKy = mappedProjects.Sum(p => (decimal)p.GiaiNganTrongKy),
            GiaiNganLuyKe = mappedProjects.Sum(p => (decimal)p.GiaiNganLuyKe),
            TaiSanBanGiao = mappedProjects.Sum(p => (decimal)p.TaiSanBanGiao)
        });

        return new ReportResponseDto
        {
            Title = "BÁO CÁO TỔNG HỢP TÌNH HÌNH THỰC HIỆN DỰ ÁN ĐẦU TƯ (BIỂU SỐ 02.A)",
            Unit = unitName,
            Year = year,
            Period = period,
            PeriodName = periodName,
            FromDate = startOfPeriod,
            ToDate = endOfPeriod,
            Rows = resultRows
        };
    }

    public async Task<byte[]> ExportBieuMau02AReportExcelAsync(int year, int period, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var report = await GetBieuMau02AReportAsync(year, period, fromDate, toDate);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Biểu 02.A - Báo cáo Đầu tư");

        worksheet.ShowGridLines = true;
        worksheet.Style.Font.FontName = "Times New Roman";
        worksheet.Style.Font.FontSize = 11;

        // 1. Header Metadata
        worksheet.Cell("A1").Value = "NGÂN HÀNG HỢP TÁC XÃ VIỆT NĂM";
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A2").Value = "BAN QUẢN LÝ DỰ ÁN";

        worksheet.Cell("P1").Value = "Biểu số: 02.A";
        worksheet.Cell("P1").Style.Font.Bold = true;
        worksheet.Cell("P2").Value = "Ban hành theo TT số 200/2015/TT-BTC";
        worksheet.Cell("P2").Style.Font.Italic = true;

        // Title
        worksheet.Cell("A4").Value = "BÁO CÁO TỔNG HỢP TÌNH HÌNH THỰC HIỆN DỰ ÁN ĐẦU TƯ";
        worksheet.Range("A4:S4").Merge();
        worksheet.Cell("A4").Style.Font.Bold = true;
        worksheet.Cell("A4").Style.Font.FontSize = 14;
        worksheet.Cell("A4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        string periodText = report.ToDate.HasValue && report.FromDate.HasValue
            ? $"Từ ngày {report.FromDate.Value:dd/MM/yyyy} đến ngày {report.ToDate.Value:dd/MM/yyyy}"
            : $"Năm {year}";
        worksheet.Cell("A5").Value = periodText;
        worksheet.Range("A5:S5").Merge();
        worksheet.Cell("A5").Style.Font.Italic = true;
        worksheet.Cell("A5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Cell("S6").Value = "Đơn vị tính: Tỷ đồng";
        worksheet.Cell("S6").Style.Font.Italic = true;
        worksheet.Cell("S6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        string dateStr = report.ToDate.HasValue ? report.ToDate.Value.ToString("dd/MM/yyyy") : $"31/12/{year}";

        // 2. Table Headers (Row 8 to 10)
        worksheet.Range("A8:A9").Merge().Value = "TT";
        worksheet.Range("B8:B9").Merge().Value = "Tên dự án";
        worksheet.Range("C8:C9").Merge().Value = "Quyết định phê duyệt";
        worksheet.Range("D8:H8").Merge().Value = "Tổng mức vốn đầu tư";
        worksheet.Cell("D9").Value = "Tổng";
        worksheet.Cell("E9").Value = "Vốn CSH";
        worksheet.Cell("F9").Value = "% Vốn CSH";
        worksheet.Cell("G9").Value = "Vốn huy động";
        worksheet.Cell("H9").Value = "% Vốn HĐ";

        worksheet.Range("I8:I9").Merge().Value = "Thời gian đầu tư theo KH";

        worksheet.Range("J8:L8").Merge().Value = "Nguồn vốn huy động";
        worksheet.Cell("J9").Value = "Tổng số";
        worksheet.Cell("K9").Value = "Thời hạn vay";
        worksheet.Cell("L9").Value = "Lãi suất (%)";

        worksheet.Range("M8:O8").Merge().Value = $"Giá trị khối lượng thực hiện đến {dateStr}";
        worksheet.Cell("M9").Value = "Kỳ trước chuyển sang";
        worksheet.Cell("N9").Value = "Thực hiện trong kỳ";
        worksheet.Cell("O9").Value = $"Lũy kế đến {dateStr}";

        worksheet.Range("P8:R8").Merge().Value = $"Giải ngân đến {dateStr}";
        worksheet.Cell("P9").Value = "Kỳ trước chuyển sang";
        worksheet.Cell("Q9").Value = "Thực hiện trong kỳ";
        worksheet.Cell("R9").Value = $"Lũy kế đến {dateStr}";

        worksheet.Range("S8:S9").Merge().Value = "Tài sản hoàn thành đưa vào SD";

        var headerRange = worksheet.Range("A8:S9");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Alignment.WrapText = true;
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        // Numbering row 10
        for (int i = 1; i <= 19; i++)
        {
            worksheet.Cell(10, i).Value = $"({i})";
        }
        var numRange = worksheet.Range("A10:S10");
        numRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        numRange.Style.Font.Italic = true;
        numRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        numRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        numRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        // 3. Render Data Rows
        int currentRow = 11;
        foreach (var r in report.Rows)
        {
            worksheet.Cell(currentRow, 1).Value = r.Stt;
            worksheet.Cell(currentRow, 2).Value = r.ProjectName;
            worksheet.Cell(currentRow, 3).Value = r.ApprovalDecision;

            if (r.RowType == "BlockHeader")
            {
                worksheet.Range(currentRow, 1, currentRow, 19).Style.Font.Bold = true;
                worksheet.Range(currentRow, 1, currentRow, 19).Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE");
                worksheet.Cell(currentRow, 2).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
            }
            else if (r.RowType == "GroupHeader")
            {
                worksheet.Range(currentRow, 1, currentRow, 19).Style.Font.Bold = true;
                worksheet.Range(currentRow, 1, currentRow, 19).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
            }
            else if (r.RowType == "SubGroupHeader")
            {
                worksheet.Range(currentRow, 1, currentRow, 19).Style.Font.Bold = true;
                worksheet.Range(currentRow, 1, currentRow, 19).Style.Font.Italic = true;
            }
            else if (r.RowType == "BlockFooter" || r.RowType == "GrandTotal")
            {
                worksheet.Range(currentRow, 1, currentRow, 19).Style.Font.Bold = true;
                worksheet.Range(currentRow, 1, currentRow, 19).Style.Fill.BackgroundColor = r.RowType == "GrandTotal" 
                    ? XLColor.FromHtml("#FEF08A") 
                    : XLColor.FromHtml("#E0E7FF");

                worksheet.Cell(currentRow, 4).Value = r.TongMucDauTuTong;
                worksheet.Cell(currentRow, 5).Value = r.TongMucDauTuVCSH;
                worksheet.Cell(currentRow, 6).Value = r.PhanTramVCSH;
                worksheet.Cell(currentRow, 7).Value = r.TongMucDauTuVay;
                worksheet.Cell(currentRow, 8).Value = r.PhanTramVay;
                worksheet.Cell(currentRow, 10).Value = r.TongMucDauTuVay;
                worksheet.Cell(currentRow, 13).Value = r.KhoiLuongKyTruoc;
                worksheet.Cell(currentRow, 14).Value = r.KhoiLuongTrongKy;
                worksheet.Cell(currentRow, 15).Value = r.KhoiLuongLuyKe;
                worksheet.Cell(currentRow, 16).Value = r.GiaiNganKyTruoc;
                worksheet.Cell(currentRow, 17).Value = r.GiaiNganTrongKy;
                worksheet.Cell(currentRow, 18).Value = r.GiaiNganLuyKe;
                worksheet.Cell(currentRow, 19).Value = r.TaiSanBanGiao;
            }
            else // ProjectRow
            {
                worksheet.Cell(currentRow, 4).Value = r.TongMucDauTuTong;
                worksheet.Cell(currentRow, 5).Value = r.TongMucDauTuVCSH;
                worksheet.Cell(currentRow, 6).Value = r.PhanTramVCSH;
                worksheet.Cell(currentRow, 7).Value = r.TongMucDauTuVay;
                worksheet.Cell(currentRow, 8).Value = r.PhanTramVay;
                worksheet.Cell(currentRow, 9).Value = r.ThoiGianThucHien ?? string.Empty;
                worksheet.Cell(currentRow, 10).Value = r.TongMucDauTuVay;
                worksheet.Cell(currentRow, 11).Value = r.ThoiHanVay ?? string.Empty;
                if (r.LaiSuat.HasValue) worksheet.Cell(currentRow, 12).Value = r.LaiSuat.Value;

                worksheet.Cell(currentRow, 13).Value = r.KhoiLuongKyTruoc;
                worksheet.Cell(currentRow, 14).Value = r.KhoiLuongTrongKy;
                worksheet.Cell(currentRow, 15).Value = r.KhoiLuongLuyKe;
                worksheet.Cell(currentRow, 16).Value = r.GiaiNganKyTruoc;
                worksheet.Cell(currentRow, 17).Value = r.GiaiNganTrongKy;
                worksheet.Cell(currentRow, 18).Value = r.GiaiNganLuyKe;
                worksheet.Cell(currentRow, 19).Value = r.TaiSanBanGiao;
            }

            // Numeric format
            foreach (var colIdx in new[] { 4, 5, 7, 10, 13, 14, 15, 16, 17, 18, 19 })
            {
                var cell = worksheet.Cell(currentRow, colIdx);
                if (cell.Value.IsNumber)
                {
                    cell.Style.NumberFormat.Format = "#,##0.00;(#,##0.00);\"-\"";
                }
            }
            foreach (var colIdx in new[] { 6, 8, 12 })
            {
                var cell = worksheet.Cell(currentRow, colIdx);
                if (cell.Value.IsNumber)
                {
                    cell.Style.NumberFormat.Format = "0.0\"%\";(0.0\"%\");\"-\"";
                }
            }

            worksheet.Range(currentRow, 1, currentRow, 19).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(currentRow, 1, currentRow, 19).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            currentRow++;
        }

        // Auto adjust columns
        worksheet.Column(1).Width = 8;
        worksheet.Column(2).Width = 40;
        worksheet.Column(3).Width = 30;
        for (int c = 4; c <= 19; c++)
        {
            worksheet.Column(c).Width = 16;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportBieuMau02AReportCsvAsync(int year, int period, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var report = await GetBieuMau02AReportAsync(year, period, fromDate, toDate);
        var sb = new StringBuilder();

        sb.AppendLine("TT,Tên dự án,Quyết định phê duyệt,Tổng mức đầu tư Tổng,Vốn CSH,% Vốn CSH,Vốn vay,% Vốn vay,Thời gian đầu tư theo KH,Tổng số vốn vay,Thời hạn vay,Lãi suất,Khối lượng Kỳ trước,Khối lượng Trong kỳ,Khối lượng Lũy kế,Giải ngân Kỳ trước,Giải ngân Trong kỳ,Giải ngân Lũy kế,Tài sản bàn giao");

        foreach (var r in report.Rows)
        {
            sb.AppendLine($"\"{r.Stt}\",\"{r.ProjectName}\",\"{r.ApprovalDecision}\",{r.TongMucDauTuTong},{r.TongMucDauTuVCSH},{r.PhanTramVCSH},{r.TongMucDauTuVay},{r.PhanTramVay},\"{r.ThoiGianThucHien}\",{r.TongMucDauTuVay},\"{r.ThoiHanVay}\",{r.LaiSuat ?? 0},{r.KhoiLuongKyTruoc},{r.KhoiLuongTrongKy},{r.KhoiLuongLuyKe},{r.GiaiNganKyTruoc},{r.GiaiNganTrongKy},{r.GiaiNganLuyKe},{r.TaiSanBanGiao}");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<byte[]> ExportBieuMau02AReportHtmlAsync(int year, int period, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var report = await GetBieuMau02AReportAsync(year, period, fromDate, toDate);
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Biểu số 02.A</title>");
        sb.AppendLine("<style>body{font-family:'Times New Roman',serif;padding:20px;} table{border-collapse:collapse;width:100%;font-size:13px;} th,td{border:1px solid #999;padding:6px 8px;text-align:right;} th{background:#1E3A8A;color:white;text-align:center;} .text-left{text-align:left;} .text-center{text-align:center;} .block-header{background:#DBEAFE;font-weight:bold;text-align:left;color:#1E3A8A;} .group-header{background:#F1F5F9;font-weight:bold;text-align:left;} .footer-row{background:#E0E7FF;font-weight:bold;} .grand-total{background:#FEF08A;font-weight:bold;}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h2 style='text-align:center;'>BÁO CÁO TỔNG HỢP TÌNH HÌNH THỰC HIỆN DỰ ÁN ĐẦU TƯ (BIỂU SỐ 02.A)</h2>");
        sb.AppendLine($"<p style='text-align:center;font-style:italic;'>Năm {year} - Đơn vị tính: {report.Unit}</p>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th rowspan='2'>TT</th><th rowspan='2'>Tên dự án</th><th rowspan='2'>Quyết định phê duyệt</th><th colspan='5'>Tổng mức vốn đầu tư</th><th rowspan='2'>Thời gian ĐT</th><th colspan='3'>Vốn vay</th><th colspan='3'>Khối lượng thực hiện</th><th colspan='3'>Giải ngân</th><th rowspan='2'>Tài sản bàn giao</th></tr>");
        sb.AppendLine("<tr><th>Tổng</th><th>CSH</th><th>% CSH</th><th>Vay</th><th>% Vay</th><th>Tổng vay</th><th>Thời hạn</th><th>Lãi suất</th><th>Trước kỳ</th><th>Trong kỳ</th><th>Lũy kế</th><th>Trước kỳ</th><th>Trong kỳ</th><th>Lũy kế</th></tr></thead><tbody>");

        foreach (var r in report.Rows)
        {
            string cls = r.RowType == "BlockHeader" ? "block-header" : (r.RowType == "GroupHeader" ? "group-header" : (r.RowType == "BlockFooter" ? "footer-row" : (r.RowType == "GrandTotal" ? "grand-total" : "")));
            sb.AppendLine($"<tr class='{cls}'><td class='text-center'>{r.Stt}</td><td class='text-left'>{r.ProjectName}</td><td class='text-left'>{r.ApprovalDecision}</td><td>{r.TongMucDauTuTong:N2}</td><td>{r.TongMucDauTuVCSH:N2}</td><td>{r.PhanTramVCSH:N1}%</td><td>{r.TongMucDauTuVay:N2}</td><td>{r.PhanTramVay:N1}%</td><td>{r.ThoiGianThucHien}</td><td>{r.TongMucDauTuVay:N2}</td><td>{r.ThoiHanVay}</td><td>{(r.LaiSuat.HasValue ? $"{r.LaiSuat:N1}%" : "-")}</td><td>{r.KhoiLuongKyTruoc:N2}</td><td>{r.KhoiLuongTrongKy:N2}</td><td>{r.KhoiLuongLuyKe:N2}</td><td>{r.GiaiNganKyTruoc:N2}</td><td>{r.GiaiNganTrongKy:N2}</td><td>{r.GiaiNganLuyKe:N2}</td><td>{r.TaiSanBanGiao:N2}</td></tr>");
        }

        sb.AppendLine("</tbody></table></body></html>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    #endregion

}
