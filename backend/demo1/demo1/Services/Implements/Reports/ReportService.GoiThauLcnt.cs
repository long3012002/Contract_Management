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
    #region 8. Báo cáo Kế hoạch & Kết quả Lựa chọn Nhà thầu (Gói thầu) LCNT (Mẫu Báo cáo 3)

    public async Task<GoiThauLcntReportResponseDto> GetGoiThauLcntReportAsync(int? year = null, Guid? duAnId = null, string? search = null, string? donViTinh = null)
    {
        var (factor, unitName) = ParseUnit(donViTinh);

        var query = _context.GoiThaus
            .AsNoTracking()
            .Include(g => g.DuAn)
            .Where(g => !g.IsDeleted && (g.DuAn == null || (!g.DuAn.IsDeleted && g.DuAn.TrangThai != 10)))
            .AsQueryable();

        if (duAnId.HasValue)
        {
            query = query.Where(g => g.DuAnId == duAnId.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(g => g.CreatedAt.Year == year.Value || (g.DuAn != null && g.DuAn.NgayBatDau.HasValue && g.DuAn.NgayBatDau.Value.Year == year.Value));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string sLower = search.Trim().ToLower();
            query = query.Where(g => g.Name.ToLower().Contains(sLower) || g.Code.ToLower().Contains(sLower) || (g.DuAn != null && g.DuAn.Name.ToLower().Contains(sLower)));
        }

        var goiThaus = await query
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        var goiThauIds = goiThaus.Select(g => g.Id).ToList();

        // Nạp các hợp đồng liên kết
        var contracts = await _context.HopDongs
            .AsNoTracking()
            .Include(h => h.NhaThau)
            .Where(h => h.GoiThauId.HasValue && goiThauIds.Contains(h.GoiThauId.Value) && !h.IsDeleted)
            .ToListAsync();

        var contractsByGoiThau = contracts
            .GroupBy(h => h.GoiThauId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<GoiThauLcntReportRowDto>();
        int stt = 1;

        foreach (var gt in goiThaus)
        {
            contractsByGoiThau.TryGetValue(gt.Id, out var linkedContracts);
            linkedContracts ??= new List<HopDong>();

            decimal giaTriDuToan = gt.GiaTriGoiThau / factor;
            decimal tongGiaTriHd = linkedContracts.Sum(h => h.GiaTriHopDong) / factor;
            decimal giaTriTietKiem = Math.Max(0, giaTriDuToan - tongGiaTriHd);
            double tyLeSuDung = giaTriDuToan > 0 ? Math.Round((double)(tongGiaTriHd / giaTriDuToan) * 100, 2) : 0;
            string tenNhaThau = linkedContracts.FirstOrDefault(h => h.NhaThau != null)?.NhaThau?.Name ?? (linkedContracts.Any() ? "Đã ký HĐ" : "-");
            string trangThai = linkedContracts.Any() ? "Đã hoàn thành LCNT" : "Đang lựa chọn nhà thầu";

            rows.Add(new GoiThauLcntReportRowDto
            {
                Stt = stt++,
                GoiThauId = gt.Id,
                DuAnId = gt.DuAnId,
                MaDuAn = gt.DuAn?.Code ?? string.Empty,
                TenDuAn = gt.DuAn?.Name ?? string.Empty,
                MaGoiThau = gt.Code,
                TenGoiThau = gt.Name,
                GiaTriDuToan = giaTriDuToan,
                HinhThucLcnt = gt.HinhThucLcnt ?? string.Empty,
                PhuongThucLcnt = gt.PhuongThucLcnt ?? string.Empty,
                TongGiaTriHopDongDaKy = tongGiaTriHd,
                GiaTriTietKiem = giaTriTietKiem,
                TyLeSuDungDuToanPercent = tyLeSuDung,
                TenNhaThauTrungThau = tenNhaThau,
                TrangThaiGoiThau = trangThai
            });
        }

        var totalDuToan = rows.Sum(r => r.GiaTriDuToan);
        var totalHd = rows.Sum(r => r.TongGiaTriHopDongDaKy);
        var totalTietKiem = rows.Sum(r => r.GiaTriTietKiem);
        double tyLeTietKiemChung = totalDuToan > 0 ? Math.Round((double)(totalTietKiem / totalDuToan) * 100, 2) : 0;

        return new GoiThauLcntReportResponseDto
        {
            Title = "BÁO CÁO KẾ HOẠCH & KẾT QUẢ LỰA CHỌN NHÀ THẦU",
            Unit = unitName,
            Year = year,
            Summary = new GoiThauLcntReportSummaryDto
            {
                TongSoGoiThau = rows.Count,
                TongGiaTriDuToan = totalDuToan,
                TongGiaTriHopDongDaKy = totalHd,
                TongGiaTriTietKiem = totalTietKiem,
                TyLeTietKiemChungPercent = tyLeTietKiemChung
            },
            Rows = rows
        };
    }

    public async Task<byte[]> ExportGoiThauLcntReportExcelAsync(
        int? year = null,
        Guid? duAnId = null,
        string? search = null,
        string? donViTinh = null,
        int version = 2)
    {
        return await ExportBaoCao3NhaThauLcntV2Async(year, duAnId, search, donViTinh);
    }



    private async Task<byte[]> ExportBaoCao3NhaThauLcntV2Async(
        int? year,
        Guid? duAnId,
        string? search,
        string? donViTinh)
    {
        var report = await GetGoiThauLcntReportAsync(year, duAnId, search, donViTinh);
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Lựa chọn Nhà thầu");
        worksheet.Style.Font.FontName = "Times New Roman";
        worksheet.Style.Font.FontSize = 11;

        // Title Row 1
        worksheet.Cell("C1").Value = "BÁO CÁO LỰA CHỌN NHÀ THẦU (LCNT)";
        worksheet.Cell("C1").Style.Font.Bold = true;
        worksheet.Cell("C1").Style.Font.FontSize = 14;
        worksheet.Cell("C1").Style.Font.FontColor = XLColor.FromHtml("#1F4E78");
        worksheet.Cell("C1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range("C1:M1").Merge();

        // Subtitle Row 2
        worksheet.Cell("C2").Value = $"(Đơn vị tính: {report.Unit})";
        worksheet.Cell("C2").Style.Font.Italic = true;
        worksheet.Cell("C2").Style.Font.FontSize = 10;
        worksheet.Cell("C2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range("C2:M2").Merge();

        // Header Row 3
        string[] headers = [
            "STT", "Mã dự án", "Mã gói thầu", "Tên gói thầu", $"Giá trị dự toán ({report.Unit})",
            "Hình thức LCNT", "Phương thức LCNT", $"Tổng giá trị HĐ đã ký ({report.Unit})",
            // $"Giá trị tiết kiệm ({report.Unit})",
            "Tỷ lệ sử dụng dự toán (%)", "Tên nhà Thầu", "Trạng thái gói thầu"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(3, 3 + i);
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

        foreach (var row in report.Rows)
        {
            worksheet.Cell(currentRow, 3).Value = stt++;
            worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 4).Value = row.MaDuAn;
            worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 5).Value = row.MaGoiThau;
            worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 6).Value = row.TenGoiThau;
            worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            worksheet.Cell(currentRow, 7).Value = row.GiaTriDuToan;
            worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 8).Value = row.HinhThucLcnt ?? string.Empty;
            worksheet.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 9).Value = row.PhuongThucLcnt ?? string.Empty;
            worksheet.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 10).Value = row.TongGiaTriHopDongDaKy;
            worksheet.Cell(currentRow, 10).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // worksheet.Cell(currentRow, 11).Value = row.GiaTriTietKiem;
            // worksheet.Cell(currentRow, 11).Style.NumberFormat.Format = "#,##0";
            // worksheet.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 11).Value = row.TyLeSuDungDuToanPercent / 100.0;
            worksheet.Cell(currentRow, 11).Style.NumberFormat.Format = "0.0%";
            worksheet.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 12).Value = row.TenNhaThauTrungThau;
            worksheet.Cell(currentRow, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            worksheet.Cell(currentRow, 13).Value = row.TrangThaiGoiThau;
            worksheet.Cell(currentRow, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var rowRange = worksheet.Range(currentRow, 3, currentRow, 13);
            rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            currentRow++;
        }

        if (report.Rows.Count > 0)
        {
            int startRow = 4;
            int endRow = currentRow - 1;
            worksheet.Cell(currentRow, 3).Value = "TỔNG CỘNG";
            worksheet.Range(currentRow, 3, currentRow, 6).Merge();
            worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 7).FormulaA1 = $"=SUM(G{startRow}:G{endRow})";
            worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 8).Value = string.Empty;
            worksheet.Cell(currentRow, 9).Value = string.Empty;

            worksheet.Cell(currentRow, 10).FormulaA1 = $"=SUM(J{startRow}:J{endRow})";
            worksheet.Cell(currentRow, 10).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 11).FormulaA1 = $"=IF(G{currentRow}>0, J{currentRow}/G{currentRow}, 0)";
            worksheet.Cell(currentRow, 11).Style.NumberFormat.Format = "0.0%";
            worksheet.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 12).Value = string.Empty;
            worksheet.Cell(currentRow, 13).Value = string.Empty;

            var totalRowRange = worksheet.Range(currentRow, 3, currentRow, 13);
            totalRowRange.Style.Font.Bold = true;
            totalRowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
            totalRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            totalRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            currentRow++;
        }

        worksheet.Columns(3, 13).AdjustToContents(10.0, 50.0);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }


    #endregion
}
