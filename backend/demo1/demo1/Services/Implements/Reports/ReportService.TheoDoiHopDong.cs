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
    public async Task<TheoDoiHopDongReportResponseDto> GetTheoDoiHopDongReportAsync(int? year, DateTime? cutoffDate, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null)
    {
        var (factor, unitName) = ParseUnit(donViTinh);
        int selectedYear = year ?? DateTime.Now.Year;
        DateTime targetCutoffDate = cutoffDate ?? new DateTime(selectedYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);

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

        if (year.HasValue)
        {
            var startOfYear = new DateTime(year.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfYear = new DateTime(year.Value, 12, 31, 23, 59, 59, DateTimeKind.Utc);

            query = query.Where(h =>
                (h.NgayKy.HasValue ? h.NgayKy.Value.Year == year.Value : (h.NgayHieuLuc.HasValue ? h.NgayHieuLuc.Value.Year == year.Value : h.CreatedAt.Year == year.Value)) ||
                (h.DotThanhToans.Any(d => (d.NgayThanhToan.HasValue && d.NgayThanhToan.Value.Year == year.Value) ||
                                          (d.NgayThanhToanThucTe.HasValue && d.NgayThanhToanThucTe.Value.Year == year.Value))) ||
                ((h.NgayKy ?? h.NgayHieuLuc ?? h.CreatedAt) <= endOfYear &&
                 (!h.ExpiredDate.HasValue && !h.NgayKetThucThucTe.HasValue ||
                  (h.NgayKetThucThucTe.HasValue ? h.NgayKetThucThucTe.Value >= startOfYear : h.ExpiredDate!.Value >= startOfYear)))
            );
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
                NgayThanhToanThucTe = m.NgayThanhToanThucTe,
                DieuKienThanhToan = m.DieuKienThanhToan,
                GhiChuThanhToan = m.GhiChuThanhToan,
                IsPaid = m.IsPaid
            }).ToList();

            int soNgayConLai = contract.ExpiredDate.HasValue
                ? Math.Max(0, (contract.ExpiredDate.Value.Date - DateTime.UtcNow.Date).Days)
                : 0;

            string trangThaiThucHienText = contract.DaKetThuc
                ? "Đã hoàn thành"
                : (contract.ExpiredDate.HasValue && contract.ExpiredDate.Value.Date < DateTime.UtcNow.Date ? "Đã hết hạn" : "Đang thực hiện");

            string canhBaoHanhDong = contract.DaKetThuc
                ? "🟢 Đã thanh lý hoàn thành"
                : (contract.ExpiredDate.HasValue && contract.ExpiredDate.Value.Date < DateTime.UtcNow.Date
                    ? "🟡 Chờ ký Biên bản nghiệm thu"
                    : (soNgayConLai <= 30 && soNgayConLai > 0
                        ? "🟡 Sắp hết hạn - Chuẩn bị gia hạn"
                        : "🟢 Đang thực hiện bình thường"));

            string? nguoiDaiDienVaSdt = null;
            if (contract.NhaThau != null && !string.IsNullOrWhiteSpace(contract.NhaThau.Representative))
            {
                nguoiDaiDienVaSdt = contract.NhaThau.Representative;
            }

            rows.Add(new TheoDoiHopDongReportRowDto
            {
                Stt = stt++,
                HopDongId = contract.Id,
                SoHopDong = !string.IsNullOrWhiteSpace(contract.SoHopDong) ? contract.SoHopDong : contract.Code,
                TenHopDong = contract.Name,
                NgayKyHopDong = contract.NgayHieuLuc,
                NgayKetThucDuKien = contract.ExpiredDate,
                NgayKetThucThucTe = contract.NgayKetThucThucTe,
                DaKetThuc = contract.DaKetThuc,
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
                NguoiDaiDienVaSdt = nguoiDaiDienVaSdt,
                SoNgayConLai = soNgayConLai,
                TrangThaiThucHienText = trangThaiThucHienText,
                CanhBaoHanhDong = canhBaoHanhDong,
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

    public async Task<byte[]> ExportTheoDoiHopDongReportExcelAsync(int? year, DateTime? cutoffDate, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null, int version = 1)
    {
        if (version == 2)
        {
            return await ExportBaoCao4QuanLyHopDongV2Async(year, cutoffDate, loaiHopDongIds, search, donViTinh);
        }

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



    private async Task<byte[]> ExportBaoCao4QuanLyHopDongV2Async(
        int? year,
        DateTime? cutoffDate,
        List<Guid>? loaiHopDongIds,
        string? search,
        string? donViTinh)
    {
        var report = await GetTheoDoiHopDongReportAsync(year, cutoffDate, loaiHopDongIds, search, donViTinh);
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Quản lý Hợp đồng");
        worksheet.Style.Font.FontName = "Times New Roman";
        worksheet.Style.Font.FontSize = 11;

        // Title Row 2
        worksheet.Cell("B2").Value = "BÁO CÁO QUẢN LÝ HỢP ĐỒNG";
        worksheet.Cell("B2").Style.Font.Bold = true;
        worksheet.Cell("B2").Style.Font.FontSize = 14;
        worksheet.Cell("B2").Style.Font.FontColor = XLColor.FromHtml("#1F4E78");
        worksheet.Cell("B2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range("B2:N2").Merge();

        // Subtitle Row 3
        worksheet.Cell("B3").Value = $"(Đơn vị tính: {report.Unit})";
        worksheet.Cell("B3").Style.Font.Italic = true;
        worksheet.Cell("B3").Style.Font.FontSize = 10;
        worksheet.Cell("B3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range("B3:N3").Merge();

        // Header Row 4
        string[] headers = [
            "STT", "Số / Mã HĐ", "Tên Hợp đồng kinh tế", "Dự án triển khai liên kết",
            "Nhà thầu", "Người đại diện", $"Giá trị HĐ ({report.Unit})",
            $"Giá trị đã thanh toán lũy kế ({report.Unit})", $"Giá trị còn lại chưa thanh toán ({report.Unit})",
            "Ngày ký", "Ngày hết hạn", "Số ngày còn lại", "Trạng thái thực hiện"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(4, 2 + i);
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

        int currentRow = 5;
        int stt = 1;

        foreach (var row in report.Rows)
        {
            worksheet.Cell(currentRow, 2).Value = stt++;
            worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 3).Value = row.SoHopDong;
            worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 4).Value = row.TenHopDong;
            worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            worksheet.Cell(currentRow, 5).Value = row.TenDuAn ?? string.Empty;
            worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            worksheet.Cell(currentRow, 6).Value = row.TenNhaThau ?? string.Empty;
            worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            worksheet.Cell(currentRow, 7).Value = row.NguoiDaiDienVaSdt ?? string.Empty;
            worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            worksheet.Cell(currentRow, 8).Value = row.GiaTriHopDong;
            worksheet.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 9).Value = row.GiaTriDaThanhToan;
            worksheet.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 10).Value = row.GiaTriConLai;
            worksheet.Cell(currentRow, 10).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            if (row.NgayKyHopDong.HasValue)
            {
                worksheet.Cell(currentRow, 11).Value = row.NgayKyHopDong.Value;
                worksheet.Cell(currentRow, 11).Style.DateFormat.Format = "dd/MM/yyyy";
                worksheet.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            else
            {
                worksheet.Cell(currentRow, 11).Value = string.Empty;
            }

            if (row.NgayKetThucDuKien.HasValue)
            {
                worksheet.Cell(currentRow, 12).Value = row.NgayKetThucDuKien.Value;
                worksheet.Cell(currentRow, 12).Style.DateFormat.Format = "dd/MM/yyyy";
                worksheet.Cell(currentRow, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            else
            {
                worksheet.Cell(currentRow, 12).Value = string.Empty;
            }

            worksheet.Cell(currentRow, 13).Value = row.SoNgayConLai;
            worksheet.Cell(currentRow, 13).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 14).Value = row.TrangThaiThucHienText;
            worksheet.Cell(currentRow, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var rowRange = worksheet.Range(currentRow, 2, currentRow, 14);
            rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            currentRow++;
        }

        if (report.Rows.Count > 0)
        {
            int startRow = 5;
            int endRow = currentRow - 1;
            worksheet.Cell(currentRow, 2).Value = "TỔNG CỘNG";
            worksheet.Range(currentRow, 2, currentRow, 7).Merge();
            worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 8).FormulaA1 = $"=SUM(H{startRow}:H{endRow})";
            worksheet.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 9).FormulaA1 = $"=SUM(I{startRow}:I{endRow})";
            worksheet.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 10).FormulaA1 = $"=SUM(J{startRow}:J{endRow})";
            worksheet.Cell(currentRow, 10).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 11).Value = string.Empty;
            worksheet.Cell(currentRow, 12).Value = string.Empty;
            worksheet.Cell(currentRow, 13).Value = string.Empty;
            worksheet.Cell(currentRow, 14).Value = string.Empty;

            var totalRowRange = worksheet.Range(currentRow, 2, currentRow, 14);
            totalRowRange.Style.Font.Bold = true;
            totalRowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
            totalRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            totalRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            currentRow++;
        }

        worksheet.Columns(2, 14).AdjustToContents(10.0, 50.0);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }


}
