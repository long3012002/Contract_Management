using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ReportResponseDto> GetInvestmentReportAsync(int year, int period)
    {
        // 1. Tính toán thời gian báo cáo
        DateTime startOfPeriod = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime endOfPeriod;

        string periodDisplayName;
        string periodName;
        if (period == 1) // 6T
        {
            endOfPeriod = new DateTime(year, 6, 30, 23, 59, 59, DateTimeKind.Utc);
            periodDisplayName = $"6T đầu năm {year}";
            periodName = "6T";
        }
        else // 1N
        {
            endOfPeriod = new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            periodDisplayName = $"năm {year}";
            periodName = "1N";
        }

        // 2. Chỉ tải các thuộc tính cần thiết của dự án và tính tổng tiền điều chỉnh trực tiếp ở DB
        var projectsData = await _context.DuAns
            .AsNoTracking()
            .Where(da => da.IsActive)
            .Select(da => new
            {
                da.Id,
                da.Name,
                da.Code,
                da.LoaiDuAn,
                da.NguonDuAnIds,
                da.DuToanPheDuyet,
                da.SoQuyetDinh,
                da.NgayBatDau,
                da.NgayKetThuc,
                da.UpdatedAt,
                da.TrangThai,
                da.DaKetThuc,
                NhomDuAnCode = da.NhomDuAn != null ? da.NhomDuAn.Code : null,
                da.NhomDuAnId,
                PhanLoaiDuAnCode = da.PhanLoaiDuAn != null ? da.PhanLoaiDuAn.Code : null,
                PhanLoaiDuAnName = da.PhanLoaiDuAn != null ? da.PhanLoaiDuAn.Name : null,
                AdjustmentsSum = da.DieuChinhs
                    .Where(dc => dc.NgayDieuChinh <= endOfPeriod)
                    .Sum(dc => (decimal?)dc.GiaTriDieuChinh) ?? 0
            })
            .ToListAsync();

        var projectMap = projectsData.ToDictionary(p => p.Id, p => p);

        // 3. Tính toán các giá trị lũy kế thanh toán trực tiếp ở DB bằng GroupBy, loại bỏ việc nạp tất cả hợp đồng lên bộ nhớ RAM
        var performedValues = await _context.DotThanhToans
            .AsNoTracking()
            .Where(dt => dt.HopDong.IsActive && dt.HopDong.DuAnId.HasValue)
            .GroupBy(dt => dt.HopDong.DuAnId!.Value)
            .Select(g => new
            {
                DuAnId = g.Key,
                KyTruoc = g.Where(dt => dt.CreatedAt < startOfPeriod).Sum(dt => dt.GiaTriThanhToan),
                TrongKy = g.Where(dt => dt.CreatedAt >= startOfPeriod && dt.CreatedAt <= endOfPeriod).Sum(dt => dt.GiaTriThanhToan)
            })
            .ToDictionaryAsync(x => x.DuAnId, x => x);

        // Các danh sách phụ hỗ trợ phân nhóm dự án
        var b_I = new List<ReportRowDto>();   // Nhóm B - Xây dựng
        var b_II = new List<ReportRowDto>();  // Nhóm B - CNTT
        var b_III = new List<ReportRowDto>(); // Nhóm B - Khác
        
        var c_I = new List<ReportRowDto>();   // Nhóm C - Xây dựng
        var c_II = new List<ReportRowDto>();  // Nhóm C - CNTT
        var c_III = new List<ReportRowDto>(); // Nhóm C - Khác

        int b_I_index = 1, b_II_index = 1, b_III_index = 1;
        int c_I_index = 1, c_II_index = 1, c_III_index = 1;

        foreach (var project in projectsData)
        {
            // Tính toán tổng ngân sách đã phê duyệt
            decimal totalBudgetVnd = 0;
            if (project.LoaiDuAn == 2 && !string.IsNullOrWhiteSpace(project.NguonDuAnIds))
            {
                var sourceIds = project.NguonDuAnIds.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                    .Where(g => g != Guid.Empty)
                    .ToList();

                if (sourceIds.Any())
                {
                    foreach (var sourceId in sourceIds)
                    {
                        if (projectMap.TryGetValue(sourceId, out var sp))
                        {
                            totalBudgetVnd += (sp.DuToanPheDuyet + sp.AdjustmentsSum);
                        }
                    }
                }
                else
                {
                    totalBudgetVnd = project.DuToanPheDuyet + project.AdjustmentsSum;
                }
            }
            else
            {
                totalBudgetVnd = project.DuToanPheDuyet + project.AdjustmentsSum;
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
                if (project.NgayKetThuc.HasValue && project.NgayKetThuc.Value <= endOfPeriod)
                {
                    isCompletedBeforeEnd = true;
                }
                else if (!project.NgayKetThuc.HasValue && project.UpdatedAt.HasValue && project.UpdatedAt.Value <= endOfPeriod)
                {
                    isCompletedBeforeEnd = true;
                }
                else if (!project.NgayKetThuc.HasValue && !project.UpdatedAt.HasValue)
                {
                    isCompletedBeforeEnd = true; // Giá trị mặc định nếu trạng thái đã hoàn thành
                }

                if (isCompletedBeforeEnd)
                {
                    taiSanBanGiaoVnd = performedLuyKeVnd;
                }
            }

            // Quy đổi sang Triệu đồng
            decimal conversionFactor = 1_000_000m;
            decimal budgetTotal = totalBudgetVnd / conversionFactor;
            decimal budgetVcsh = budgetTotal;
            decimal budgetVay = 0;
            decimal budgetKhac = 0;

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

            // Phân loại loại dự án
            string projType = "Khac";
            if (project.PhanLoaiDuAnCode != null)
            {
                var codeUpper = project.PhanLoaiDuAnCode.ToUpper();
                var nameLower = (project.PhanLoaiDuAnName ?? string.Empty).ToLower();
                if (codeUpper.Contains("XAY_DUNG") || codeUpper.Contains("CONSTRUCTION") || nameLower.Contains("xây dựng") || nameLower.Contains("xay dung"))
                {
                    projType = "XayDung";
                }
                else if (codeUpper.Contains("CNTT") || codeUpper.Contains("IT") || codeUpper.Contains("SOFTWARE") || nameLower.Contains("công nghệ") || nameLower.Contains("cong nghe"))
                {
                    projType = "CNTT";
                }
            }
            else
            {
                var nameLower = project.Name.ToLower();
                if (nameLower.Contains("xây dựng") || nameLower.Contains("kiến trúc") || nameLower.Contains("nhà") || nameLower.Contains("đất"))
                {
                    projType = "XayDung";
                }
                else if (nameLower.Contains("công nghệ") || nameLower.Contains("cntt") || nameLower.Contains("phần mềm") || 
                         nameLower.Contains("hệ thống") || nameLower.Contains("software") || nameLower.Contains("hardware") || 
                         nameLower.Contains("máy chủ") || nameLower.Contains("thiết bị") || nameLower.Contains("bảo mật") || 
                         nameLower.Contains("dlp") || nameLower.Contains("hsm") || nameLower.Contains("ftp") || 
                         nameLower.Contains("database") || nameLower.Contains("mạng") || nameLower.Contains("it"))
                {
                    projType = "CNTT";
                }
            }

            // Phân loại nhóm dự án (Nhóm B >= 45 tỷ đồng)
            bool isGroupB = (project.NhomDuAnCode != null && project.NhomDuAnCode.Equals("NHOM_B", StringComparison.OrdinalIgnoreCase)) || 
                            (project.NhomDuAnId == null && totalBudgetVnd >= 45_000_000_000m);

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
                TaiSanBanGiao = tsBanGiao
            };

            if (isGroupB)
            {
                if (projType == "XayDung")
                {
                    row.Stt = b_I_index++.ToString();
                    b_I.Add(row);
                }
                else if (projType == "CNTT")
                {
                    row.Stt = b_II_index++.ToString();
                    b_II.Add(row);
                }
                else
                {
                    row.Stt = b_III_index++.ToString();
                    b_III.Add(row);
                }
            }
            else
            {
                if (projType == "XayDung")
                {
                    row.Stt = c_I_index++.ToString();
                    c_I.Add(row);
                }
                else if (projType == "CNTT")
                {
                    row.Stt = c_II_index++.ToString();
                    c_II.Add(row);
                }
                else
                {
                    row.Stt = c_III_index++.ToString();
                    c_III.Add(row);
                }
            }
        }

        // 4. Tổ hợp hiển thị báo cáo dạng cây
        var rows = new List<ReportRowDto>();

        // --- GROUP B ---
        var groupBHeader = new ReportRowDto
        {
            Stt = "B",
            RowType = "GroupHeader",
            ProjectName = "Các dự án nhóm B"
        };
        var b_I_Header = new ReportRowDto
        {
            Stt = "I",
            RowType = "SubGroupHeader",
            ProjectName = "Dự án đầu tư xây dựng"
        };
        var b_II_Header = new ReportRowDto
        {
            Stt = "II",
            RowType = "SubGroupHeader",
            ProjectName = "Dự án công nghệ thông tin"
        };
        var b_III_Header = new ReportRowDto
        {
            Stt = "III",
            RowType = "SubGroupHeader",
            ProjectName = "Dự án khác"
        };
        var groupBFooter = new ReportRowDto
        {
            RowType = "GroupFooter",
            ProjectName = "Tổng (B)"
        };

        // Populate Group B values
        PopulateSubGroupSummary(b_I_Header, b_I);
        PopulateSubGroupSummary(b_II_Header, b_II);
        PopulateSubGroupSummary(b_III_Header, b_III);
        PopulateGroupSummary(groupBHeader, new List<ReportRowDto> { b_I_Header, b_II_Header, b_III_Header });
        PopulateGroupSummary(groupBFooter, new List<ReportRowDto> { b_I_Header, b_II_Header, b_III_Header });

        rows.Add(groupBHeader);
        rows.Add(b_I_Header);
        if (b_I.Any()) rows.AddRange(b_I);
        else rows.Add(new ReportRowDto { RowType = "EmptyPlaceholder", ProjectName = "(Không có)" });

        rows.Add(b_II_Header);
        if (b_II.Any()) rows.AddRange(b_II);
        else rows.Add(new ReportRowDto { RowType = "EmptyPlaceholder", ProjectName = "(Không có)" });

        rows.Add(b_III_Header);
        if (b_III.Any()) rows.AddRange(b_III);
        else rows.Add(new ReportRowDto { RowType = "EmptyPlaceholder", ProjectName = "(Không có)" });

        rows.Add(groupBFooter);

        // --- GROUP C ---
        var groupCHeader = new ReportRowDto
        {
            Stt = "C",
            RowType = "GroupHeader",
            ProjectName = "Các dự án khác"
        };
        var c_I_Header = new ReportRowDto
        {
            Stt = "I",
            RowType = "SubGroupHeader",
            ProjectName = "Dự án đầu tư xây dựng"
        };
        var c_II_Header = new ReportRowDto
        {
            Stt = "II",
            RowType = "SubGroupHeader",
            ProjectName = "Dự án công nghệ thông tin"
        };
        var c_III_Header = new ReportRowDto
        {
            Stt = "III",
            RowType = "SubGroupHeader",
            ProjectName = "Dự án khác"
        };
        var groupCFooter = new ReportRowDto
        {
            RowType = "GroupFooter",
            ProjectName = "Tổng (C)"
        };

        // Populate Group C values
        PopulateSubGroupSummary(c_I_Header, c_I);
        PopulateSubGroupSummary(c_II_Header, c_II);
        PopulateSubGroupSummary(c_III_Header, c_III);
        PopulateGroupSummary(groupCHeader, new List<ReportRowDto> { c_I_Header, c_II_Header, c_III_Header });
        PopulateGroupSummary(groupCFooter, new List<ReportRowDto> { c_I_Header, c_II_Header, c_III_Header });

        rows.Add(groupCHeader);
        rows.Add(c_I_Header);
        if (c_I.Any()) rows.AddRange(c_I);
        else rows.Add(new ReportRowDto { RowType = "EmptyPlaceholder", ProjectName = "(Không có)" });

        rows.Add(c_II_Header);
        if (c_II.Any()) rows.AddRange(c_II);
        else rows.Add(new ReportRowDto { RowType = "EmptyPlaceholder", ProjectName = "(Không có)" });

        rows.Add(c_III_Header);
        if (c_III.Any()) rows.AddRange(c_III);
        else rows.Add(new ReportRowDto { RowType = "EmptyPlaceholder", ProjectName = "(Không có)" });

        rows.Add(groupCFooter);

        // --- GRAND TOTAL ---
        var grandTotal = new ReportRowDto
        {
            RowType = "GrandTotal",
            ProjectName = "TỔNG CỘNG"
        };
        PopulateGroupSummary(grandTotal, new List<ReportRowDto> { groupBFooter, groupCFooter });
        rows.Add(grandTotal);

        return new ReportResponseDto
        {
            Title = $"TÌNH HÌNH ĐẦU TƯ VÀ HUY ĐỘNG VỐN ĐỂ ĐẦU TƯ VÀO CÁC DỰ ÁN HÌNH THÀNH TSCĐ VÀ XDCB ({periodDisplayName})",
            Unit = "Triệu đồng",
            Year = year,
            Period = period,
            PeriodName = periodName,
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

    public async Task<byte[]> ExportInvestmentReportExcelAsync(int year, int period)
    {
        var report = await GetInvestmentReportAsync(year, period);
        
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

            string periodText = period == 1 ? $"Trong kỳ báo cáo 6T đầu năm {year}" : $"Trong kỳ báo cáo năm {year}";
            worksheet.Cell("A7").Value = $"( {periodText} )";
            worksheet.Cell("A7").Style.Font.Bold = true;
            worksheet.Cell("A7").Style.Font.FontSize = 12;
            worksheet.Cell("A7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A7:L7").Merge();

            // Unit
            worksheet.Cell("L8").Value = "Đơn vị tính: Triệu đồng";
            worksheet.Cell("L8").Style.Font.Italic = true;
            worksheet.Cell("L8").Style.Font.FontSize = 11;
            worksheet.Cell("L8").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Dates for headers
            string dateStr = period == 1 ? $"30/06/{year}" : $"31/12/{year}";

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

    public async Task<byte[]> ExportInvestmentReportCsvAsync(int year, int period)
    {
        var report = await GetInvestmentReportAsync(year, period);
        string dateStr = period == 1 ? $"30/06/{year}" : $"31/12/{year}";

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
                string periodText = period == 1 ? $"Trong kỳ báo cáo 6T đầu năm {year}" : $"Trong kỳ báo cáo năm {year}";
                await writer.WriteLineAsync($"\"( {periodText} )\"");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"\"Đơn vị tính: Triệu đồng\"");
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

    public async Task<byte[]> ExportInvestmentReportHtmlAsync(int year, int period)
    {
        var report = await GetInvestmentReportAsync(year, period);
        string dateStr = period == 1 ? $"30/06/{year}" : $"31/12/{year}";
        string periodText = period == 1 ? $"Trong kỳ báo cáo 6T đầu năm {year}" : $"Trong kỳ báo cáo năm {year}";

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
        htmlBuilder.AppendLine("<div class=\"unit-line\">Đơn vị tính: Triệu đồng</div>");

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

    public async Task<CongViecGoiThauReportDto> GetCongViecGoiThauReportAsync(Guid idGoiThau)
    {
        var goiThau = await _context.GoiThaus
            .Include(g => g.DuAn)
            .Include(g => g.CongViecGoiThaus)
            .FirstOrDefaultAsync(g => g.Id == idGoiThau);

        if (goiThau == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy gói thầu với ID '{idGoiThau}'.");
        }

        var congViecs = goiThau.CongViecGoiThaus
            .OrderBy(c => c.Stt)
            .ThenBy(c => c.CreatedAt)
            .Select(c => new CongViecGoiThauDto
            {
                Id = c.Id,
                GoiThauId = c.GoiThauId,
                Stt = c.Stt,
                TenTaiLieu = c.TenTaiLieu,
                NgayKy = c.NgayKy,
                LoaiVanBan = null,
                TinhTrang = c.TinhTrang,
                GhiChu = c.GhiChu,
                Code = c.Code,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToList();

        int completed = congViecs.Count(c => c.TinhTrang != null && c.TinhTrang.Equals("Đã xong", StringComparison.OrdinalIgnoreCase));
        int inProgress = congViecs.Count(c => c.TinhTrang != null && !c.TinhTrang.Equals("Đã xong", StringComparison.OrdinalIgnoreCase));

        return new CongViecGoiThauReportDto
        {
            GoiThauId = goiThau.Id,
            TenGoiThau = goiThau.Name,
            MaGoiThau = goiThau.Code,
            TenDuAn = goiThau.DuAn?.Name,
            GiaTriGoiThau = goiThau.GiaTriGoiThau,
            CongViecs = congViecs,
            TongSoCongViec = congViecs.Count,
            SoCongViecDaHoanThanh = completed,
            SoCongViecDangThucHien = inProgress
        };
    }

    public async Task<byte[]> ExportCongViecGoiThauReportExcelAsync(Guid idGoiThau)
    {
        var report = await GetCongViecGoiThauReportAsync(idGoiThau);

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Trình tự thực hiện");

            worksheet.Style.Font.FontName = "Times New Roman";
            worksheet.Style.Font.FontSize = 11;

            // Title
            worksheet.Cell("A1").Value = "TRÌNH TỰ THỰC HIỆN GÓI THẦU";
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 14;
            worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A1:F1").Merge();

            // Package Name
            worksheet.Cell("A2").Value = report.TenGoiThau;
            worksheet.Cell("A2").Style.Font.Bold = true;
            worksheet.Cell("A2").Style.Font.FontSize = 12;
            worksheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A2:F2").Merge();

            // Headers row 4
            worksheet.Cell("A4").Value = "STT";
            worksheet.Cell("B4").Value = "Tài liệu";
            worksheet.Cell("C4").Value = "Ngày ký";
            worksheet.Cell("D4").Value = "Loại văn bản";
            worksheet.Cell("E4").Value = "Tình trạng";
            worksheet.Cell("F4").Value = "Ghi chú";

            var headerRange = worksheet.Range("A4:F4");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            int currentRow = 5;
            foreach (var item in report.CongViecs)
            {
                worksheet.Cell(currentRow, 1).Value = item.Stt;
                worksheet.Cell(currentRow, 2).Value = item.TenTaiLieu;
                worksheet.Cell(currentRow, 3).Value = item.NgayKy.HasValue ? item.NgayKy.Value.ToString("dd/MM/yyyy") : "";
                worksheet.Cell(currentRow, 4).Value = item.LoaiVanBan ?? "";
                worksheet.Cell(currentRow, 5).Value = item.TinhTrang ?? "";
                worksheet.Cell(currentRow, 6).Value = item.GhiChu ?? "";

                var rowRange = worksheet.Range(currentRow, 1, currentRow, 6);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                currentRow++;
            }

            // Signature section
            currentRow += 2;
            worksheet.Cell(currentRow, 2).Value = "Bên giao";
            worksheet.Cell(currentRow, 2).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 5).Value = "Bên nhận";
            worksheet.Cell(currentRow, 5).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Column widths
            worksheet.Column(1).Width = 8;   // STT
            worksheet.Column(2).Width = 50;  // Tài liệu
            worksheet.Column(3).Width = 15;  // Ngày ký
            worksheet.Column(4).Width = 18;  // Loại văn bản
            worksheet.Column(5).Width = 18;  // Tình trạng
            worksheet.Column(6).Width = 30;  // Ghi chú

            using (var memoryStream = new MemoryStream())
            {
                workbook.SaveAs(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }

    public async Task<ContractPaymentReportResponseDto> GetContractPaymentReportAsync(int year, int? loaiHopDong, string? search)
    {
        var query = _context.HopDongs
            .Include(h => h.DotThanhToans)
            .Include(h => h.DuAn)
            .Include(h => h.GoiThau)
            .Include(h => h.NhaThau)
            .Where(h => h.IsActive);

        if (loaiHopDong.HasValue && loaiHopDong.Value > 0)
        {
            query = query.Where(h => h.LoaiHopDong == loaiHopDong.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string searchLower = search.Trim().ToLower();
            query = query.Where(h =>
                (h.Code != null && h.Code.ToLower().Contains(searchLower)) ||
                (h.Name != null && h.Name.ToLower().Contains(searchLower)) ||
                (h.DuAn != null && h.DuAn.Name.ToLower().Contains(searchLower)) ||
                (h.GoiThau != null && h.GoiThau.Name.ToLower().Contains(searchLower)) ||
                (h.NhaThau != null && h.NhaThau.Name.ToLower().Contains(searchLower))
            );
        }

        var contracts = await query
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        var rows = new List<ContractPaymentReportRowDto>();

        foreach (var contract in contracts)
        {
            var milestones = contract.DotThanhToans != null ? contract.DotThanhToans.ToList() : new List<DotThanhToan>();

            int tongSoKy = milestones.Count;
            int soKyDaThanhToan = milestones.Count(m => m.IsPaid);
            int soKyConPhaiThanhToan = tongSoKy - soKyDaThanhToan;

            decimal soTienDaThanhToan = milestones.Where(m => m.IsPaid).Sum(m => m.GiaTriThanhToan);
            decimal soTienConPhaiThanhToan = contract.GiaTriHopDong - soTienDaThanhToan;
            if (soTienConPhaiThanhToan < 0) soTienConPhaiThanhToan = 0;

            double tyLeDaThanhToanPercent = contract.GiaTriHopDong > 0 
                ? (double)Math.Round(soTienDaThanhToan / contract.GiaTriHopDong * 100, 2) 
                : 0;

            // Milestones scheduled or recorded in the target year
            var milestonesInYear = milestones.Where(m => 
                (m.NgayThanhToan.HasValue && m.NgayThanhToan.Value.Year == year) ||
                (!m.NgayThanhToan.HasValue && m.CreatedAt.Year == year)
            ).ToList();

            decimal phaiThanhToanTrongNam = milestonesInYear.Sum(m => m.GiaTriThanhToan);
            decimal daThanhToanTrongNam = milestonesInYear.Where(m => m.IsPaid).Sum(m => m.GiaTriThanhToan);
            decimal conPhaiThanhToanTrongNam = phaiThanhToanTrongNam - daThanhToanTrongNam;
            if (conPhaiThanhToanTrongNam < 0) conPhaiThanhToanTrongNam = 0;

            // Payment Status Label
            string trangThaiThanhToan;
            if (tongSoKy > 0 && soKyDaThanhToan == tongSoKy)
            {
                trangThaiThanhToan = "Đã hoàn thành";
            }
            else if (soKyDaThanhToan > 0)
            {
                trangThaiThanhToan = "Đang thanh toán";
            }
            else if (contract.ExpiredDate.HasValue && contract.ExpiredDate.Value < DateTime.UtcNow && soKyConPhaiThanhToan > 0)
            {
                trangThaiThanhToan = "Quá hạn";
            }
            else
            {
                trangThaiThanhToan = "Chưa thanh toán";
            }

            string loaiHopDongTen = GetLoaiHopDongName(contract.LoaiHopDong);

            var milestoneDtos = milestones.Select(m => new ContractPaymentReportMilestoneDto
            {
                Id = m.Id,
                TenDot = m.TenDot,
                TyLeThanhToan = m.TyLeThanhToan,
                GiaTriThanhToan = m.GiaTriThanhToan,
                NgayThanhToan = m.NgayThanhToan,
                DieuKienThanhToan = m.DieuKienThanhToan,
                IsPaid = m.IsPaid
            }).ToList();

            rows.Add(new ContractPaymentReportRowDto
            {
                HopDongId = contract.Id,
                MaHopDong = contract.Code,
                TenHopDong = contract.Name,
                LoaiHopDong = contract.LoaiHopDong,
                LoaiHopDongTen = loaiHopDongTen,
                TenDuAn = contract.DuAn?.Name,
                TenGoiThau = contract.GoiThau?.Name,
                TenNhaThau = contract.NhaThau?.Name,
                GiaTriHopDong = contract.GiaTriHopDong,
                NgayHieuLuc = contract.NgayHieuLuc,
                ExpiredDate = contract.ExpiredDate,
                TongSoKy = tongSoKy,
                SoKyDaThanhToan = soKyDaThanhToan,
                SoKyConPhaiThanhToan = soKyConPhaiThanhToan,
                SoTienDaThanhToan = soTienDaThanhToan,
                SoTienConPhaiThanhToan = soTienConPhaiThanhToan,
                TyLeDaThanhToanPercent = tyLeDaThanhToanPercent,
                PhaiThanhToanTrongNam = phaiThanhToanTrongNam,
                DaThanhToanTrongNam = daThanhToanTrongNam,
                ConPhaiThanhToanTrongNam = conPhaiThanhToanTrongNam,
                TrangThaiThanhToan = trangThaiThanhToan,
                DanhSachDotThanhToan = milestoneDtos
            });
        }

        var summary = new ContractPaymentReportSummaryDto
        {
            TongSoHopDong = rows.Count,
            SoHopDongBaoTri = rows.Count(r => r.LoaiHopDong == 2),
            TongGiaTriHopDong = rows.Sum(r => r.GiaTriHopDong),
            TongSoKyThanhToan = rows.Sum(r => r.TongSoKy),
            TongSoKyDaThanhToan = rows.Sum(r => r.SoKyDaThanhToan),
            TongSoKyConPhaiThanhToan = rows.Sum(r => r.SoKyConPhaiThanhToan),
            TongSoTienDaThanhToan = rows.Sum(r => r.SoTienDaThanhToan),
            TongSoTienConPhaiThanhToan = rows.Sum(r => r.SoTienConPhaiThanhToan),
            TongPhaiThanhToanTrongNam = rows.Sum(r => r.PhaiThanhToanTrongNam),
            TongDaThanhToanTrongNam = rows.Sum(r => r.DaThanhToanTrongNam),
            TongConPhaiThanhToanTrongNam = rows.Sum(r => r.ConPhaiThanhToanTrongNam)
        };

        string loaiFilterName = loaiHopDong.HasValue ? GetLoaiHopDongName(loaiHopDong.Value) : "Tất cả loại hợp đồng";

        return new ContractPaymentReportResponseDto
        {
            Title = $"BÁO CÁO THEO DÕI THANH TOÁN HỢP ĐỒNG NĂM {year}",
            Year = year,
            LoaiHopDong = loaiHopDong,
            LoaiHopDongFilterTen = loaiFilterName,
            Summary = summary,
            Rows = rows
        };
    }

    private string GetLoaiHopDongName(int loaiHopDong)
    {
        return loaiHopDong switch
        {
            1 => "HĐ Mua sắm / Triển khai",
            2 => "HĐ Bảo trì / Bảo dưỡng",
            3 => "HĐ Tư vấn / Dịch vụ",
            _ => "Hợp đồng khác"
        };
    }

    public async Task<byte[]> ExportContractPaymentReportExcelAsync(int year, int? loaiHopDong, string? search)
    {
        var report = await GetContractPaymentReportAsync(year, loaiHopDong, search);

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Theo doi thanh toan HD");

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
            worksheet.Cell("Q1").Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Cell("Q1").Style.Font.Italic = true;
            worksheet.Cell("Q1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            worksheet.Cell("Q1").Style.Font.FontSize = 10;

            // Title
            worksheet.Cell("A4").Value = $"BÁO CÁO THEO DÕI THANH TOÁN HỢP ĐỒNG NĂM {year}";
            worksheet.Cell("A4").Style.Font.Bold = true;
            worksheet.Cell("A4").Style.Font.FontSize = 14;
            worksheet.Cell("A4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A4:Q4").Merge();

            worksheet.Cell("A5").Value = $"Phân loại: {report.LoaiHopDongFilterTen}";
            worksheet.Cell("A5").Style.Font.Italic = true;
            worksheet.Cell("A5").Style.Font.FontSize = 11;
            worksheet.Cell("A5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A5:Q5").Merge();

            // Unit line
            worksheet.Cell("Q7").Value = "Đơn vị tính: Đồng (VND)";
            worksheet.Cell("Q7").Style.Font.Italic = true;
            worksheet.Cell("Q7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Summary Section (Rows 9-11)
            worksheet.Cell("A9").Value = "TỔNG QUAN THEO DÕI THANH TOÁN";
            worksheet.Cell("A9").Style.Font.Bold = true;
            worksheet.Range("A9:D9").Merge();

            worksheet.Cell("A10").Value = $"Tổng số hợp đồng: {report.Summary.TongSoHopDong} (Bảo trì: {report.Summary.SoHopDongBaoTri})";
            worksheet.Cell("D10").Value = $"Tổng kỳ thanh toán: {report.Summary.TongSoKyThanhToan} (Đã TT: {report.Summary.TongSoKyDaThanhToan}, Còn lại: {report.Summary.TongSoKyConPhaiThanhToan})";
            worksheet.Cell("H10").Value = $"Tổng giá trị hợp đồng: {report.Summary.TongGiaTriHopDong:#,##0} VND";

            worksheet.Cell("A11").Value = $"Lũy kế đã thanh toán: {report.Summary.TongSoTienDaThanhToan:#,##0} VND";
            worksheet.Cell("D11").Value = $"Lũy kế còn phải TT: {report.Summary.TongSoTienConPhaiThanhToan:#,##0} VND";
            worksheet.Cell("H11").Value = $"Kế hoạch TT trong năm {year}: {report.Summary.TongPhaiThanhToanTrongNam:#,##0} VND (Đã TT: {report.Summary.TongDaThanhToanTrongNam:#,##0}, Còn lại: {report.Summary.TongConPhaiThanhToanTrongNam:#,##0})";

            var summaryBox = worksheet.Range("A9:Q11");
            summaryBox.Style.Fill.BackgroundColor = XLColor.FromHtml("#F9FAFB");
            summaryBox.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            summaryBox.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E5E7EB");

            // Main Table Headers (Row 13-14)
            int headerRow = 13;
            worksheet.Cell(headerRow, 1).Value = "STT";
            worksheet.Cell(headerRow, 2).Value = "Mã hợp đồng";
            worksheet.Cell(headerRow, 3).Value = "Tên hợp đồng";
            worksheet.Cell(headerRow, 4).Value = "Loại hợp đồng";
            worksheet.Cell(headerRow, 5).Value = "Dự án / Gói thầu";
            worksheet.Cell(headerRow, 6).Value = "Nhà thầu / Đối tác";
            worksheet.Cell(headerRow, 7).Value = "Giá trị HĐ (VND)";
            worksheet.Cell(headerRow, 8).Value = "Tổng số kỳ";
            worksheet.Cell(headerRow, 9).Value = "Số kỳ đã TT";
            worksheet.Cell(headerRow, 10).Value = "Số kỳ còn lại";
            worksheet.Cell(headerRow, 11).Value = "Đã thanh toán (VND)";
            worksheet.Cell(headerRow, 12).Value = "Còn phải thanh toán (VND)";
            worksheet.Cell(headerRow, 13).Value = "% Đã TT";
            worksheet.Cell(headerRow, 14).Value = $"Phải TT năm {year} (VND)";
            worksheet.Cell(headerRow, 15).Value = $"Đã TT năm {year} (VND)";
            worksheet.Cell(headerRow, 16).Value = $"Còn lại năm {year} (VND)";
            worksheet.Cell(headerRow, 17).Value = "Trạng thái";

            var headerRange = worksheet.Range(headerRow, 1, headerRow, 17);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            int currentRow = 14;
            int stt = 1;

            foreach (var row in report.Rows)
            {
                worksheet.Cell(currentRow, 1).Value = stt++;
                worksheet.Cell(currentRow, 2).Value = row.MaHopDong;
                worksheet.Cell(currentRow, 3).Value = row.TenHopDong;
                worksheet.Cell(currentRow, 4).Value = row.LoaiHopDongTen;
                
                string projectOrPackage = !string.IsNullOrEmpty(row.TenDuAn) 
                    ? row.TenDuAn 
                    : (!string.IsNullOrEmpty(row.TenGoiThau) ? row.TenGoiThau : "-");
                worksheet.Cell(currentRow, 5).Value = projectOrPackage;
                
                worksheet.Cell(currentRow, 6).Value = row.TenNhaThau ?? "-";
                worksheet.Cell(currentRow, 7).Value = row.GiaTriHopDong;
                worksheet.Cell(currentRow, 8).Value = row.TongSoKy;
                worksheet.Cell(currentRow, 9).Value = row.SoKyDaThanhToan;
                worksheet.Cell(currentRow, 10).Value = row.SoKyConPhaiThanhToan;
                worksheet.Cell(currentRow, 11).Value = row.SoTienDaThanhToan;
                worksheet.Cell(currentRow, 12).Value = row.SoTienConPhaiThanhToan;
                worksheet.Cell(currentRow, 13).Value = row.TyLeDaThanhToanPercent / 100.0;
                worksheet.Cell(currentRow, 14).Value = row.PhaiThanhToanTrongNam;
                worksheet.Cell(currentRow, 15).Value = row.DaThanhToanTrongNam;
                worksheet.Cell(currentRow, 16).Value = row.ConPhaiThanhToanTrongNam;
                worksheet.Cell(currentRow, 17).Value = row.TrangThaiThanhToan;

                var rowRange = worksheet.Range(currentRow, 1, currentRow, 17);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                // Numeric formats
                worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 11).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 12).Style.NumberFormat.Format = "#,##0";
                
                worksheet.Cell(currentRow, 13).Style.NumberFormat.Format = "0.0%";
                worksheet.Cell(currentRow, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(currentRow, 14).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 15).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 16).Style.NumberFormat.Format = "#,##0";

                worksheet.Cell(currentRow, 17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                currentRow++;
            }

            // Total Footer Row
            worksheet.Cell(currentRow, 1).Value = "TỔNG CỘNG";
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Range(currentRow, 1, currentRow, 6).Merge();
            worksheet.Range(currentRow, 1, currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 7).Value = report.Summary.TongGiaTriHopDong;
            worksheet.Cell(currentRow, 8).Value = report.Summary.TongSoKyThanhToan;
            worksheet.Cell(currentRow, 9).Value = report.Summary.TongSoKyDaThanhToan;
            worksheet.Cell(currentRow, 10).Value = report.Summary.TongSoKyConPhaiThanhToan;
            worksheet.Cell(currentRow, 11).Value = report.Summary.TongSoTienDaThanhToan;
            worksheet.Cell(currentRow, 12).Value = report.Summary.TongSoTienConPhaiThanhToan;
            
            double grandRatio = report.Summary.TongGiaTriHopDong > 0 
                ? (double)(report.Summary.TongSoTienDaThanhToan / report.Summary.TongGiaTriHopDong) 
                : 0;
            worksheet.Cell(currentRow, 13).Value = grandRatio;

            worksheet.Cell(currentRow, 14).Value = report.Summary.TongPhaiThanhToanTrongNam;
            worksheet.Cell(currentRow, 15).Value = report.Summary.TongDaThanhToanTrongNam;
            worksheet.Cell(currentRow, 16).Value = report.Summary.TongConPhaiThanhToanTrongNam;
            worksheet.Cell(currentRow, 17).Value = "";

            var totalRowRange = worksheet.Range(currentRow, 1, currentRow, 17);
            totalRowRange.Style.Font.Bold = true;
            totalRowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
            totalRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            totalRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            for (int c = 7; c <= 16; c++)
            {
                if (c == 8 || c == 9 || c == 10)
                {
                    worksheet.Cell(currentRow, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else if (c == 13)
                {
                    worksheet.Cell(currentRow, c).Style.NumberFormat.Format = "0.0%";
                }
                else
                {
                    worksheet.Cell(currentRow, c).Style.NumberFormat.Format = "#,##0";
                }
            }

            // Adjust Column Widths
            worksheet.Column(1).Width = 6;   // STT
            worksheet.Column(2).Width = 16;  // Mã HĐ
            worksheet.Column(3).Width = 35;  // Tên HĐ
            worksheet.Column(4).Width = 22;  // Loại HĐ
            worksheet.Column(5).Width = 28;  // Dự án / Gói thầu
            worksheet.Column(6).Width = 25;  // Nhà thầu
            worksheet.Column(7).Width = 20;  // Giá trị HĐ
            worksheet.Column(8).Width = 12;  // Tổng kỳ
            worksheet.Column(9).Width = 12;  // Kỳ đã TT
            worksheet.Column(10).Width = 12; // Kỳ còn lại
            worksheet.Column(11).Width = 20; // Đã TT
            worksheet.Column(12).Width = 20; // Còn phải TT
            worksheet.Column(13).Width = 12; // % Đã TT
            worksheet.Column(14).Width = 20; // Phải TT trong năm
            worksheet.Column(15).Width = 20; // Đã TT trong năm
            worksheet.Column(16).Width = 20; // Còn lại trong năm
            worksheet.Column(17).Width = 16; // Trạng thái

            using (var memoryStream = new MemoryStream())
            {
                workbook.SaveAs(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }

    public async Task<byte[]> ExportContractPaymentReportCsvAsync(int year, int? loaiHopDong, string? search)
    {
        var report = await GetContractPaymentReportAsync(year, loaiHopDong, search);

        using (var memoryStream = new MemoryStream())
        {
            using (var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8))
            {
                writer.Write('\uFEFF'); // UTF-8 BOM

                await writer.WriteLineAsync($"\"NGÂN HÀNG HỢP TÁC XÃ VIỆT NAM\"");
                await writer.WriteLineAsync($"\"TRUNG TÂM CÔNG NGHỆ THÔNG TIN\"");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"\"BÁO CÁO THEO DÕI THANH TOÁN HỢP ĐỒNG NĂM {year}\"");
                await writer.WriteLineAsync($"\"Phân loại: {EscapeCsvField(report.LoaiHopDongFilterTen)}\"");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"\"Đơn vị tính: Đồng (VND)\"");
                await writer.WriteLineAsync();

                await writer.WriteLineAsync($"\"STT\",\"Mã hợp đồng\",\"Tên hợp đồng\",\"Loại hợp đồng\",\"Dự án / Gói thầu\",\"Nhà thầu\",\"Giá trị HĐ (VND)\",\"Tổng số kỳ\",\"Số kỳ đã TT\",\"Số kỳ còn lại\",\"Đã thanh toán (VND)\",\"Còn phải thanh toán (VND)\",\"% Đã TT\",\"Phải TT trong năm {year} (VND)\",\"Đã TT trong năm {year} (VND)\",\"Còn lại trong năm {year} (VND)\",\"Trạng thái\"");

                int stt = 1;
                foreach (var r in report.Rows)
                {
                    string proj = !string.IsNullOrEmpty(r.TenDuAn) ? r.TenDuAn : (!string.IsNullOrEmpty(r.TenGoiThau) ? r.TenGoiThau : "-");
                    await writer.WriteLineAsync($"\"{stt++}\",\"{EscapeCsvField(r.MaHopDong)}\",\"{EscapeCsvField(r.TenHopDong)}\",\"{EscapeCsvField(r.LoaiHopDongTen)}\",\"{EscapeCsvField(proj)}\",\"{EscapeCsvField(r.TenNhaThau ?? "-")}\",\"{r.GiaTriHopDong}\",\"{r.TongSoKy}\",\"{r.SoKyDaThanhToan}\",\"{r.SoKyConPhaiThanhToan}\",\"{r.SoTienDaThanhToan}\",\"{r.SoTienConPhaiThanhToan}\",\"{r.TyLeDaThanhToanPercent}%\",\"{r.PhaiThanhToanTrongNam}\",\"{r.DaThanhToanTrongNam}\",\"{r.ConPhaiThanhToanTrongNam}\",\"{EscapeCsvField(r.TrangThaiThanhToan)}\"");
                }

                await writer.FlushAsync();
            }
            return memoryStream.ToArray();
        }
    }

    public async Task<byte[]> ExportContractPaymentReportHtmlAsync(int year, int? loaiHopDong, string? search)
    {
        var report = await GetContractPaymentReportAsync(year, loaiHopDong, search);

        var htmlBuilder = new System.Text.StringBuilder();
        htmlBuilder.AppendLine("<!DOCTYPE html>");
        htmlBuilder.AppendLine("<html>");
        htmlBuilder.AppendLine("<head>");
        htmlBuilder.AppendLine("<meta charset=\"utf-8\" />");
        htmlBuilder.AppendLine($"<title>{System.Web.HttpUtility.HtmlEncode(report.Title)}</title>");
        htmlBuilder.AppendLine("<style>");
        htmlBuilder.AppendLine("  body { font-family: 'Times New Roman', Times, serif; margin: 30px; font-size: 13px; color: #1f2937; }");
        htmlBuilder.AppendLine("  .title { text-align: center; font-size: 18px; font-weight: bold; margin-bottom: 5px; }");
        htmlBuilder.AppendLine("  .subtitle { text-align: center; font-size: 13px; font-style: italic; margin-bottom: 20px; color: #4b5563; }");
        htmlBuilder.AppendLine("  table { width: 100%; border-collapse: collapse; margin-top: 15px; }");
        htmlBuilder.AppendLine("  th, td { border: 1px solid #d1d5db; padding: 6px 8px; font-size: 12px; }");
        htmlBuilder.AppendLine("  th { background-color: #f3f4f6; font-weight: bold; text-align: center; }");
        htmlBuilder.AppendLine("  .text-center { text-align: center; }");
        htmlBuilder.AppendLine("  .text-right { text-align: right; }");
        htmlBuilder.AppendLine("  .bold { font-weight: bold; }");
        htmlBuilder.AppendLine("</style>");
        htmlBuilder.AppendLine("</head>");
        htmlBuilder.AppendLine("<body>");

        htmlBuilder.AppendLine($"<div class=\"title\">{System.Web.HttpUtility.HtmlEncode(report.Title)}</div>");
        htmlBuilder.AppendLine($"<div class=\"subtitle\">Phân loại: {System.Web.HttpUtility.HtmlEncode(report.LoaiHopDongFilterTen)} | Đơn vị tính: Đồng (VND)</div>");

        htmlBuilder.AppendLine("<table>");
        htmlBuilder.AppendLine("  <thead>");
        htmlBuilder.AppendLine("    <tr>");
        htmlBuilder.AppendLine("      <th>STT</th>");
        htmlBuilder.AppendLine("      <th>Mã HĐ</th>");
        htmlBuilder.AppendLine("      <th>Tên Hợp đồng</th>");
        htmlBuilder.AppendLine("      <th>Loại HĐ</th>");
        htmlBuilder.AppendLine("      <th>Dự án / Gói thầu</th>");
        htmlBuilder.AppendLine("      <th>Nhà thầu</th>");
        htmlBuilder.AppendLine("      <th>Giá trị HĐ</th>");
        htmlBuilder.AppendLine("      <th>Số kỳ</th>");
        htmlBuilder.AppendLine("      <th>Đã TT</th>");
        htmlBuilder.AppendLine("      <th>Còn lại</th>");
        htmlBuilder.AppendLine("      <th>Số tiền đã TT</th>");
        htmlBuilder.AppendLine("      <th>Số tiền còn lại</th>");
        htmlBuilder.AppendLine("      <th>Trạng thái</th>");
        htmlBuilder.AppendLine("    </tr>");
        htmlBuilder.AppendLine("  </thead>");
        htmlBuilder.AppendLine("  <tbody>");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            string proj = !string.IsNullOrEmpty(r.TenDuAn) ? r.TenDuAn : (!string.IsNullOrEmpty(r.TenGoiThau) ? r.TenGoiThau : "-");
            htmlBuilder.AppendLine("    <tr>");
            htmlBuilder.AppendLine($"      <td class=\"text-center\">{stt++}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-center\">{System.Web.HttpUtility.HtmlEncode(r.MaHopDong)}</td>");
            htmlBuilder.AppendLine($"      <td>{System.Web.HttpUtility.HtmlEncode(r.TenHopDong)}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-center\">{System.Web.HttpUtility.HtmlEncode(r.LoaiHopDongTen)}</td>");
            htmlBuilder.AppendLine($"      <td>{System.Web.HttpUtility.HtmlEncode(proj)}</td>");
            htmlBuilder.AppendLine($"      <td>{System.Web.HttpUtility.HtmlEncode(r.TenNhaThau ?? "-")}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-right\">{r.GiaTriHopDong:#,##0}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-center\">{r.TongSoKy}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-center\">{r.SoKyDaThanhToan}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-center\">{r.SoKyConPhaiThanhToan}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-right\">{r.SoTienDaThanhToan:#,##0}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-right\">{r.SoTienConPhaiThanhToan:#,##0}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-center\">{System.Web.HttpUtility.HtmlEncode(r.TrangThaiThanhToan)}</td>");
            htmlBuilder.AppendLine("    </tr>");
        }

        htmlBuilder.AppendLine("  </tbody>");
        htmlBuilder.AppendLine("</table>");
        htmlBuilder.AppendLine("</body>");
        htmlBuilder.AppendLine("</html>");

        return System.Text.Encoding.UTF8.GetBytes(htmlBuilder.ToString());
    }
}

