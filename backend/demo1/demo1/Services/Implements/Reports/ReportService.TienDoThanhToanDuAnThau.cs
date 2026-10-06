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
    #region 9. Báo cáo Theo dõi Tiến độ Thanh toán các Dự án Thầu (Mẫu Excel)

    public async Task<TienDoThanhToanDuAnThauReportResponseDto> GetTienDoThanhToanDuAnThauReportAsync(int? year = null, Guid? duAnId = null, string? search = null, string? donViTinh = null)
    {
        var (conversionFactor, unitName) = ParseUnit(donViTinh);

        IQueryable<HopDong> query = _context.HopDongs.AsNoTracking()
            .Include(h => h.DuAn)
                .ThenInclude(d => d!.DanhSachNguonVon)
                    .ThenInclude(nv => nv.NguonVon)
            .Include(h => h.GoiThau)
            .Include(h => h.NhaThau)
            .Include(h => h.DotThanhToans);

        if (year.HasValue)
        {
            query = query.Where(h => (h.NgayKy.HasValue && h.NgayKy.Value.Year == year.Value) ||
                                     (h.NgayHieuLuc.HasValue && h.NgayHieuLuc.Value.Year == year.Value) ||
                                     (h.CreatedAt.Year == year.Value));
        }

        if (duAnId.HasValue)
        {
            query = query.Where(h => h.DuAnId == duAnId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(h => h.Name.ToLower().Contains(keyword) ||
                                     h.Code.ToLower().Contains(keyword) ||
                                     (h.SoHopDong != null && h.SoHopDong.ToLower().Contains(keyword)) ||
                                     (h.DuAn != null && h.DuAn.Name.ToLower().Contains(keyword)) ||
                                     (h.GoiThau != null && h.GoiThau.Name.ToLower().Contains(keyword)) ||
                                     (h.NhaThau != null && h.NhaThau.Name.ToLower().Contains(keyword)));
        }

        var hopDongs = await query
            .OrderBy(h => h.DuAn != null ? h.DuAn.Code : "")
            .ThenBy(h => h.DuAn != null ? h.DuAn.Name : "")
            .ThenBy(h => h.GoiThau != null ? h.GoiThau.Name : "")
            .ThenBy(h => h.Code)
            .ToListAsync();

        var rows = new List<TienDoThanhToanDuAnThauReportRowDto>();
        int projectIndex = 0;
        Guid? currentDuAnId = null;
        int packageInProjectIndex = 0;

        foreach (var hd in hopDongs)
        {
            if (hd.DuAnId != currentDuAnId)
            {
                currentDuAnId = hd.DuAnId;
                projectIndex++;
                packageInProjectIndex = 1;
            }
            else
            {
                packageInProjectIndex++;
            }

            string sttDisplay = currentDuAnId.HasValue ? $"{projectIndex}.{packageInProjectIndex}" : $"{projectIndex}";

            // Nguồn vốn
            string nguonVonText = string.Empty;
            if (hd.DuAn?.DanhSachNguonVon != null && hd.DuAn.DanhSachNguonVon.Any())
            {
                nguonVonText = string.Join(", ", hd.DuAn.DanhSachNguonVon
                    .Where(nv => nv.NguonVon != null && !string.IsNullOrWhiteSpace(nv.NguonVon.Name))
                    .Select(nv => nv.NguonVon!.Name.Trim())
                    .Distinct());
            }

            // Đợt thanh toán & Tạm ứng
            var dotThanhToans = hd.DotThanhToans
                .OrderBy(d => d.NgayThanhToan ?? d.CreatedAt)
                .ToList();

            decimal tamUngRaw = 0;
            var cacLanThanhToan = new List<decimal>();

            foreach (var dot in dotThanhToans)
            {
                var nameLower = dot.TenDot.ToLower();
                if (nameLower.Contains("tạm ứng") || nameLower.Contains("tam ung") || nameLower.Contains("advance"))
                {
                    tamUngRaw += dot.GiaTriThanhToan;
                }
                else
                {
                    decimal valConverted = conversionFactor > 1m ? Math.Round(dot.GiaTriThanhToan / conversionFactor, 2) : dot.GiaTriThanhToan;
                    cacLanThanhToan.Add(valConverted);
                }
            }

            decimal giaTriHopDongConverted = conversionFactor > 1m ? Math.Round(hd.GiaTriHopDong / conversionFactor, 2) : hd.GiaTriHopDong;
            decimal tamUngConverted = conversionFactor > 1m ? Math.Round(tamUngRaw / conversionFactor, 2) : tamUngRaw;

            rows.Add(new TienDoThanhToanDuAnThauReportRowDto
            {
                Stt = projectIndex,
                SttDisplay = sttDisplay,
                NguonVon = nguonVonText,
                TenDuAn = hd.DuAn?.Name ?? string.Empty,
                SoQuyetDinhPheDuyetDuToan = hd.DuAn?.SoQuyetDinhPheDuyetDuToan ?? hd.DuAn?.SoQuyetDinh,
                TenGoiThau = hd.GoiThau?.Name ?? hd.Name,
                SoQuyetDinhKQLCNT = hd.GoiThau?.SoQuyetDinhKQLCNT,
                TenNhaThau = hd.NhaThau?.Name ?? string.Empty,
                MaSoThue = hd.NhaThau?.TaxCode,
                DiaChi = hd.NhaThau?.Address,
                SoHopDong = !string.IsNullOrWhiteSpace(hd.SoHopDong) ? hd.SoHopDong : hd.Code,
                NgayKy = hd.NgayKy ?? hd.NgayHieuLuc,
                ThoiGianThucHien = hd.ThoiHanThucHien,
                GiaTriHopDong = giaTriHopDongConverted,
                TamUng = tamUngConverted,
                CacLanThanhToan = cacLanThanhToan,
                GhiChu = hd.Description,
                HopDongId = hd.Id,
                GoiThauId = hd.GoiThauId,
                DuAnId = hd.DuAnId
            });
        }

        int maxLanCount = rows.Any() ? rows.Max(r => r.CacLanThanhToan.Count) : 0;

        var tongCacLanThanhToan = new List<decimal>();
        for (int i = 0; i < maxLanCount; i++)
        {
            decimal sumLan = rows.Sum(r => i < r.CacLanThanhToan.Count ? r.CacLanThanhToan[i] : 0m);
            tongCacLanThanhToan.Add(sumLan);
        }

        var summary = new TienDoThanhToanDuAnThauReportSummaryDto
        {
            TongSoHopDong = rows.Count,
            TongGiaTriHopDong = rows.Sum(r => r.GiaTriHopDong),
            TongTamUng = rows.Sum(r => r.TamUng),
            TongDaThanhToan = rows.Sum(r => r.TamUng + r.CacLanThanhToan.Sum()),
            TongCacLanThanhToan = tongCacLanThanhToan
        };

        return new TienDoThanhToanDuAnThauReportResponseDto
        {
            Title = "BÁO CÁO THEO DÕI TIẾN ĐỘ THANH TOÁN CÁC DỰ ÁN THẦU",
            Unit = unitName,
            MaxDotThanhToanCount = maxLanCount,
            Summary = summary,
            Rows = rows
        };
    }

    public async Task<IReadOnlyList<DuAnLookupDto>> GetTienDoThanhToanFilterOptionsAsync()
    {
        // Trả về danh sách dự án thu gọn dành riêng cho bộ lọc Báo cáo
        return await _context.DuAns.AsNoTracking()
            .Where(d => d.IsActive && !d.IsDeleted)
            .OrderBy(d => d.Code)
            .ThenBy(d => d.Name)
            .Select(d => new DuAnLookupDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name
            })
            .ToListAsync();
    }

    public async Task<byte[]> ExportTienDoThanhToanDuAnThauReportExcelAsync(int? year = null, Guid? duAnId = null, string? search = null, string? donViTinh = null)
    {
        var reportData = await GetTienDoThanhToanDuAnThauReportAsync(year, duAnId, search, donViTinh);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("BaoCaoTienDoThanhToan");

        int maxLan = reportData.MaxDotThanhToanCount;
        int totalCols = 14 + maxLan; // 14 fixed columns + maxLan payment columns

        // Title
        var titleCell = worksheet.Cell(1, 1);
        titleCell.Value = reportData.Title.ToUpper();
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 14;
        titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range(1, 1, 1, totalCols).Merge();

        // Subtitle / Unit
        var subTitleCell = worksheet.Cell(2, 1);
        subTitleCell.Value = $"Đơn vị tính: {reportData.Unit}";
        subTitleCell.Style.Font.Italic = true;
        subTitleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        worksheet.Range(2, 1, 2, totalCols).Merge();

        // Header Row (Row 3)
        int headerRow = 3;
        var headers = new List<string>
        {
            "STT",
            "Nguồn vốn hình thành",
            "Dự án",
            "Quyết định phê duyệt dự toán",
            "Tên gói thầu",
            "Quyết định phê duyệt KQLCNT",
            "Công ty",
            "Mã số thuế",
            "Địa chỉ",
            "Số HĐ",
            "Ngày ký",
            "Thời gian thực hiện HĐ",
            "Giá trị HĐ",
            "Tạm ứng"
        };

        for (int i = 1; i <= maxLan; i++)
        {
            headers.Add($"Lần {i}");
        }

        for (int col = 0; col < headers.Count; col++)
        {
            var cell = worksheet.Cell(headerRow, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        worksheet.Row(headerRow).Height = 28;

        // Data Rows
        int currentRow = 4;
        foreach (var row in reportData.Rows)
        {
            worksheet.Cell(currentRow, 1).Value = row.SttDisplay;
            worksheet.Cell(currentRow, 2).Value = row.NguonVon;
            worksheet.Cell(currentRow, 3).Value = row.TenDuAn;
            worksheet.Cell(currentRow, 4).Value = row.SoQuyetDinhPheDuyetDuToan ?? string.Empty;
            worksheet.Cell(currentRow, 5).Value = row.TenGoiThau;
            worksheet.Cell(currentRow, 6).Value = row.SoQuyetDinhKQLCNT ?? string.Empty;
            worksheet.Cell(currentRow, 7).Value = row.TenNhaThau;
            worksheet.Cell(currentRow, 8).Value = row.MaSoThue ?? string.Empty;
            worksheet.Cell(currentRow, 9).Value = row.DiaChi ?? string.Empty;
            worksheet.Cell(currentRow, 10).Value = row.SoHopDong;
            
            if (row.NgayKy.HasValue)
            {
                worksheet.Cell(currentRow, 11).Value = row.NgayKy.Value.ToString("dd/MM/yyyy");
            }
            else
            {
                worksheet.Cell(currentRow, 11).Value = string.Empty;
            }

            worksheet.Cell(currentRow, 12).Value = row.ThoiGianThucHien ?? string.Empty;

            // Value columns
            var valCell = worksheet.Cell(currentRow, 13);
            valCell.Value = row.GiaTriHopDong;
            valCell.Style.NumberFormat.Format = "#,#0";
            valCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var advCell = worksheet.Cell(currentRow, 14);
            advCell.Value = row.TamUng;
            advCell.Style.NumberFormat.Format = "#,#0";
            advCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Payment rounds
            for (int i = 0; i < maxLan; i++)
            {
                var pCell = worksheet.Cell(currentRow, 15 + i);
                if (i < row.CacLanThanhToan.Count)
                {
                    pCell.Value = row.CacLanThanhToan[i];
                    pCell.Style.NumberFormat.Format = "#,#0";
                    pCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
                else
                {
                    pCell.Value = string.Empty;
                }
            }

            // Borders for data row
            for (int col = 1; col <= totalCols; col++)
            {
                worksheet.Cell(currentRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            currentRow++;
        }

        // Summary Row
        worksheet.Cell(currentRow, 1).Value = "TỔNG CỘNG";
        worksheet.Range(currentRow, 1, currentRow, 12).Merge();
        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
        worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var sumGiaTriCell = worksheet.Cell(currentRow, 13);
        sumGiaTriCell.Value = reportData.Summary.TongGiaTriHopDong;
        sumGiaTriCell.Style.Font.Bold = true;
        sumGiaTriCell.Style.NumberFormat.Format = "#,#0";
        sumGiaTriCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        var sumTamUngCell = worksheet.Cell(currentRow, 14);
        sumTamUngCell.Value = reportData.Summary.TongTamUng;
        sumTamUngCell.Style.Font.Bold = true;
        sumTamUngCell.Style.NumberFormat.Format = "#,#0";
        sumTamUngCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        for (int i = 0; i < maxLan; i++)
        {
            var sumLanCell = worksheet.Cell(currentRow, 15 + i);
            if (i < reportData.Summary.TongCacLanThanhToan.Count)
            {
                sumLanCell.Value = reportData.Summary.TongCacLanThanhToan[i];
                sumLanCell.Style.Font.Bold = true;
                sumLanCell.Style.NumberFormat.Format = "#,#0";
                sumLanCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }
        }

        for (int col = 1; col <= totalCols; col++)
        {
            var cell = worksheet.Cell(currentRow, col);
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        worksheet.Columns(1, totalCols).AdjustToContents(10.0, 60.0);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportTienDoThanhToanDuAnThauReportCsvAsync(int? year = null, Guid? duAnId = null, string? search = null, string? donViTinh = null)
    {
        var reportData = await GetTienDoThanhToanDuAnThauReportAsync(year, duAnId, search, donViTinh);
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("STT,NguonVon,TenDuAn,SoQuyetDinhPheDuyetDuToan,TenGoiThau,SoQuyetDinhKQLCNT,TenNhaThau,MaSoThue,DiaChi,SoHopDong,NgayKy,ThoiGianThucHien,GiaTriHopDong,TamUng");
        foreach (var r in reportData.Rows)
        {
            sb.AppendLine($"\"{r.SttDisplay}\",\"{r.NguonVon}\",\"{r.TenDuAn}\",\"{r.SoQuyetDinhPheDuyetDuToan}\",\"{r.TenGoiThau}\",\"{r.SoQuyetDinhKQLCNT}\",\"{r.TenNhaThau}\",\"{r.MaSoThue}\",\"{r.DiaChi}\",\"{r.SoHopDong}\",\"{r.NgayKy:dd/MM/yyyy}\",\"{r.ThoiGianThucHien}\",{r.GiaTriHopDong},{r.TamUng}");
        }

        return System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<byte[]> ExportTienDoThanhToanDuAnThauReportHtmlAsync(int? year = null, Guid? duAnId = null, string? search = null, string? donViTinh = null)
    {
        var reportData = await GetTienDoThanhToanDuAnThauReportAsync(year, duAnId, search, donViTinh);
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("<html><head><meta charset='utf-8'/><style>table { border-collapse: collapse; width: 100%; } th, td { border: 1px solid #ccc; padding: 6px; text-align: left; } th { background-color: #d9e1f2; }</style></head><body>");
        sb.AppendLine($"<h2>{reportData.Title}</h2>");
        sb.AppendLine("<table><thead><tr><th>STT</th><th>Nguồn vốn</th><th>Dự án</th><th>QĐ Phê duyệt dự toán</th><th>Tên gói thầu</th><th>QĐ KQLCNT</th><th>Công ty</th><th>Mã số thuế</th><th>Số HĐ</th><th>Ngày ký</th><th>Giá trị HĐ</th><th>Tạm ứng</th></tr></thead><tbody>");

        foreach (var r in reportData.Rows)
        {
            sb.AppendLine($"<tr><td>{r.SttDisplay}</td><td>{r.NguonVon}</td><td>{r.TenDuAn}</td><td>{r.SoQuyetDinhPheDuyetDuToan}</td><td>{r.TenGoiThau}</td><td>{r.SoQuyetDinhKQLCNT}</td><td>{r.TenNhaThau}</td><td>{r.MaSoThue}</td><td>{r.SoHopDong}</td><td>{r.NgayKy:dd/MM/yyyy}</td><td>{r.GiaTriHopDong:N0}</td><td>{r.TamUng:N0}</td></tr>");
        }

        sb.AppendLine("</tbody></table></body></html>");
        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    #endregion

}
