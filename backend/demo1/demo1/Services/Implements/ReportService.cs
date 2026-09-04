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

using Microsoft.Extensions.Logging;

namespace demo1.Services.Implements;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReportService> _logger;
    private readonly ICurrentUserService? _currentUserService;

    public ReportService(AppDbContext context, ILogger<ReportService> logger, ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    private static (decimal Factor, string UnitName) ParseUnit(string? donViTinh)
    {
        if (string.IsNullOrWhiteSpace(donViTinh))
            return (1m, "Đồng");

        var s = donViTinh.Trim().ToLowerInvariant();
        if (s == "4" || s.Contains("tỷ") || s.Contains("ty"))
            return (1_000_000_000m, "Tỷ đồng");
        if (s == "3" || s.Contains("triệu") || s.Contains("trieu"))
            return (1_000_000m, "Triệu đồng");
        if (s == "2" || s.Contains("nghìn") || s.Contains("ngan") || s == "k")
            return (1_000m, "Nghìn đồng");
        if (s == "1" || s.Contains("đồng") || s.Contains("dong") || s == "vnd")
            return (1m, "Đồng");

        return (1m, "Đồng");
    }

    public async Task<ReportResponseDto> GetInvestmentReportAsync(int year, int period, string? donViTinh = null)
    {
        var (conversionFactor, unitName) = ParseUnit(donViTinh);

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

        // 2. Tải bản đồ ngân sách tất cả dự án nguồn (LoaiDuAn = 1) để tính tổng ngân sách cho dự án triển khai theo danh sách dự án nguồn được chọn
        var sourceProjectsMap = await _context.DuAns
            .AsNoTracking()
            .Where(da => da.IsActive && !da.IsDeleted && da.LoaiDuAn == 1)
            .Select(da => new
            {
                da.Id,
                da.DuToanPheDuyet,
                AdjustmentsSum = da.DieuChinhs
                    .Where(dc => dc.IsActive && !dc.IsDeleted && dc.NgayDieuChinh <= endOfPeriod)
                    .Sum(dc => (decimal?)dc.GiaTriDieuChinh) ?? 0
            })
            .ToDictionaryAsync(p => p.Id, p => p);

        // 3. Tải danh sách dự án hợp lệ (lọc phân quyền, bỏ qua dự án đã xóa, loại bỏ dự án nguồn đã triển khai để tránh trùng lặp)
        var query = _context.DuAns
            .AsNoTracking()
            .Where(da => da.IsActive && !da.IsDeleted);

        // Tránh trùng lặp dự án nguồn đã triển khai thành dự án thực hiện
        query = query.Where(da => !(da.LoaiDuAn == 1 && da.DaTrienKhai == true));

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
                da.LoaiDuAn,
                SourceIds = da.NguonDuAns.Select(nk => nk.NguonProjectId).ToList(),
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
                    .Where(dc => dc.IsActive && !dc.IsDeleted && dc.NgayDieuChinh <= endOfPeriod)
                    .Sum(dc => (decimal?)dc.GiaTriDieuChinh) ?? 0
            })
            .ToListAsync();

        // 4. Tính toán giá trị lũy kế đã thanh toán (chỉ tính đợt đã thanh toán IsPaid = true, hợp đồng active & chưa xóa, dùng ngày thanh toán thực tế)
        var performedValues = await _context.DotThanhToans
            .AsNoTracking()
            .Where(dt => dt.IsPaid 
                && dt.HopDong != null 
                && dt.HopDong.IsActive 
                && !dt.HopDong.IsDeleted 
                && dt.HopDong.DuAnId.HasValue)
            .Select(dt => new
            {
                DuAnId = dt.HopDong.DuAnId!.Value,
                PaymentDate = dt.NgayThanhToan ?? dt.CreatedAt,
                dt.GiaTriThanhToan
            })
            .GroupBy(x => x.DuAnId)
            .Select(g => new
            {
                DuAnId = g.Key,
                KyTruoc = g.Where(x => x.PaymentDate < startOfPeriod).Sum(x => x.GiaTriThanhToan),
                TrongKy = g.Where(x => x.PaymentDate >= startOfPeriod && x.PaymentDate <= endOfPeriod).Sum(x => x.GiaTriThanhToan)
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
            // Tính toán tổng ngân sách theo các dự án nguồn được chọn (nếu là dự án triển khai)
            decimal totalBudgetVnd = 0;
            if (project.LoaiDuAn == 2 && project.SourceIds != null && project.SourceIds.Any())
            {
                var sourceIds = project.SourceIds;

                if (sourceIds.Any())
                {
                    foreach (var sourceId in sourceIds)
                    {
                        if (sourceProjectsMap.TryGetValue(sourceId, out var sp))
                        {
                            totalBudgetVnd += (sp.DuToanPheDuyet + sp.AdjustmentsSum);
                        }
                    }
                }

                if (totalBudgetVnd == 0)
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

            // Quy đổi theo đơn vị tính
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
            Unit = unitName,
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

    public async Task<byte[]> ExportInvestmentReportExcelAsync(int year, int period, string? donViTinh = null)
    {
        var report = await GetInvestmentReportAsync(year, period, donViTinh);
        
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
            worksheet.Cell("L8").Value = $"Đơn vị tính: {report.Unit}";
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

    public async Task<byte[]> ExportInvestmentReportCsvAsync(int year, int period, string? donViTinh = null)
    {
        var report = await GetInvestmentReportAsync(year, period, donViTinh);
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

    public async Task<byte[]> ExportInvestmentReportHtmlAsync(int year, int period, string? donViTinh = null)
    {
        var report = await GetInvestmentReportAsync(year, period, donViTinh);
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

    public async Task<CongViecGoiThauReportDto> GetCongViecGoiThauReportAsync(Guid idGoiThau, string? donViTinh = null)
    {
        var (factor, unitName) = ParseUnit(donViTinh);

        var goiThau = await _context.GoiThaus
            .Include(g => g.DuAn)
            .Include(g => g.CongViecGoiThaus)
            .FirstOrDefaultAsync(g => g.Id == idGoiThau && (g.DuAn == null || g.DuAn.LoaiDuAn == 2));

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
            Unit = unitName,
            GiaTriGoiThau = goiThau.GiaTriGoiThau / factor,
            CongViecs = congViecs,
            TongSoCongViec = congViecs.Count,
            SoCongViecDaHoanThanh = completed,
            SoCongViecDangThucHien = inProgress
        };
    }

    public async Task<byte[]> ExportCongViecGoiThauReportExcelAsync(Guid idGoiThau, string? donViTinh = null)
    {
        var report = await GetCongViecGoiThauReportAsync(idGoiThau, donViTinh);

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

    public async Task<ContractPaymentReportResponseDto> GetContractPaymentReportAsync(int year, int? loaiHopDong, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null)
    {
        var (factor, unitName) = ParseUnit(donViTinh);

        var query = _context.HopDongs
            .Include(h => h.DotThanhToans)
            .Include(h => h.DuAn)
            .Include(h => h.GoiThau)
            .Include(h => h.NhaThau)
            .Where(h => h.IsActive && !h.IsDeleted && (h.DuAn == null || h.DuAn.LoaiDuAn == 2));

        if (_currentUserService != null)
        {
            var currentUsername = _currentUserService.GetUsername();
            if (!string.IsNullOrEmpty(currentUsername))
            {
                var currentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);
                if (currentUser != null && !currentUser.IsSystemAdmin)
                {
                    query = query.Where(h => (h.DuAn != null && (h.DuAn.CreatedByUserId == currentUser.Id || h.DuAn.ChuDuAnId == currentUser.Id)) 
                        || _context.UserPermissions.Any(up => up.UserId == currentUser.Id && up.DuAnId == h.DuAnId)
                        || _context.CongViecNguoiLienQuans.Any(nlq => nlq.UserId == currentUser.Id && nlq.CongViecGoiThau != null && ((h.GoiThauId.HasValue && nlq.CongViecGoiThau.GoiThauId == h.GoiThauId.Value) || (h.DuAnId.HasValue && nlq.CongViecGoiThau.GoiThau != null && nlq.CongViecGoiThau.GoiThau.DuAnId == h.DuAnId.Value)))
                        || (currentUser.CanViewHopDong && h.LoaiHopDongNavigation != null && h.LoaiHopDongNavigation.Code == "01"));
                }
            }
        }

        if (loaiHopDong.HasValue && loaiHopDong.Value > 0)
        {
            query = query.Where(h => h.LoaiHopDong == loaiHopDong.Value);
        }

        if (loaiHopDongIds != null && loaiHopDongIds.Count > 0)
        {
            query = query.Where(h => h.LoaiHopDongId.HasValue && loaiHopDongIds.Contains(h.LoaiHopDongId.Value));
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

            decimal rawSoTienDaThanhToan = milestones.Where(m => m.IsPaid).Sum(m => m.GiaTriThanhToan);
            decimal rawSoTienConPhaiThanhToan = contract.GiaTriHopDong - rawSoTienDaThanhToan;
            if (rawSoTienConPhaiThanhToan < 0) rawSoTienConPhaiThanhToan = 0;

            double tyLeDaThanhToanPercent = contract.GiaTriHopDong > 0 
                ? (double)Math.Round(rawSoTienDaThanhToan / contract.GiaTriHopDong * 100, 2) 
                : 0;

            // Milestones scheduled or recorded in the target year
            var milestonesInYear = milestones.Where(m => 
                (m.NgayThanhToan.HasValue && m.NgayThanhToan.Value.Year == year) ||
                (!m.NgayThanhToan.HasValue && m.CreatedAt.Year == year)
            ).ToList();

            decimal rawPhaiThanhToanTrongNam = milestonesInYear.Sum(m => m.GiaTriThanhToan);
            decimal rawDaThanhToanTrongNam = milestonesInYear.Where(m => m.IsPaid).Sum(m => m.GiaTriThanhToan);
            decimal rawConPhaiThanhToanTrongNam = rawPhaiThanhToanTrongNam - rawDaThanhToanTrongNam;
            if (rawConPhaiThanhToanTrongNam < 0) rawConPhaiThanhToanTrongNam = 0;

            // Apply unit conversion factor
            decimal giaTriHopDong = contract.GiaTriHopDong / factor;
            decimal soTienDaThanhToan = rawSoTienDaThanhToan / factor;
            decimal soTienConPhaiThanhToan = rawSoTienConPhaiThanhToan / factor;

            decimal phaiThanhToanTrongNam = rawPhaiThanhToanTrongNam / factor;
            decimal daThanhToanTrongNam = rawDaThanhToanTrongNam / factor;
            decimal conPhaiThanhToanTrongNam = rawConPhaiThanhToanTrongNam / factor;

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
                GiaTriThanhToan = m.GiaTriThanhToan / factor,
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
                GiaTriHopDong = giaTriHopDong,
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
            Unit = unitName,
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

    public async Task<byte[]> ExportContractPaymentReportExcelAsync(int year, int? loaiHopDong, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null)
    {
        var report = await GetContractPaymentReportAsync(year, loaiHopDong, loaiHopDongIds, search, donViTinh);

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
            worksheet.Cell("Q7").Value = $"Đơn vị tính: {report.Unit}";
            worksheet.Cell("Q7").Style.Font.Italic = true;
            worksheet.Cell("Q7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Summary Section (Rows 9-11)
            worksheet.Cell("A9").Value = "TỔNG QUAN THEO DÕI THANH TOÁN";
            worksheet.Cell("A9").Style.Font.Bold = true;
            worksheet.Range("A9:D9").Merge();

            worksheet.Cell("A10").Value = $"Tổng số hợp đồng: {report.Summary.TongSoHopDong} (Bảo trì: {report.Summary.SoHopDongBaoTri})";
            worksheet.Cell("D10").Value = $"Tổng kỳ thanh toán: {report.Summary.TongSoKyThanhToan} (Đã TT: {report.Summary.TongSoKyDaThanhToan}, Còn lại: {report.Summary.TongSoKyConPhaiThanhToan})";
            worksheet.Cell("H10").Value = $"Tổng giá trị hợp đồng: {report.Summary.TongGiaTriHopDong:#,##0} {report.Unit}";

            worksheet.Cell("A11").Value = $"Lũy kế đã thanh toán: {report.Summary.TongSoTienDaThanhToan:#,##0} {report.Unit}";
            worksheet.Cell("D11").Value = $"Lũy kế còn phải TT: {report.Summary.TongSoTienConPhaiThanhToan:#,##0} {report.Unit}";
            worksheet.Cell("H11").Value = $"Kế hoạch TT trong năm {year}: {report.Summary.TongPhaiThanhToanTrongNam:#,##0} {report.Unit} (Đã TT: {report.Summary.TongDaThanhToanTrongNam:#,##0}, Còn lại: {report.Summary.TongConPhaiThanhToanTrongNam:#,##0})";

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
            worksheet.Cell(headerRow, 7).Value = $"Giá trị HĐ ({report.Unit})";
            worksheet.Cell(headerRow, 8).Value = "Tổng số kỳ";
            worksheet.Cell(headerRow, 9).Value = "Số kỳ đã TT";
            worksheet.Cell(headerRow, 10).Value = "Số kỳ còn lại";
            worksheet.Cell(headerRow, 11).Value = $"Đã thanh toán ({report.Unit})";
            worksheet.Cell(headerRow, 12).Value = $"Còn phải thanh toán ({report.Unit})";
            worksheet.Cell(headerRow, 13).Value = "% Đã TT";
            worksheet.Cell(headerRow, 14).Value = $"Phải TT năm {year} ({report.Unit})";
            worksheet.Cell(headerRow, 15).Value = $"Đã TT năm {year} ({report.Unit})";
            worksheet.Cell(headerRow, 16).Value = $"Còn lại năm {year} ({report.Unit})";
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

    public async Task<byte[]> ExportContractPaymentReportCsvAsync(int year, int? loaiHopDong, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null)
    {
        var report = await GetContractPaymentReportAsync(year, loaiHopDong, loaiHopDongIds, search, donViTinh);

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
                await writer.WriteLineAsync($"\"Đơn vị tính: {report.Unit}\"");
                await writer.WriteLineAsync();

                await writer.WriteLineAsync($"\"STT\",\"Mã hợp đồng\",\"Tên hợp đồng\",\"Loại hợp đồng\",\"Dự án / Gói thầu\",\"Nhà thầu\",\"Giá trị HĐ ({report.Unit})\",\"Tổng số kỳ\",\"Số kỳ đã TT\",\"Số kỳ còn lại\",\"Đã thanh toán ({report.Unit})\",\"Còn phải thanh toán ({report.Unit})\",\"% Đã TT\",\"Phải TT trong năm {year} ({report.Unit})\",\"Đã TT trong năm {year} ({report.Unit})\",\"Còn lại trong năm {year} ({report.Unit})\",\"Trạng thái\"");

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

    public async Task<byte[]> ExportContractPaymentReportHtmlAsync(int year, int? loaiHopDong, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null)
    {
        var report = await GetContractPaymentReportAsync(year, loaiHopDong, loaiHopDongIds, search, donViTinh);

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
        htmlBuilder.AppendLine($"<div class=\"subtitle\">Phân loại: {System.Web.HttpUtility.HtmlEncode(report.LoaiHopDongFilterTen)} | Đơn vị tính: {System.Web.HttpUtility.HtmlEncode(report.Unit)}</div>");

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

    public async Task<TheoDoiHopDongReportResponseDto> GetTheoDoiHopDongReportAsync(int? year, DateTime? cutoffDate, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null)
    {
        var (factor, unitName) = ParseUnit(donViTinh);
        int selectedYear = year ?? DateTime.Now.Year;
        DateTime targetCutoffDate = cutoffDate ?? new DateTime(selectedYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var query = _context.HopDongs
            .Include(h => h.DotThanhToans)
            .Include(h => h.DuAn)
            .Include(h => h.GoiThau)
            .Include(h => h.NhaThau)
            .Where(h => h.IsActive && !h.IsDeleted && (h.DuAn == null || h.DuAn.LoaiDuAn == 2));

        if (_currentUserService != null)
        {
            var currentUsername = _currentUserService.GetUsername();
            if (!string.IsNullOrEmpty(currentUsername))
            {
                var currentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);
                if (currentUser != null && !currentUser.IsSystemAdmin)
                {
                    query = query.Where(h => (h.DuAn != null && (h.DuAn.CreatedByUserId == currentUser.Id || h.DuAn.ChuDuAnId == currentUser.Id)) 
                        || _context.UserPermissions.Any(up => up.UserId == currentUser.Id && up.DuAnId == h.DuAnId)
                        || _context.CongViecNguoiLienQuans.Any(nlq => nlq.UserId == currentUser.Id && nlq.CongViecGoiThau != null && ((h.GoiThauId.HasValue && nlq.CongViecGoiThau.GoiThauId == h.GoiThauId.Value) || (h.DuAnId.HasValue && nlq.CongViecGoiThau.GoiThau != null && nlq.CongViecGoiThau.GoiThau.DuAnId == h.DuAnId.Value)))
                        || (currentUser.CanViewHopDong && h.LoaiHopDongNavigation != null && h.LoaiHopDongNavigation.Code == "01"));
                }
            }
        }

        if (loaiHopDongIds != null && loaiHopDongIds.Count > 0)
        {
            query = query.Where(h => h.LoaiHopDongId.HasValue && loaiHopDongIds.Contains(h.LoaiHopDongId.Value));
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
            .OrderBy(h => h.Code)
            .ThenByDescending(h => h.CreatedAt)
            .ToListAsync();

        var rows = new List<TheoDoiHopDongReportRowDto>();
        int stt = 1;

        foreach (var contract in contracts)
        {
            var milestones = contract.DotThanhToans != null ? contract.DotThanhToans.ToList() : new List<DotThanhToan>();

            decimal rawGiaTriDaThanhToan = milestones.Where(m => m.IsPaid).Sum(m => m.GiaTriThanhToan);
            decimal rawGiaTriConLai = contract.GiaTriHopDong - rawGiaTriDaThanhToan;
            if (rawGiaTriConLai < 0) rawGiaTriConLai = 0;

            decimal rawTongSauMoc = milestones
                .Where(m => (m.NgayThanhToan.HasValue && m.NgayThanhToan.Value > targetCutoffDate) ||
                            (!m.NgayThanhToan.HasValue && m.CreatedAt > targetCutoffDate))
                .Sum(m => m.GiaTriThanhToan);

            decimal rawDuKienThanhToanDenMoc = rawGiaTriConLai - rawTongSauMoc;
            if (rawDuKienThanhToanDenMoc < 0) rawDuKienThanhToanDenMoc = 0;

            decimal giaTriHopDong = contract.GiaTriHopDong / factor;
            decimal giaTriDaThanhToan = rawGiaTriDaThanhToan / factor;
            decimal giaTriConLai = rawGiaTriConLai / factor;
            decimal duKienThanhToanDenMoc = rawDuKienThanhToanDenMoc / factor;

            var milestoneDtos = milestones.Select(m => new TheoDoiHopDongDotThanhToanDto
            {
                Id = m.Id,
                TenDot = m.TenDot,
                TyLeThanhToan = m.TyLeThanhToan,
                GiaTriThanhToan = m.GiaTriThanhToan / factor,
                NgayThanhToan = m.NgayThanhToan,
                DieuKienThanhToan = m.DieuKienThanhToan,
                IsPaid = m.IsPaid
            }).ToList();

            rows.Add(new TheoDoiHopDongReportRowDto
            {
                Stt = stt++,
                HopDongId = contract.Id,
                SoHopDong = contract.Code,
                TenHopDong = contract.Name,
                NgayKyHopDong = contract.NgayHieuLuc,
                NgayKetThucDuKien = contract.ExpiredDate,
                GiaTriHopDong = giaTriHopDong,
                GiaTriDaThanhToan = giaTriDaThanhToan,
                GiaTriConLai = giaTriConLai,
                DuKienThanhToanDenMoc = duKienThanhToanDenMoc,
                GhiChu = contract.Description,
                LoaiHopDong = contract.LoaiHopDong,
                LoaiHopDongTen = GetLoaiHopDongName(contract.LoaiHopDong),
                TenDuAn = contract.DuAn?.Name,
                TenGoiThau = contract.GoiThau?.Name,
                TenNhaThau = contract.NhaThau?.Name,
                DanhSachDotThanhToan = milestoneDtos
            });
        }

        var summary = new TheoDoiHopDongReportSummaryDto
        {
            TongSoHopDong = rows.Count,
            TongGiaTriHopDong = rows.Sum(r => r.GiaTriHopDong),
            TongGiaTriDaThanhToan = rows.Sum(r => r.GiaTriDaThanhToan),
            TongGiaTriConLai = rows.Sum(r => r.GiaTriConLai),
            TongDuKienThanhToanDenMoc = rows.Sum(r => r.DuKienThanhToanDenMoc)
        };

        string loaiFilterName = loaiHopDongIds != null && loaiHopDongIds.Count == 1 ? "Loại hợp đồng cụ thể" : "Tất cả loại hợp đồng";

        return new TheoDoiHopDongReportResponseDto
        {
            Title = $"Báo cáo chi tiết từ ngày 01/01/{selectedYear} - đến ngày {targetCutoffDate:dd/MM/yyyy}",
            Unit = unitName,
            Year = selectedYear,
            CutoffDate = targetCutoffDate,
            LoaiHopDong = null,
            LoaiHopDongFilterTen = loaiFilterName,
            Summary = summary,
            Rows = rows
        };
    }

    public async Task<byte[]> ExportTheoDoiHopDongReportExcelAsync(int? year, DateTime? cutoffDate, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null)
    {
        var report = await GetTheoDoiHopDongReportAsync(year, cutoffDate, loaiHopDongIds, search, donViTinh);

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Theo doi HD");

            worksheet.Style.Font.FontName = "Times New Roman";
            worksheet.Style.Font.FontSize = 11;

            // Row 1: Title
            worksheet.Cell("B1").Value = report.Title;
            worksheet.Cell("B1").Style.Font.Bold = true;
            worksheet.Cell("B1").Style.Font.FontSize = 13;
            worksheet.Cell("B1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("B1:H1").Merge();

            // Row 2: Headers
            worksheet.Cell("A2").Value = "STT";
            worksheet.Cell("B2").Value = "Số hợp đồng";
            worksheet.Cell("C2").Value = "Tên hợp đồng";
            worksheet.Cell("D2").Value = "Ngày ký hợp đồng";
            worksheet.Cell("E2").Value = "Ngày kết thúc\n (dự kiến)";
            worksheet.Cell("F2").Value = "Giá trị hợp đồng";
            worksheet.Cell("G2").Value = "Giá trị đã\n thanh toán";
            worksheet.Cell("H2").Value = "Giá trị còn lại của \nhợp đồng";
            worksheet.Cell("I2").Value = $"Dự Kiến Thanh Toán Đến {report.CutoffDate:dd/MM/yyyy}";
            worksheet.Cell("J2").Value = "Ghi Chú";

            var headerRange = worksheet.Range("A2:J2");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Alignment.WrapText = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            int startRow = 3;
            int currentRow = startRow;

            foreach (var row in report.Rows)
            {
                worksheet.Cell(currentRow, 1).Value = row.Stt;
                worksheet.Cell(currentRow, 2).Value = row.SoHopDong;
                worksheet.Cell(currentRow, 3).Value = row.TenHopDong;

                if (row.NgayKyHopDong.HasValue)
                {
                    worksheet.Cell(currentRow, 4).Value = row.NgayKyHopDong.Value;
                    worksheet.Cell(currentRow, 4).Style.DateFormat.Format = "dd/MM/yyyy";
                }
                else
                {
                    worksheet.Cell(currentRow, 4).Value = string.Empty;
                }

                if (row.NgayKetThucDuKien.HasValue)
                {
                    worksheet.Cell(currentRow, 5).Value = row.NgayKetThucDuKien.Value;
                    worksheet.Cell(currentRow, 5).Style.DateFormat.Format = "dd/MM/yyyy";
                }
                else
                {
                    worksheet.Cell(currentRow, 5).Value = string.Empty;
                }

                worksheet.Cell(currentRow, 6).Value = row.GiaTriHopDong;
                if (row.GiaTriDaThanhToan > 0)
                {
                    worksheet.Cell(currentRow, 7).Value = row.GiaTriDaThanhToan;
                }
                else
                {
                    worksheet.Cell(currentRow, 7).Value = 0;
                }

                worksheet.Cell(currentRow, 8).Value = row.GiaTriConLai;

                if (row.DuKienThanhToanDenMoc > 0)
                {
                    worksheet.Cell(currentRow, 9).Value = row.DuKienThanhToanDenMoc;
                }
                else
                {
                    worksheet.Cell(currentRow, 9).Value = string.Empty;
                }

                worksheet.Cell(currentRow, 10).Value = row.GhiChu ?? string.Empty;

                var rowRange = worksheet.Range(currentRow, 1, currentRow, 10);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0";

                worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                worksheet.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                worksheet.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                currentRow++;
            }

            if (report.Rows.Count > 0)
            {
                int endDataRow = currentRow - 1;
                worksheet.Cell(currentRow, 9).FormulaA1 = $"=SUM(I{startRow}:I{endDataRow})";
                worksheet.Cell(currentRow, 9).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }

            worksheet.Column(1).Width = 6;   // STT
            worksheet.Column(2).Width = 28;  // Số hợp đồng
            worksheet.Column(3).Width = 50;  // Tên hợp đồng
            worksheet.Column(4).Width = 20;  // Ngày ký
            worksheet.Column(5).Width = 20;  // Ngày kết thúc
            worksheet.Column(6).Width = 18;  // Giá trị HĐ
            worksheet.Column(7).Width = 18;  // Giá trị đã TT
            worksheet.Column(8).Width = 22;  // Giá trị còn lại
            worksheet.Column(9).Width = 24;  // Dự kiến TT
            worksheet.Column(10).Width = 20; // Ghi chú

            using (var memoryStream = new MemoryStream())
            {
                workbook.SaveAs(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }

    public async Task<byte[]> ExportCongViecGoiThauReportCsvAsync(Guid idGoiThau, string? donViTinh = null)
    {
        var report = await GetCongViecGoiThauReportAsync(idGoiThau, donViTinh);

        using (var memoryStream = new MemoryStream())
        {
            using (var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8))
            {
                writer.Write('\uFEFF'); // UTF-8 BOM

                await writer.WriteLineAsync($"\"TRÌNH TỰ THỰC HIỆN CÁC BƯỚC CÔNG VIỆC GÓI THẦU\"");
                await writer.WriteLineAsync($"\"Mã gói thầu: {EscapeCsvField(report.MaGoiThau)}\"");
                await writer.WriteLineAsync($"\"Tên gói thầu: {EscapeCsvField(report.TenGoiThau)}\"");
                await writer.WriteLineAsync($"\"Dự án: {EscapeCsvField(report.TenDuAn ?? "-")}\"");
                await writer.WriteLineAsync($"\"Đơn vị tính: {report.Unit}\"");
                await writer.WriteLineAsync();

                await writer.WriteLineAsync($"\"STT\",\"Tên tài liệu\",\"Ngày ký\",\"Loại văn bản\",\"Tình trạng\",\"Ghi chú\"");

                foreach (var c in report.CongViecs)
                {
                    string ngayKy = c.NgayKy.HasValue ? c.NgayKy.Value.ToString("dd/MM/yyyy") : "-";
                    await writer.WriteLineAsync($"\"{c.Stt}\",\"{EscapeCsvField(c.TenTaiLieu)}\",\"{ngayKy}\",\"{EscapeCsvField(c.LoaiVanBan ?? "-")}\",\"{EscapeCsvField(c.TinhTrang ?? "-")}\",\"{EscapeCsvField(c.GhiChu ?? "-")}\"");
                }

                await writer.FlushAsync();
            }
            return memoryStream.ToArray();
        }
    }

    public async Task<byte[]> ExportCongViecGoiThauReportHtmlAsync(Guid idGoiThau, string? donViTinh = null)
    {
        var report = await GetCongViecGoiThauReportAsync(idGoiThau, donViTinh);

        var htmlBuilder = new System.Text.StringBuilder();
        htmlBuilder.AppendLine("<!DOCTYPE html>");
        htmlBuilder.AppendLine("<html>");
        htmlBuilder.AppendLine("<head>");
        htmlBuilder.AppendLine("<meta charset=\"utf-8\" />");
        htmlBuilder.AppendLine($"<title>Báo cáo Tiến độ Gói thầu {System.Web.HttpUtility.HtmlEncode(report.MaGoiThau)}</title>");
        htmlBuilder.AppendLine("<style>");
        htmlBuilder.AppendLine("  body { font-family: 'Times New Roman', Times, serif; margin: 30px; font-size: 13px; color: #1f2937; }");
        htmlBuilder.AppendLine("  .title { text-align: center; font-size: 18px; font-weight: bold; margin-bottom: 5px; }");
        htmlBuilder.AppendLine("  .subtitle { text-align: center; font-size: 13px; font-style: italic; margin-bottom: 20px; color: #4b5563; }");
        htmlBuilder.AppendLine("  table { width: 100%; border-collapse: collapse; margin-top: 15px; }");
        htmlBuilder.AppendLine("  th, td { border: 1px solid #d1d5db; padding: 6px 8px; font-size: 12px; }");
        htmlBuilder.AppendLine("  th { background-color: #f3f4f6; font-weight: bold; text-align: center; }");
        htmlBuilder.AppendLine("  .text-center { text-align: center; }");
        htmlBuilder.AppendLine("  .text-right { text-align: right; }");
        htmlBuilder.AppendLine("</style>");
        htmlBuilder.AppendLine("</head>");
        htmlBuilder.AppendLine("<body>");

        htmlBuilder.AppendLine($"<div class=\"title\">TRÌNH TỰ THỰC HIỆN CÁC BƯỚC CÔNG VIỆC GÓI THẦU</div>");
        htmlBuilder.AppendLine($"<div class=\"subtitle\">Gói thầu: {System.Web.HttpUtility.HtmlEncode(report.TenGoiThau)} ({System.Web.HttpUtility.HtmlEncode(report.MaGoiThau)}) | Dự án: {System.Web.HttpUtility.HtmlEncode(report.TenDuAn ?? "-")}</div>");

        htmlBuilder.AppendLine("<table>");
        htmlBuilder.AppendLine("  <thead>");
        htmlBuilder.AppendLine("    <tr>");
        htmlBuilder.AppendLine("      <th>STT</th>");
        htmlBuilder.AppendLine("      <th>Tên tài liệu</th>");
        htmlBuilder.AppendLine("      <th>Ngày ký</th>");
        htmlBuilder.AppendLine("      <th>Loại văn bản</th>");
        htmlBuilder.AppendLine("      <th>Tình trạng</th>");
        htmlBuilder.AppendLine("      <th>Ghi chú</th>");
        htmlBuilder.AppendLine("    </tr>");
        htmlBuilder.AppendLine("  </thead>");
        htmlBuilder.AppendLine("  <tbody>");

        foreach (var c in report.CongViecs)
        {
            string ngayKy = c.NgayKy.HasValue ? c.NgayKy.Value.ToString("dd/MM/yyyy") : "-";
            htmlBuilder.AppendLine("    <tr>");
            htmlBuilder.AppendLine($"      <td class=\"text-center\">{c.Stt}</td>");
            htmlBuilder.AppendLine($"      <td>{System.Web.HttpUtility.HtmlEncode(c.TenTaiLieu)}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-center\">{ngayKy}</td>");
            htmlBuilder.AppendLine($"      <td>{System.Web.HttpUtility.HtmlEncode(c.LoaiVanBan ?? "-")}</td>");
            htmlBuilder.AppendLine($"      <td>{System.Web.HttpUtility.HtmlEncode(c.TinhTrang ?? "-")}</td>");
            htmlBuilder.AppendLine($"      <td>{System.Web.HttpUtility.HtmlEncode(c.GhiChu ?? "")}</td>");
            htmlBuilder.AppendLine("    </tr>");
        }

        htmlBuilder.AppendLine("  </tbody>");
        htmlBuilder.AppendLine("</table>");
        htmlBuilder.AppendLine("</body>");
        htmlBuilder.AppendLine("</html>");

        return System.Text.Encoding.UTF8.GetBytes(htmlBuilder.ToString());
    }

    public async Task<byte[]> ExportTheoDoiHopDongReportCsvAsync(int? year, DateTime? cutoffDate, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null)
    {
        var report = await GetTheoDoiHopDongReportAsync(year, cutoffDate, loaiHopDongIds, search, donViTinh);

        using (var memoryStream = new MemoryStream())
        {
            using (var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8))
            {
                writer.Write('\uFEFF'); // UTF-8 BOM

                await writer.WriteLineAsync($"\"{EscapeCsvField(report.Title)}\"");
                await writer.WriteLineAsync($"\"Mốc thời gian: {report.CutoffDate:dd/MM/yyyy} | Đơn vị tính: {report.Unit}\"");
                await writer.WriteLineAsync();

                await writer.WriteLineAsync($"\"STT\",\"Số hợp đồng\",\"Tên hợp đồng\",\"Ngày ký\",\"Ngày kết thúc DK\",\"Giá trị HĐ ({report.Unit})\",\"Đã thanh toán ({report.Unit})\",\"Còn lại ({report.Unit})\",\"Dự kiến TT đến mốc ({report.Unit})\",\"Ghi chú\"");

                foreach (var r in report.Rows)
                {
                    string ngayKy = r.NgayKyHopDong.HasValue ? r.NgayKyHopDong.Value.ToString("dd/MM/yyyy") : "-";
                    string ngayKt = r.NgayKetThucDuKien.HasValue ? r.NgayKetThucDuKien.Value.ToString("dd/MM/yyyy") : "-";
                    await writer.WriteLineAsync($"\"{r.Stt}\",\"{EscapeCsvField(r.SoHopDong)}\",\"{EscapeCsvField(r.TenHopDong)}\",\"{ngayKy}\",\"{ngayKt}\",\"{r.GiaTriHopDong}\",\"{r.GiaTriDaThanhToan}\",\"{r.GiaTriConLai}\",\"{r.DuKienThanhToanDenMoc}\",\"{EscapeCsvField(r.GhiChu ?? "")}\"");
                }

                await writer.FlushAsync();
            }
            return memoryStream.ToArray();
        }
    }

    public async Task<byte[]> ExportTheoDoiHopDongReportHtmlAsync(int? year, DateTime? cutoffDate, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null)
    {
        var report = await GetTheoDoiHopDongReportAsync(year, cutoffDate, loaiHopDongIds, search, donViTinh);

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
        htmlBuilder.AppendLine("</style>");
        htmlBuilder.AppendLine("</head>");
        htmlBuilder.AppendLine("<body>");

        htmlBuilder.AppendLine($"<div class=\"title\">{System.Web.HttpUtility.HtmlEncode(report.Title)}</div>");
        htmlBuilder.AppendLine($"<div class=\"subtitle\">Mốc thời gian: {report.CutoffDate:dd/MM/yyyy} | Đơn vị tính: {System.Web.HttpUtility.HtmlEncode(report.Unit)}</div>");

        htmlBuilder.AppendLine("<table>");
        htmlBuilder.AppendLine("  <thead>");
        htmlBuilder.AppendLine("    <tr>");
        htmlBuilder.AppendLine("      <th>STT</th>");
        htmlBuilder.AppendLine("      <th>Số HĐ</th>");
        htmlBuilder.AppendLine("      <th>Tên Hợp đồng</th>");
        htmlBuilder.AppendLine("      <th>Ngày ký</th>");
        htmlBuilder.AppendLine("      <th>Ngày kết thúc DK</th>");
        htmlBuilder.AppendLine("      <th>Giá trị HĐ</th>");
        htmlBuilder.AppendLine("      <th>Đã thanh toán</th>");
        htmlBuilder.AppendLine("      <th>Còn lại</th>");
        htmlBuilder.AppendLine("      <th>Dự kiến TT</th>");
        htmlBuilder.AppendLine("      <th>Ghi chú</th>");
        htmlBuilder.AppendLine("    </tr>");
        htmlBuilder.AppendLine("  </thead>");
        htmlBuilder.AppendLine("  <tbody>");

        foreach (var r in report.Rows)
        {
            string ngayKy = r.NgayKyHopDong.HasValue ? r.NgayKyHopDong.Value.ToString("dd/MM/yyyy") : "-";
            string ngayKt = r.NgayKetThucDuKien.HasValue ? r.NgayKetThucDuKien.Value.ToString("dd/MM/yyyy") : "-";
            htmlBuilder.AppendLine("    <tr>");
            htmlBuilder.AppendLine($"      <td class=\"text-center\">{r.Stt}</td>");
            htmlBuilder.AppendLine($"      <td>{System.Web.HttpUtility.HtmlEncode(r.SoHopDong)}</td>");
            htmlBuilder.AppendLine($"      <td>{System.Web.HttpUtility.HtmlEncode(r.TenHopDong)}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-center\">{ngayKy}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-center\">{ngayKt}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-right\">{r.GiaTriHopDong:#,##0.##}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-right\">{r.GiaTriDaThanhToan:#,##0.##}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-right\">{r.GiaTriConLai:#,##0.##}</td>");
            htmlBuilder.AppendLine($"      <td class=\"text-right\">{r.DuKienThanhToanDenMoc:#,##0.##}</td>");
            htmlBuilder.AppendLine($"      <td>{System.Web.HttpUtility.HtmlEncode(r.GhiChu ?? "")}</td>");
            htmlBuilder.AppendLine("    </tr>");
        }

        htmlBuilder.AppendLine("  </tbody>");
        htmlBuilder.AppendLine("</table>");
        htmlBuilder.AppendLine("</body>");
        htmlBuilder.AppendLine("</html>");

        return System.Text.Encoding.UTF8.GetBytes(htmlBuilder.ToString());
    }

    #region 5. Báo cáo Kế hoạch vốn Đầu tư & Mua sắm (Biên bản họp đại diện vốn)

    private static (string Code, string Name) ClassifyNhomKyThuat(Entity.DuAn proj, int phuLucType)
    {
        if (phuLucType != 4 && phuLucType != 5)
        {
            return ("NHOM_CHUNG", "Danh mục dự án");
        }

        string text = $"{proj.Name} {proj.NoiDung} {proj.NhomDuAn?.Name} {proj.PhanLoaiDuAn?.Name}".ToLower();

        if (text.Contains("hạ tầng") || text.Contains("máy chủ") || text.Contains("lưu trữ") || text.Contains("backup") || text.Contains("server") || text.Contains("datacenter") || text.Contains("data center") || text.Contains("trang bị"))
        {
            return ("NHOM_I", "Nhóm I: Hạ tầng CNTT");
        }
        if (text.Contains("mạng") || text.Contains("bảo mật") || text.Contains("firewall") || text.Contains("truyền dẫn") || text.Contains("security") || text.Contains("an toàn thông tin"))
        {
            return ("NHOM_II", "Nhóm II: Mạng & Bảo mật");
        }
        if (text.Contains("phần mềm") || text.Contains("bản quyền") || text.Contains("giải pháp") || text.Contains("ứng dụng") || text.Contains("core") || text.Contains("thẻ") || text.Contains("ngân hàng số") || text.Contains("software"))
        {
            return ("NHOM_III", "Nhóm III: Phần mềm");
        }

        return ("NHOM_IV", "Nhóm IV: Mua sắm & Thiết bị khác");
    }

    public async Task<KeHoachVonReportResponseDto> GetKeHoachVonReportAsync(int? year, int? phuLuc, string? donViTinh = null)
    {
        int selectedYear = year ?? DateTime.Now.Year;
        var (factor, unitName) = ParseUnit(donViTinh ?? "triệu");

        var response = new KeHoachVonReportResponseDto
        {
            Title = $"KẾ HOẠCH ĐẦU TƯ & MUA SẮM NĂM {selectedYear}",
            Year = selectedYear,
            Unit = unitName,
            PhuLucs = new List<KeHoachVonReportPhuLucDto>()
        };

        var projects = await _context.DuAns
            .AsNoTracking()
            .Include(d => d.NhomDuAn)
            .Include(d => d.PhanLoaiDuAn)
            .Where(d => d.IsActive && !d.IsDeleted && (d.NamBatDau == null || d.NamBatDau <= selectedYear))
            .ToListAsync();

        var phuLucTypes = new List<(int Type, string Name)>
        {
            (1, "Phụ lục 01: Kế hoạch đầu tư xây dựng cơ bản"),
            (2, "Phụ lục 02: Kế hoạch mua sắm ô tô"),
            (3, "Phụ lục 03: Kế hoạch mua sắm tài sản cố định, công cụ lao động"),
            (4, "Phụ lục 04: Kế hoạch đầu tư nâng cấp, mua sắm TSCĐ lĩnh vực CNTT"),
            (5, "Phụ lục 05: Kế hoạch đầu tư nâng cấp, mua sắm TSCĐ lĩnh vực Thẻ & Ngân hàng số"),
            (6, "Phụ biểu 01: Danh sách các dự án đã duyệt kế hoạch vốn đang triển khai")
        };

        if (phuLuc.HasValue && phuLuc.Value >= 1 && phuLuc.Value <= 6)
        {
            phuLucTypes = phuLucTypes.Where(p => p.Type == phuLuc.Value).ToList();
        }

        foreach (var pType in phuLucTypes)
        {
            var plDto = new KeHoachVonReportPhuLucDto
            {
                PhuLucType = pType.Type,
                TenPhuLuc = pType.Name,
                Rows = new List<KeHoachVonReportRowDto>(),
                NhomKyThuats = new List<KeHoachVonReportNhomKyThuatDto>()
            };

            IEnumerable<Entity.DuAn> filteredProj = pType.Type switch
            {
                1 => projects.Where(x => x.PhanLoaiDuAn?.Code?.Contains("XDCB") == true || x.NoiDung?.Contains("xây dựng") == true || x.NhomDuAn?.Code == "XDCB"),
                2 => projects.Where(x => x.NoiDung?.Contains("ô tô") == true || x.NoiDung?.Contains("xe") == true),
                3 => projects.Where(x => x.NhomDuAn?.Code == "TSCD" || x.NoiDung?.Contains("máy photo") == true || x.NoiDung?.Contains("điều hòa") == true),
                4 => projects.Where(x => x.NhomDuAn?.Code == "CNTT" || x.PhanLoaiDuAn?.Code?.Contains("CNTT") == true || (x.NoiDung?.Contains("CNTT") == true && x.NoiDung.Contains("Thẻ") == false)),
                5 => projects.Where(x => x.NoiDung?.Contains("Thẻ") == true || x.NoiDung?.Contains("Ngân hàng số") == true),
                6 => projects.Where(x => x.DaTrienKhai == true || x.TrangThai == 2),
                _ => projects
            };

            int stt = 1;
            foreach (var proj in filteredProj)
            {
                decimal duToan = proj.DuToanPheDuyet / factor;
                var (nhomCode, nhomTen) = ClassifyNhomKyThuat(proj, pType.Type);

                var row = new KeHoachVonReportRowDto
                {
                    Stt = stt++,
                    DuAnId = proj.Id,
                    DonViChiNhanh = proj.ChuDauTu ?? "Trụ sở chính",
                    TenDuAn = proj.Name,
                    QuyMoXaydung = proj.NoiDung,
                    SuCanThiet = proj.ThoiGianThucHien,
                    HangMucCongViec = proj.ToChucThucHien,
                    VonDieuLeVaQuyDuTru = duToan * 0.6m,
                    QuyPhucLoi = duToan * 0.4m,
                    QuyDauTuPhatTrien = 0,
                    NguonKhac = 0,
                    TongDeXuatPheDuyet = duToan,
                    GhiChu = proj.SoQuyetDinh,
                    SoQuyetDinhNghiQuyet = proj.SoQuyetDinh,
                    PhuLucType = pType.Type,
                    NhomKyThuatCode = nhomCode,
                    TenNhomKyThuat = nhomTen
                };
                plDto.Rows.Add(row);
            }

            // Phân nhóm kỹ thuật (Nhóm I, Nhóm II, Nhóm III)
            var groupedNhom = plDto.Rows
                .GroupBy(r => (r.NhomKyThuatCode, r.TenNhomKyThuat))
                .OrderBy(g => g.Key.NhomKyThuatCode)
                .ToList();

            foreach (var g in groupedNhom)
            {
                var nhomDto = new KeHoachVonReportNhomKyThuatDto
                {
                    NhomKyThuatCode = g.Key.NhomKyThuatCode ?? "NHOM_CHUNG",
                    TenNhomKyThuat = g.Key.TenNhomKyThuat ?? "Danh mục dự án",
                    Rows = g.ToList(),
                    TongVonDieuLeVaQuyDuTru = g.Sum(r => r.VonDieuLeVaQuyDuTru),
                    TongQuyPhucLoi = g.Sum(r => r.QuyPhucLoi),
                    TongQuyDauTuPhatTrien = g.Sum(r => r.QuyDauTuPhatTrien),
                    TongNguonKhac = g.Sum(r => r.NguonKhac),
                    TongCongDeXuat = g.Sum(r => r.TongDeXuatPheDuyet)
                };
                plDto.NhomKyThuats.Add(nhomDto);
            }

            plDto.TongVonDieuLeVaQuyDuTru = plDto.Rows.Sum(r => r.VonDieuLeVaQuyDuTru);
            plDto.TongQuyPhucLoi = plDto.Rows.Sum(r => r.QuyPhucLoi);
            plDto.TongQuyDauTuPhatTrien = plDto.Rows.Sum(r => r.QuyDauTuPhatTrien);
            plDto.TongNguonKhac = plDto.Rows.Sum(r => r.NguonKhac);
            plDto.TongCongDeXuat = plDto.Rows.Sum(r => r.TongDeXuatPheDuyet);

            response.PhuLucs.Add(plDto);
        }

        response.TongCacPhuLuc = response.PhuLucs.Sum(p => p.TongCongDeXuat);
        return response;
    }

    public async Task<byte[]> ExportKeHoachVonReportExcelAsync(int? year, int? phuLuc, string? donViTinh = null)
    {
        var report = await GetKeHoachVonReportAsync(year, phuLuc, donViTinh);

        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            foreach (var pl in report.PhuLucs)
            {
                string sheetName = pl.PhuLucType == 6 ? "Phụ biểu 01" : $"Phụ lục 0{pl.PhuLucType}";
                var worksheet = workbook.Worksheets.Add(sheetName);

                worksheet.Cell("A1").Value = pl.TenPhuLuc.ToUpper();
                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Font.FontSize = 14;

                worksheet.Cell("A2").Value = $"Đơn vị tính: {report.Unit}";
                worksheet.Cell("A2").Style.Font.Italic = true;

                int row = 4;
                worksheet.Cell(row, 1).Value = "STT";
                worksheet.Cell(row, 2).Value = "Đơn vị / Chi nhánh";
                worksheet.Cell(row, 3).Value = "Tên dự án / Công trình";
                worksheet.Cell(row, 4).Value = "Quy mô / Hạng mục";
                worksheet.Cell(row, 5).Value = "Vốn điều lệ & Quỹ dự trữ";
                worksheet.Cell(row, 6).Value = "Quỹ phúc lợi / Khác";
                worksheet.Cell(row, 7).Value = "Tổng đề xuất phê duyệt";
                worksheet.Cell(row, 8).Value = "Ghi chú";

                var headerRange = worksheet.Range(row, 1, row, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");

                row++;
                if (pl.NhomKyThuats.Count > 1 || (pl.NhomKyThuats.Count == 1 && pl.NhomKyThuats[0].NhomKyThuatCode != "NHOM_CHUNG"))
                {
                    foreach (var nhom in pl.NhomKyThuats)
                    {
                        // Row Header Nhóm Kỹ Thuật
                        worksheet.Cell(row, 1).Value = nhom.TenNhomKyThuat;
                        var groupHeaderRange = worksheet.Range(row, 1, row, 8);
                        groupHeaderRange.Merge();
                        groupHeaderRange.Style.Font.Bold = true;
                        groupHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E6F0FA");
                        row++;

                        foreach (var r in nhom.Rows)
                        {
                            worksheet.Cell(row, 1).Value = r.Stt;
                            worksheet.Cell(row, 2).Value = r.DonViChiNhanh;
                            worksheet.Cell(row, 3).Value = r.TenDuAn;
                            worksheet.Cell(row, 4).Value = r.QuyMoXaydung ?? r.HangMucCongViec ?? "-";
                            worksheet.Cell(row, 5).Value = r.VonDieuLeVaQuyDuTru;
                            worksheet.Cell(row, 6).Value = r.QuyPhucLoi + r.QuyDauTuPhatTrien + r.NguonKhac;
                            worksheet.Cell(row, 7).Value = r.TongDeXuatPheDuyet;
                            worksheet.Cell(row, 8).Value = r.GhiChu ?? "";

                            worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                            worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
                            row++;
                        }

                        // Row Subtotal Nhóm Kỹ Thuật
                        worksheet.Cell(row, 3).Value = $"CỘNG {nhom.TenNhomKyThuat.ToUpper()}";
                        worksheet.Cell(row, 5).Value = nhom.TongVonDieuLeVaQuyDuTru;
                        worksheet.Cell(row, 6).Value = nhom.TongQuyPhucLoi + nhom.TongQuyDauTuPhatTrien + nhom.TongNguonKhac;
                        worksheet.Cell(row, 7).Value = nhom.TongCongDeXuat;

                        var groupSubtotalRange = worksheet.Range(row, 1, row, 8);
                        groupSubtotalRange.Style.Font.Bold = true;
                        groupSubtotalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F9F9F9");
                        worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                        worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                        worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
                        row++;
                    }
                }
                else
                {
                    foreach (var r in pl.Rows)
                    {
                        worksheet.Cell(row, 1).Value = r.Stt;
                        worksheet.Cell(row, 2).Value = r.DonViChiNhanh;
                        worksheet.Cell(row, 3).Value = r.TenDuAn;
                        worksheet.Cell(row, 4).Value = r.QuyMoXaydung ?? r.HangMucCongViec ?? "-";
                        worksheet.Cell(row, 5).Value = r.VonDieuLeVaQuyDuTru;
                        worksheet.Cell(row, 6).Value = r.QuyPhucLoi + r.QuyDauTuPhatTrien + r.NguonKhac;
                        worksheet.Cell(row, 7).Value = r.TongDeXuatPheDuyet;
                        worksheet.Cell(row, 8).Value = r.GhiChu ?? "";

                        worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                        worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                        worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
                        row++;
                    }
                }

                // Row Grand Total Phụ lục
                worksheet.Cell(row, 3).Value = "TỔNG CỘNG";
                worksheet.Cell(row, 5).Value = pl.TongVonDieuLeVaQuyDuTru;
                worksheet.Cell(row, 6).Value = pl.TongQuyPhucLoi + pl.TongQuyDauTuPhatTrien + pl.TongNguonKhac;
                worksheet.Cell(row, 7).Value = pl.TongCongDeXuat;

                var grandTotalRange = worksheet.Range(row, 1, row, 8);
                grandTotalRange.Style.Font.Bold = true;
                grandTotalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E0E0E0");
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";

                worksheet.Columns().AdjustToContents();
            }

            using (var ms = new MemoryStream())
            {
                workbook.SaveAs(ms);
                return ms.ToArray();
            }
        }
    }

    public async Task<byte[]> ExportKeHoachVonReportCsvAsync(int? year, int? phuLuc, string? donViTinh = null)
    {
        var report = await GetKeHoachVonReportAsync(year, phuLuc, donViTinh);
        using (var ms = new MemoryStream())
        {
            using (var writer = new StreamWriter(ms, System.Text.Encoding.UTF8))
            {
                writer.Write('\uFEFF');
                await writer.WriteLineAsync($"\"{report.Title}\"");
                await writer.WriteLineAsync($"\"Đơn vị tính: {report.Unit}\"");
                await writer.WriteLineAsync();

                foreach (var pl in report.PhuLucs)
                {
                    await writer.WriteLineAsync($"\"{pl.TenPhuLuc}\"");
                    await writer.WriteLineAsync($"\"STT\",\"Đơn vị\",\"Tên dự án\",\"Quy mô\",\"Vốn ĐL & Quỹ DT\",\"Quỹ phúc lợi/Khác\",\"Tổng đề xuất\",\"Ghi chú\"");
                    foreach (var r in pl.Rows)
                    {
                        await writer.WriteLineAsync($"\"{r.Stt}\",\"{EscapeCsvField(r.DonViChiNhanh ?? "")}\",\"{EscapeCsvField(r.TenDuAn)}\",\"{EscapeCsvField(r.QuyMoXaydung ?? "-")}\",\"{r.VonDieuLeVaQuyDuTru}\",\"{r.QuyPhucLoi}\",\"{r.TongDeXuatPheDuyet}\",\"{EscapeCsvField(r.GhiChu ?? "")}\"");
                    }
                    await writer.WriteLineAsync();
                }
                await writer.FlushAsync();
            }
            return ms.ToArray();
        }
    }

    public async Task<byte[]> ExportKeHoachVonReportHtmlAsync(int? year, int? phuLuc, string? donViTinh = null)
    {
        var report = await GetKeHoachVonReportAsync(year, phuLuc, donViTinh);
        var html = new System.Text.StringBuilder();
        html.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\" /><style>body{font-family:serif;margin:20px;} table{width:100%;border-collapse:collapse;} th,td{border:1px solid #ccc;padding:6px;} th{background:#f0f0f0;}</style></head><body>");
        html.AppendLine($"<h2>{System.Web.HttpUtility.HtmlEncode(report.Title)}</h2>");
        html.AppendLine($"<p><i>Đơn vị tính: {System.Web.HttpUtility.HtmlEncode(report.Unit)}</i></p>");

        foreach (var pl in report.PhuLucs)
        {
            html.AppendLine($"<h3>{System.Web.HttpUtility.HtmlEncode(pl.TenPhuLuc)}</h3>");
            html.AppendLine("<table><thead><tr><th>STT</th><th>Đơn vị</th><th>Tên dự án</th><th>Quy mô</th><th>Tổng đề xuất</th><th>Ghi chú</th></tr></thead><tbody>");
            foreach (var r in pl.Rows)
            {
                html.AppendLine($"<tr><td>{r.Stt}</td><td>{System.Web.HttpUtility.HtmlEncode(r.DonViChiNhanh ?? "")}</td><td>{System.Web.HttpUtility.HtmlEncode(r.TenDuAn)}</td><td>{System.Web.HttpUtility.HtmlEncode(r.QuyMoXaydung ?? "-")}</td><td>{r.TongDeXuatPheDuyet:#,##0.##}</td><td>{System.Web.HttpUtility.HtmlEncode(r.GhiChu ?? "")}</td></tr>");
            }
            html.AppendLine("</tbody></table>");
        }
        html.AppendLine("</body></html>");
        return System.Text.Encoding.UTF8.GetBytes(html.ToString());
    }

    #endregion

    #region 6. Báo cáo Tổng hợp & Phân kỳ Vốn Đầu tư CNTT Giai đoạn (Nghị quyết 16-NQ-NHHT)

    public async Task<KeHoachVonCnttReportResponseDto> GetKeHoachVonCnttReportAsync(int? fromYear, int? toYear, int? groupStatus, string? donViTinh = null)
    {
        int endY = toYear ?? DateTime.Now.Year;
        int startY = fromYear ?? (endY - 2);
        var (factor, unitName) = ParseUnit(donViTinh ?? "1");

        var response = new KeHoachVonCnttReportResponseDto
        {
            Title = $"TỔNG HỢP KẾ HOẠCH VỐN ĐẦU TƯ CNTT GIAI ĐOẠN {startY}-{endY}",
            FromYear = startY,
            ToYear = endY,
            Unit = unitName,
            Groups = new List<KeHoachVonCnttReportGroupDto>()
        };

        var projects = await _context.DuAns
            .AsNoTracking()
            .Include(d => d.NhomDuAn)
            .Where(d => d.IsActive && !d.IsDeleted)
            .ToListAsync();

        var groupTypes = new List<(int Status, string Name)>
        {
            (1, "I. Các dự án đã được phê duyệt, bố trí vốn đang thực hiện triển khai"),
            (2, "II. Các dự án đầu tư mới")
        };

        if (groupStatus.HasValue && (groupStatus.Value == 1 || groupStatus.Value == 2))
        {
            groupTypes = groupTypes.Where(g => g.Status == groupStatus.Value).ToList();
        }

        foreach (var gType in groupTypes)
        {
            var gDto = new KeHoachVonCnttReportGroupDto
            {
                NhomTrangThai = gType.Status,
                TenNhom = gType.Name,
                Rows = new List<KeHoachVonCnttReportRowDto>()
            };

            var filteredProj = gType.Status == 1
                ? projects.Where(p => p.DaTrienKhai == true || p.TrangThai == 2)
                : projects.Where(p => p.DaTrienKhai != true && p.TrangThai != 2);

            int stt = 1;
            foreach (var proj in filteredProj)
            {
                decimal totalInvestment = proj.DuToanPheDuyet / factor;
                var row = new KeHoachVonCnttReportRowDto
                {
                    Stt = stt++,
                    DuAnId = proj.Id,
                    NoiDung = proj.Name,
                    TongMucDauTu = totalInvestment,
                    VonTuCo = totalInvestment * 0.3m,
                    QuyDauTuPhatTrien = totalInvestment * 0.7m,
                    NguonKhac = 0,
                    TrangThaiText = proj.DaTrienKhai == true ? "Đang triển khai" : "Đã phê duyệt chủ trương",
                    DonViDeXuatChiDao = proj.ChuDauTu ?? "Trung tâm CNTT",
                    GhiChu = proj.SoQuyetDinh,
                    NhomTrangThai = gType.Status,
                    PhanKyDauTu = new List<KeHoachVonCnttPhanKyDto>()
                };

                int numYears = (endY - startY + 1);
                decimal yearlyVal = totalInvestment / (numYears > 0 ? numYears : 1);
                for (int y = startY; y <= endY; y++)
                {
                    row.PhanKyDauTu.Add(new KeHoachVonCnttPhanKyDto { Nam = y, GiaTri = yearlyVal });
                }

                gDto.Rows.Add(row);
            }

            gDto.TongMucDauTuNhom = gDto.Rows.Sum(r => r.TongMucDauTu);
            gDto.TongVonTuCoNhom = gDto.Rows.Sum(r => r.VonTuCo);
            gDto.TongQuyDauTuPhatTrienNhom = gDto.Rows.Sum(r => r.QuyDauTuPhatTrien);
            gDto.TongNguonKhacNhom = gDto.Rows.Sum(r => r.NguonKhac);

            for (int y = startY; y <= endY; y++)
            {
                gDto.TongPhanKyNhom[y] = gDto.Rows.Sum(r => r.PhanKyDauTu.FirstOrDefault(pk => pk.Nam == y)?.GiaTri ?? 0);
            }

            response.Groups.Add(gDto);
        }

        response.TongCongMucDauTu = response.Groups.Sum(g => g.TongMucDauTuNhom);
        response.TongCongVonTuCo = response.Groups.Sum(g => g.TongVonTuCoNhom);
        response.TongCongQuyDauTuPhatTrien = response.Groups.Sum(g => g.TongQuyDauTuPhatTrienNhom);
        response.TongCongNguonKhac = response.Groups.Sum(g => g.TongNguonKhacNhom);

        for (int y = startY; y <= endY; y++)
        {
            response.TongCongPhanKy[y] = response.Groups.Sum(g => g.TongPhanKyNhom.ContainsKey(y) ? g.TongPhanKyNhom[y] : 0);
        }

        return response;
    }

    public async Task<byte[]> ExportKeHoachVonCnttReportExcelAsync(int? fromYear, int? toYear, int? groupStatus, string? donViTinh = null)
    {
        var report = await GetKeHoachVonCnttReportAsync(fromYear, toYear, groupStatus, donViTinh);

        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Kế hoạch vốn CNTT");
            worksheet.Cell("A1").Value = report.Title;
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 14;

            worksheet.Cell("A2").Value = $"Đơn vị tính: {report.Unit}";
            worksheet.Cell("A2").Style.Font.Italic = true;

            int row = 4;
            worksheet.Cell(row, 1).Value = "STT";
            worksheet.Cell(row, 2).Value = "Nội dung";
            worksheet.Cell(row, 3).Value = "Tổng mức đầu tư";
            worksheet.Cell(row, 4).Value = "Vốn tự có";
            worksheet.Cell(row, 5).Value = "Quỹ ĐTPT";
            worksheet.Cell(row, 6).Value = "Trạng thái";
            worksheet.Cell(row, 7).Value = "Ghi chú";

            var headerRange = worksheet.Range(row, 1, row, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");

            row++;
            foreach (var g in report.Groups)
            {
                worksheet.Cell(row, 1).Value = g.TenNhom;
                worksheet.Range(row, 1, row, 7).Merge().Style.Font.Bold = true;
                row++;

                foreach (var r in g.Rows)
                {
                    worksheet.Cell(row, 1).Value = r.Stt;
                    worksheet.Cell(row, 2).Value = r.NoiDung;
                    worksheet.Cell(row, 3).Value = r.TongMucDauTu;
                    worksheet.Cell(row, 4).Value = r.VonTuCo;
                    worksheet.Cell(row, 5).Value = r.QuyDauTuPhatTrien;
                    worksheet.Cell(row, 6).Value = r.TrangThaiText ?? "-";
                    worksheet.Cell(row, 7).Value = r.GhiChu ?? "";

                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.##";
                    worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.##";
                    worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";

                    row++;
                }
            }

            worksheet.Columns().AdjustToContents();

            using (var ms = new MemoryStream())
            {
                workbook.SaveAs(ms);
                return ms.ToArray();
            }
        }
    }

    public async Task<byte[]> ExportKeHoachVonCnttReportCsvAsync(int? fromYear, int? toYear, int? groupStatus, string? donViTinh = null)
    {
        var report = await GetKeHoachVonCnttReportAsync(fromYear, toYear, groupStatus, donViTinh);
        using (var ms = new MemoryStream())
        {
            using (var writer = new StreamWriter(ms, System.Text.Encoding.UTF8))
            {
                writer.Write('\uFEFF');
                await writer.WriteLineAsync($"\"{report.Title}\"");
                await writer.WriteLineAsync($"\"Đơn vị tính: {report.Unit}\"");
                await writer.WriteLineAsync();

                foreach (var g in report.Groups)
                {
                    await writer.WriteLineAsync($"\"{g.TenNhom}\"");
                    await writer.WriteLineAsync($"\"STT\",\"Nội dung\",\"Tổng mức đầu tư\",\"Vốn tự có\",\"Quỹ ĐTPT\",\"Trạng thái\",\"Ghi chú\"");
                    foreach (var r in g.Rows)
                    {
                        await writer.WriteLineAsync($"\"{r.Stt}\",\"{EscapeCsvField(r.NoiDung)}\",\"{r.TongMucDauTu}\",\"{r.VonTuCo}\",\"{r.QuyDauTuPhatTrien}\",\"{EscapeCsvField(r.TrangThaiText ?? "-")}\",\"{EscapeCsvField(r.GhiChu ?? "")}\"");
                    }
                    await writer.WriteLineAsync();
                }
                await writer.FlushAsync();
            }
            return ms.ToArray();
        }
    }

    public async Task<byte[]> ExportKeHoachVonCnttReportHtmlAsync(int? fromYear, int? toYear, int? groupStatus, string? donViTinh = null)
    {
        var report = await GetKeHoachVonCnttReportAsync(fromYear, toYear, groupStatus, donViTinh);
        var html = new System.Text.StringBuilder();
        html.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\" /><style>body{font-family:serif;margin:20px;} table{width:100%;border-collapse:collapse;} th,td{border:1px solid #ccc;padding:6px;} th{background:#f0f0f0;}</style></head><body>");
        html.AppendLine($"<h2>{System.Web.HttpUtility.HtmlEncode(report.Title)}</h2>");
        html.AppendLine($"<p><i>Đơn vị tính: {System.Web.HttpUtility.HtmlEncode(report.Unit)}</i></p>");

        foreach (var g in report.Groups)
        {
            html.AppendLine($"<h3>{System.Web.HttpUtility.HtmlEncode(g.TenNhom)}</h3>");
            html.AppendLine("<table><thead><tr><th>STT</th><th>Nội dung</th><th>Tổng mức đầu tư</th><th>Vốn tự có</th><th>Quỹ ĐTPT</th><th>Trạng thái</th><th>Ghi chú</th></tr></thead><tbody>");
            foreach (var r in g.Rows)
            {
                html.AppendLine($"<tr><td>{r.Stt}</td><td>{System.Web.HttpUtility.HtmlEncode(r.NoiDung)}</td><td>{r.TongMucDauTu:#,##0.##}</td><td>{r.VonTuCo:#,##0.##}</td><td>{r.QuyDauTuPhatTrien:#,##0.##}</td><td>{System.Web.HttpUtility.HtmlEncode(r.TrangThaiText ?? "-")}</td><td>{System.Web.HttpUtility.HtmlEncode(r.GhiChu ?? "")}</td></tr>");
            }
            html.AppendLine("</tbody></table>");
        }
        html.AppendLine("</body></html>");
        return System.Text.Encoding.UTF8.GetBytes(html.ToString());
    }

    #endregion
}

