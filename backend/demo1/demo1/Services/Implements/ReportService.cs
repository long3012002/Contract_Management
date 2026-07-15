using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        // 1. Calculate reporting period dates
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

        // 2. Fetch all projects and related adjustments
        var allProjects = await _context.DuAns
            .Include(da => da.DieuChinhs)
            .Include(da => da.NhomDuAn)
            .Include(da => da.PhanLoaiDuAn)
            .Where(da => da.IsActive)
            .ToListAsync();

        // 3. Fetch all contracts and related payment stages
        var allContracts = await _context.HopDongs
            .Include(h => h.DotThanhToans)
            .Where(h => h.IsActive)
            .ToListAsync();

        // Helper lists to categorize projects
        var b_I = new List<ReportRowDto>();   // Group B - Construction
        var b_II = new List<ReportRowDto>();  // Group B - IT
        var b_III = new List<ReportRowDto>(); // Group B - Other

        var c_I = new List<ReportRowDto>();   // Group C - Construction
        var c_II = new List<ReportRowDto>();  // Group C - IT
        var c_III = new List<ReportRowDto>(); // Group C - Other

        int b_I_index = 1, b_II_index = 1, b_III_index = 1;
        int c_I_index = 1, c_II_index = 1, c_III_index = 1;

        foreach (var project in allProjects)
        {
            // Calculate total approved budget up to the end of the reporting period
            decimal totalBudgetVnd = 0;
            if (project.LoaiDuAn == 2 && !string.IsNullOrWhiteSpace(project.NguonDuAnIds))
            {
                var sourceIds = project.NguonDuAnIds.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                    .Where(g => g != Guid.Empty)
                    .ToList();

                if (sourceIds.Any())
                {
                    var sourceProjects = allProjects.Where(sp => sourceIds.Contains(sp.Id)).ToList();
                    foreach (var sp in sourceProjects)
                    {
                        var spAdjustments = sp.DieuChinhs?
                            .Where(dc => dc.NgayDieuChinh <= endOfPeriod)
                            .Sum(dc => dc.GiaTriDieuChinh) ?? 0;
                        totalBudgetVnd += (sp.DuToanPheDuyet + spAdjustments);
                    }
                }
                else
                {
                    var adjustments = project.DieuChinhs?
                        .Where(dc => dc.NgayDieuChinh <= endOfPeriod)
                        .Sum(dc => dc.GiaTriDieuChinh) ?? 0;
                    totalBudgetVnd = project.DuToanPheDuyet + adjustments;
                }
            }
            else
            {
                var adjustments = project.DieuChinhs?
                    .Where(dc => dc.NgayDieuChinh <= endOfPeriod)
                    .Sum(dc => dc.GiaTriDieuChinh) ?? 0;
                totalBudgetVnd = project.DuToanPheDuyet + adjustments;
            }

            // Calculate performed and disbursed values based on contract payment milestones
            var projectContracts = allContracts.Where(h => h.DuAnId == project.Id).ToList();
            decimal performedKyTruocVnd = 0;
            decimal performedTrongKyVnd = 0;

            foreach (var contract in projectContracts)
            {
                if (contract.DotThanhToans != null)
                {
                    foreach (var milestone in contract.DotThanhToans)
                    {
                        if (milestone.CreatedAt < startOfPeriod)
                        {
                            performedKyTruocVnd += milestone.GiaTriThanhToan;
                        }
                        else if (milestone.CreatedAt <= endOfPeriod)
                        {
                            performedTrongKyVnd += milestone.GiaTriThanhToan;
                        }
                    }
                }
            }

            decimal performedLuyKeVnd = performedKyTruocVnd + performedTrongKyVnd;

            // Value of completed assets put into use (Tai San Ban Giao)
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
                    isCompletedBeforeEnd = true; // Fallback to true if no dates exist but state is HoanThanh
                }

                if (isCompletedBeforeEnd)
                {
                    taiSanBanGiaoVnd = performedLuyKeVnd;
                }
            }

            // Convert all values from VND to Million VND
            decimal conversionFactor = 1_000_000m;
            decimal budgetTotal = totalBudgetVnd / conversionFactor;
            decimal budgetVcsh = budgetTotal; // 100% Owner's equity
            decimal budgetVay = 0;
            decimal budgetKhac = 0;

            decimal kLuongKyTruoc = performedKyTruocVnd / conversionFactor;
            decimal kLuongTrongKy = performedTrongKyVnd / conversionFactor;
            decimal kLuongLuyKe = performedLuyKeVnd / conversionFactor;

            decimal gNganKyTruoc = kLuongKyTruoc;
            decimal gNganTrongKy = kLuongTrongKy;
            decimal gNganLuyKe = kLuongLuyKe;

            decimal tsBanGiao = taiSanBanGiaoVnd / conversionFactor;

            // Determine Decision description
            string approvalDecision = project.SoQuyetDinh ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(approvalDecision) && !approvalDecision.Contains("ngày") && project.NgayBatDau.HasValue)
            {
                approvalDecision = $"{approvalDecision} ngày {project.NgayBatDau.Value.ToString("dd/MM/yyyy")} V/v phê duyệt dự án {project.Name}";
            }

            // Classify by Project Type
            string projType = "Khac";
            if (project.PhanLoaiDuAn != null)
            {
                var codeUpper = project.PhanLoaiDuAn.Code.ToUpper();
                var nameLower = project.PhanLoaiDuAn.Name.ToLower();
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
                // Fallback to name-based keyword search if no entity link exists
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

            // Classify by Project Group (Group B >= 45 billion VND, Group C < 45 billion VND)
            bool isGroupB = project.NhomDuAn?.Code?.Equals("NHOM_B", StringComparison.OrdinalIgnoreCase) == true || 
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

        // 4. Assemble hierarchical rows
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
}
