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
    public async Task<ContractPaymentReportResponseDto> GetContractPaymentReportAsync(int year, int? loaiHopDong, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null)
    {
        var (factor, unitName) = ParseUnit(donViTinh);

        var query = _context.HopDongs
            .AsNoTracking()
            .Include(h => h.DotThanhToans)
            .Include(h => h.DuAn)
            .Include(h => h.GoiThau)
            .Include(h => h.NhaThau)
            .Where(h => h.IsActive && !h.IsDeleted && (h.DuAn == null || h.DuAn.TrangThai != 10));

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

        var startOfYear = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfYear = new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        query = query.Where(h =>
            (h.NgayKy.HasValue ? h.NgayKy.Value.Year == year : (h.NgayHieuLuc.HasValue ? h.NgayHieuLuc.Value.Year == year : h.CreatedAt.Year == year)) ||
            (h.DotThanhToans.Any(d => (d.NgayThanhToan.HasValue && d.NgayThanhToan.Value.Year == year) ||
                                      (d.NgayThanhToanThucTe.HasValue && d.NgayThanhToanThucTe.Value.Year == year))) ||
            ((h.NgayKy ?? h.NgayHieuLuc ?? h.CreatedAt) <= endOfYear &&
             (!h.ExpiredDate.HasValue && !h.NgayKetThucThucTe.HasValue ||
              (h.NgayKetThucThucTe.HasValue ? h.NgayKetThucThucTe.Value >= startOfYear : h.ExpiredDate!.Value >= startOfYear)))
        );

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
                (h.SoHopDong != null && h.SoHopDong.ToLower().Contains(searchLower)) ||
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
            decimal rawDaThanhToanTrongNam = milestones
                .Where(m => m.IsPaid && ((m.NgayThanhToanThucTe ?? m.NgayThanhToan)?.Year == year))
                .Sum(m => m.GiaTriThanhToan);
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
            else if (contract.ExpiredDate.HasValue && contract.ExpiredDate.Value < DateTime.UtcNow && soKyConPhaiThanhToan > 0 && !contract.DaKetThuc && !contract.NgayKetThucThucTe.HasValue)
            {
                trangThaiThanhToan = "Quá hạn";
            }
            else
            {
                trangThaiThanhToan = "Chưa thanh toán";
            }

            string loaiHopDongTen = GetLoaiHopDongName(contract.LoaiHopDong);

            decimal cumulativePaid = 0m;
            var milestoneDtos = new List<ContractPaymentReportMilestoneDto>();
            foreach (var m in milestones)
            {
                if (m.IsPaid)
                {
                    cumulativePaid += m.GiaTriThanhToan;
                }
                decimal remainingForMilestone = Math.Max(0, (contract.GiaTriHopDong - cumulativePaid) / factor);

                milestoneDtos.Add(new ContractPaymentReportMilestoneDto
                {
                    Id = m.Id,
                    TenDot = m.TenDot,
                    TyLeThanhToan = m.TyLeThanhToan,
                    GiaTriThanhToan = m.GiaTriThanhToan / factor,
                    NgayThanhToan = m.NgayThanhToan,
                    NgayThanhToanThucTe = m.NgayThanhToanThucTe,
                    DieuKienThanhToan = m.DieuKienThanhToan,
                    GhiChuThanhToan = m.GhiChuThanhToan,
                    IsPaid = m.IsPaid,
                    TinhTrangHoSoNghiemThu = !string.IsNullOrWhiteSpace(m.GhiChuThanhToan)
                        ? m.GhiChuThanhToan
                        : (m.IsPaid ? "Đã nhận hồ sơ & hoàn thành nghiệm thu" : "Chờ hoàn thiện hồ sơ nghiệm thu"),
                    GiaTriHopDongConLaiChuaTra = remainingForMilestone
                });
            }

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
                NgayKetThucThucTe = contract.NgayKetThucThucTe,
                DaKetThuc = contract.DaKetThuc,
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
                TinhTrangHoSoNghiemThu = soKyDaThanhToan == tongSoKy ? "Đã hoàn tất toàn bộ nghiệm thu" : $"{soKyDaThanhToan}/{tongSoKy} đợt đã nghiệm thu",
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

        var flatMilestones = new List<ContractPaymentFlatMilestoneDto>();
        int flatStt = 1;
        foreach (var r in rows)
        {
            foreach (var m in r.DanhSachDotThanhToan)
            {
                flatMilestones.Add(new ContractPaymentFlatMilestoneDto
                {
                    Stt = flatStt++,
                    DotThanhToanId = m.Id,
                    TenDotThanhToan = m.TenDot,
                    DieuKienThanhToan = m.DieuKienThanhToan,
                    MaHopDong = r.MaHopDong,
                    TenHopDong = r.TenHopDong,
                    DuAnGoiThau = !string.IsNullOrWhiteSpace(r.TenDuAn) && !string.IsNullOrWhiteSpace(r.TenGoiThau)
                        ? $"{r.TenDuAn} / {r.TenGoiThau}"
                        : (!string.IsNullOrWhiteSpace(r.TenDuAn) ? r.TenDuAn : r.TenGoiThau),
                    TyLeThanhToan = m.TyLeThanhToan,
                    GiaTriThanhToan = m.GiaTriThanhToan,
                    HanThanhToan = m.NgayThanhToan,
                    NgayThanhToanThucTe = m.NgayThanhToanThucTe,
                    TinhTrangHoSoNghiemThu = m.TinhTrangHoSoNghiemThu,
                    TrangThaiThanhToan = m.IsPaid ? "🟢 Đã thanh toán" : "🟡 Chưa thanh toán",
                    GiaTriHopDongConLaiChuaTra = m.GiaTriHopDongConLaiChuaTra
                });
            }
        }

        return new ContractPaymentReportResponseDto
        {
            Title = $"BÁO CÁO THEO DÕI THANH TOÁN HỢP ĐỒNG NĂM {year}",
            Unit = unitName,
            Year = year,
            LoaiHopDong = loaiHopDong,
            LoaiHopDongFilterTen = loaiFilterName,
            Summary = summary,
            Rows = rows,
            FlatMilestones = flatMilestones
        };
    }



    public async Task<byte[]> ExportContractPaymentReportExcelAsync(int year, int? loaiHopDong, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null, int version = 1)
    {
        if (version == 2)
        {
            return await ExportBaoCao5DotThanhToanV2Async(year, loaiHopDong, loaiHopDongIds, search, donViTinh);
        }

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



    private async Task<byte[]> ExportBaoCao5DotThanhToanV2Async(
        int year,
        int? loaiHopDong,
        List<Guid>? loaiHopDongIds,
        string? search,
        string? donViTinh)
    {
        var report = await GetContractPaymentReportAsync(year, loaiHopDong, loaiHopDongIds, search, donViTinh);
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Đợt thanh toán");
        worksheet.Style.Font.FontName = "Times New Roman";
        worksheet.Style.Font.FontSize = 11;

        // Title Row 1
        worksheet.Cell("B1").Value = "BÁO CÁO ĐỢT THANH TOÁN & NGHIỆM THU";
        worksheet.Cell("B1").Style.Font.Bold = true;
        worksheet.Cell("B1").Style.Font.FontSize = 14;
        worksheet.Cell("B1").Style.Font.FontColor = XLColor.FromHtml("#1F4E78");
        worksheet.Cell("B1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range("B1:K1").Merge();

        // Subtitle Row 2
        worksheet.Cell("B2").Value = $"(Năm {year} - Đơn vị tính: {report.Unit})";
        worksheet.Cell("B2").Style.Font.Italic = true;
        worksheet.Cell("B2").Style.Font.FontSize = 10;
        worksheet.Cell("B2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range("B2:K2").Merge();

        // Header Row 3
        string[] headers = [
            "STT", "Tên đợt thanh toán", "Điều kiện thanh toán ",
            "Dự án / Gói thầu", "Tỷ lệ (%)", $"Giá trị thanh toán ({report.Unit})",
            "Hạn thanh toán", "Tình trạng hồ sơ nghiệm thu", "Trạng thái thanh toán", $"Giá trị HĐ còn lại chưa trả ({report.Unit})"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(3, 2 + i);
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

        int currentRow = 4;
        int stt = 1;

        if (report.FlatMilestones != null && report.FlatMilestones.Any())
        {
            foreach (var ms in report.FlatMilestones)
            {
                worksheet.Cell(currentRow, 2).Value = stt++;
                worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 3).Value = ms.TenDotThanhToan;
                worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                worksheet.Cell(currentRow, 4).Value = ms.DieuKienThanhToan ?? string.Empty;
                worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                worksheet.Cell(currentRow, 5).Value = ms.DuAnGoiThau ?? string.Empty;
                worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                worksheet.Cell(currentRow, 6).Value = ms.TyLeThanhToan / 100.0m;
                worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "0.0%";
                worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(currentRow, 7).Value = ms.GiaTriThanhToan;
                worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                DateTime? hanTt = ms.NgayThanhToanThucTe ?? ms.HanThanhToan;
                if (hanTt.HasValue)
                {
                    worksheet.Cell(currentRow, 8).Value = hanTt.Value;
                    worksheet.Cell(currentRow, 8).Style.DateFormat.Format = "dd/MM/yyyy";
                    worksheet.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else
                {
                    worksheet.Cell(currentRow, 8).Value = string.Empty;
                }

                worksheet.Cell(currentRow, 9).Value = ms.TinhTrangHoSoNghiemThu ?? string.Empty;
                worksheet.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 10).Value = ms.TrangThaiThanhToan;
                worksheet.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 11).Value = ms.GiaTriHopDongConLaiChuaTra;
                worksheet.Cell(currentRow, 11).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                var rowRange = worksheet.Range(currentRow, 2, currentRow, 11);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                currentRow++;
            }

            int startRow = 4;
            int endRow = currentRow - 1;
            worksheet.Cell(currentRow, 2).Value = "TỔNG CỘNG";
            worksheet.Range(currentRow, 2, currentRow, 6).Merge();
            worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 7).FormulaA1 = $"=SUM(G{startRow}:G{endRow})";
            worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 8).Value = string.Empty;
            worksheet.Cell(currentRow, 9).Value = string.Empty;
            worksheet.Cell(currentRow, 10).Value = string.Empty;

            worksheet.Cell(currentRow, 11).FormulaA1 = $"=SUM(K{startRow}:K{endRow})";
            worksheet.Cell(currentRow, 11).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var totalRowRange = worksheet.Range(currentRow, 2, currentRow, 11);
            totalRowRange.Style.Font.Bold = true;
            totalRowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
            totalRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            totalRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            currentRow++;
        }

        worksheet.Columns(2, 11).AdjustToContents(10.0, 50.0);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }


}
