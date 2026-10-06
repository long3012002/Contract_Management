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
    public async Task<CongViecGoiThauReportDto> GetCongViecGoiThauReportAsync(Guid idGoiThau, string? donViTinh = null)
    {
        var (factor, unitName) = ParseUnit(donViTinh);

        var goiThau = await _context.GoiThaus
            .Include(g => g.DuAn)
            .Include(g => g.CongViecGoiThaus)
            .FirstOrDefaultAsync(g => g.Id == idGoiThau && (g.DuAn == null || g.DuAn.TrangThai != 10));

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

        // Nạp hợp đồng liên kết gói thầu để lấy số liệu đấu thầu
        var contracts = await _context.HopDongs
            .AsNoTracking()
            .Include(h => h.NhaThau)
            .Where(h => h.GoiThauId == idGoiThau && !h.IsDeleted)
            .ToListAsync();

        decimal tongGiaTriHd = contracts.Sum(h => h.GiaTriHopDong) / factor;
        var nhaThauTrungThau = contracts.FirstOrDefault(h => h.NhaThau != null)?.NhaThau?.Name;

        return new CongViecGoiThauReportDto
        {
            GoiThauId = goiThau.Id,
            TenGoiThau = goiThau.Name,
            MaGoiThau = goiThau.Code,
            TenDuAn = goiThau.DuAn?.Name,
            MaDuAn = goiThau.DuAn?.Code,
            HinhThucLcnt = goiThau.HinhThucLcnt ?? string.Empty,
            PhuongThucLcnt = goiThau.PhuongThucLcnt ?? string.Empty,
            TongGiaTriHopDong = tongGiaTriHd,
            TenNhaThauTrungThau = nhaThauTrungThau,
            TrangThaiGoiThau = contracts.Any() ? "Đã hoàn thành LCNT" : "Đang lựa chọn nhà thầu",
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
            worksheet.Range("A1:E1").Merge();

            // Package Name
            worksheet.Cell("A2").Value = report.TenGoiThau;
            worksheet.Cell("A2").Style.Font.Bold = true;
            worksheet.Cell("A2").Style.Font.FontSize = 12;
            worksheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A2:E2").Merge();

            // Headers row 4
            worksheet.Cell("A4").Value = "STT";
            worksheet.Cell("B4").Value = "Tài liệu";
            worksheet.Cell("C4").Value = "Ngày ký";
            worksheet.Cell("D4").Value = "Loại văn bản";
            worksheet.Cell("E4").Value = "Tình trạng";

            var headerRange = worksheet.Range("A4:E4");
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

                var rowRange = worksheet.Range(currentRow, 1, currentRow, 5);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

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

                await writer.WriteLineAsync($"\"STT\",\"Tên tài liệu\",\"Ngày ký\",\"Loại văn bản\",\"Tình trạng\"");

                foreach (var c in report.CongViecs)
                {
                    string ngayKy = c.NgayKy.HasValue ? c.NgayKy.Value.ToString("dd/MM/yyyy") : "-";
                    await writer.WriteLineAsync($"\"{c.Stt}\",\"{EscapeCsvField(c.TenTaiLieu)}\",\"{ngayKy}\",\"{EscapeCsvField(c.LoaiVanBan ?? "-")}\",\"{EscapeCsvField(c.TinhTrang ?? "-")}\"");
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
            htmlBuilder.AppendLine("    </tr>");
        }

        htmlBuilder.AppendLine("  </tbody>");
        htmlBuilder.AppendLine("</table>");
        htmlBuilder.AppendLine("</body>");
        htmlBuilder.AppendLine("</html>");

        return System.Text.Encoding.UTF8.GetBytes(htmlBuilder.ToString());
    }


}
