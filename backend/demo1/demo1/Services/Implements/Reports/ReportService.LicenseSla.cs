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
    #region 7. Báo cáo Quản lý Hạn License / Bảo trì & SLA Nhà thầu

    public async Task<LicenseSlaReportResponseDto> GetLicenseSlaReportAsync(int? statusFilter = null, string? search = null, string? donViTinh = null)
    {
        var (conversionFactor, unitName) = ParseUnit(donViTinh);
        var today = DateTime.UtcNow.Date;

        var query = _context.Licenses
            .Include(l => l.DuAn)
            .Include(l => l.HopDong)
            .Include(l => l.NhaCungCap)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(l => (l.Code != null && l.Code.ToLower().Contains(s)) ||
                                     (l.Name != null && l.Name.ToLower().Contains(s)) ||
                                     (l.ThongTinThietBi != null && l.ThongTinThietBi.ToLower().Contains(s)) ||
                                     (l.NhaCungCap != null && l.NhaCungCap.Name != null && l.NhaCungCap.Name.ToLower().Contains(s)) ||
                                     (l.HopDong != null && l.HopDong.Name != null && l.HopDong.Name.ToLower().Contains(s)));
        }

        if (statusFilter.HasValue)
        {
            if (statusFilter.Value == 1)
            {
                // Đã hết hạn: NgayKetThuc < today
                query = query.Where(l => l.NgayKetThuc.HasValue && l.NgayKetThuc.Value < today);
            }
            else if (statusFilter.Value == 2)
            {
                // Sắp hết hạn: NgayKetThuc >= today
                query = query.Where(l => l.NgayKetThuc.HasValue && l.NgayKetThuc.Value >= today);
            }
            else if (statusFilter.Value == 3)
            {
                // Còn hiệu lực: Chưa hết hạn hoặc không có ngày kết thúc
                query = query.Where(l => !l.NgayKetThuc.HasValue || l.NgayKetThuc.Value >= today);
            }
        }

        var list = await query.ToListAsync();
        var resultItems = new List<LicenseSlaItemDto>();

        foreach (var lic in list)
        {
            var endDate = lic.NgayKetThuc?.Date;
            int daysRemaining = endDate.HasValue ? (int)(endDate.Value - today).TotalDays : 9999;

            string riskTag;
            string trangThaiText;
            int trangThaiCode = lic.TrangThai;

            if (endDate.HasValue && endDate.Value < today)
            {
                riskTag = "🔴 ĐÃ HẾT HẠN";
                trangThaiText = "Đã hết hạn";
            }
            else if (endDate.HasValue && daysRemaining <= lic.CanhBaoTruocNgay)
            {
                riskTag = "🟡 SẮP HẾT HẠN";
                trangThaiText = "Sắp hết hạn";
            }
            else
            {
                riskTag = "🟢 AN TOÀN";
                trangThaiText = "Còn hiệu lực";
            }

            if (statusFilter.HasValue)
            {
                if (statusFilter.Value == 1 && !riskTag.Contains("ĐÃ HẾT HẠN")) continue;
                if (statusFilter.Value == 2 && !riskTag.Contains("SẮP HẾT HẠN")) continue;
                if (statusFilter.Value == 3 && !riskTag.Contains("AN TOÀN")) continue;
            }

            string loaiName = lic.LoaiLicense switch
            {
                1 => "Có thời hạn (Term-based)",
                2 => "Vĩnh viễn (Perpetual)",
                3 => "Theo thiết bị vật lý",
                4 => "Theo số lượng người dùng",
                _ => "Khác"
            };

            decimal contractVal = lic.HopDong != null ? lic.HopDong.GiaTriHopDong / conversionFactor : 0m;

            resultItems.Add(new LicenseSlaItemDto
            {
                LicenseId = lic.Id,
                MaLicense = lic.Code ?? string.Empty,
                TenHeThong = lic.Name ?? string.Empty,
                TenHangSanXuat = lic.GhiChu ?? "N/A",
                TenNhaCungCap = lic.NhaCungCap?.Name ?? "Chưa xác định",
                LoaiLicense = lic.LoaiLicense,
                TenLoaiLicense = loaiName,
                SoLuong = lic.SoLuong,
                ThongTinThietBi = lic.ThongTinThietBi ?? string.Empty,
                NgayBatDau = lic.NgayBatDau,
                ThoiHan = lic.ThoiHan ?? string.Empty,
                NgayKetThuc = lic.NgayKetThuc,
                CanhBaoTruocNgay = lic.CanhBaoTruocNgay,
                SoNgayConLai = daysRemaining == 9999 ? 0 : daysRemaining,
                MaTrangThai = trangThaiCode,
                TenTrangThai = trangThaiText,
                TagCanhBaoRuiRo = riskTag,
                DeXuatHanhDong = riskTag.Contains("🔴") ? "Duyệt gấp Tờ trình mua sắm/gia hạn" : (riskTag.Contains("🟡") ? "Lập Tờ trình tái ký hợp đồng" : "Theo dõi định kỳ"),
                GhiChu = lic.GhiChu ?? string.Empty,
                HopDongId = lic.HopDongId,
                TenHopDong = lic.HopDong?.Name ?? string.Empty,
                GiaTriHopDong = contractVal,
                DuAnId = lic.DuAnId,
                TenDuAn = lic.DuAn?.Name ?? string.Empty,
                NguoiDaiDien = lic.NhaCungCap?.Representative,
                SoDienThoai = lic.NhaCungCap?.Phone
            });
        }

        return new LicenseSlaReportResponseDto
        {
            TieuDe = "BÁO CÁO QUẢN LÝ HẠN LICENSE / BẢO TRÌ & SLA NHÀ THẦU",
            DonViTinh = unitName,
            NgayTaoBaoCao = DateTime.UtcNow,
            TongHop = new LicenseSlaSummaryDto
            {
                TongSoLicense = resultItems.Count,
                SoLicenseDaHetHan = resultItems.Count(i => i.TagCanhBaoRuiRo.Contains("🔴")),
                SoLicenseSapHetHan30Ngay = resultItems.Count(i => i.TagCanhBaoRuiRo.Contains("🟡") && i.SoNgayConLai <= 30),
                SoLicenseSapHetHan90Ngay = resultItems.Count(i => i.TagCanhBaoRuiRo.Contains("🟡") && i.SoNgayConLai <= 90),
                SoLicenseAnToan = resultItems.Count(i => i.TagCanhBaoRuiRo.Contains("🟢")),
                TongChiPhiGiaHanDuKien = resultItems.Sum(i => i.GiaTriHopDong)
            },
            DanhSachChiTiet = resultItems.OrderBy(i => i.SoNgayConLai).ToList()
        };
    }

    public async Task<byte[]> ExportLicenseSlaReportExcelAsync(int? statusFilter = null, string? search = null, string? donViTinh = null)
    {
        var report = await GetLicenseSlaReportAsync(statusFilter, search, donViTinh);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("License & SLA");
        worksheet.ShowGridLines = true;

        worksheet.Cell("A1").Value = report.TieuDe;
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 14;
        worksheet.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#1F4E78");

        worksheet.Cell("A2").Value = $"Đơn vị tính: {report.DonViTinh} | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
        worksheet.Cell("A2").Style.Font.Italic = true;

        string[] headers = new[]
        {
            "STT", "Mã License", "Tên Hệ Thống / Phần Mềm", "Nhà Cung Cấp", "Loại License",
            "Thông Tin Thiết Bị / Key", "Số Lượng", "Ngày Bắt Đầu", "Ngày Hết Hạn", "Số Ngày Còn Lại",
            "Trạng Thái Rủi Ro", "Đề Xuất Hành Động", "Hợp Đồng Liên Quan", "Giá Trị HD"
        };

        int headerRow = 4;
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int rowIdx = 5;
        int stt = 1;
        foreach (var item in report.DanhSachChiTiet)
        {
            worksheet.Cell(rowIdx, 1).Value = stt++;
            worksheet.Cell(rowIdx, 2).Value = item.MaLicense;
            worksheet.Cell(rowIdx, 3).Value = item.TenHeThong;
            worksheet.Cell(rowIdx, 4).Value = item.TenNhaCungCap;
            worksheet.Cell(rowIdx, 5).Value = item.TenLoaiLicense;
            worksheet.Cell(rowIdx, 6).Value = item.ThongTinThietBi;
            worksheet.Cell(rowIdx, 7).Value = item.SoLuong ?? 0;
            worksheet.Cell(rowIdx, 8).Value = item.NgayBatDau.HasValue ? item.NgayBatDau.Value.ToString("dd/MM/yyyy") : "-";
            worksheet.Cell(rowIdx, 9).Value = item.NgayKetThuc.HasValue ? item.NgayKetThuc.Value.ToString("dd/MM/yyyy") : "-";
            worksheet.Cell(rowIdx, 10).Value = item.SoNgayConLai;
            
            var riskCell = worksheet.Cell(rowIdx, 11);
            riskCell.Value = item.TagCanhBaoRuiRo;
            if (item.TagCanhBaoRuiRo.Contains("🔴"))
            {
                riskCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FCE4D6");
                riskCell.Style.Font.FontColor = XLColor.FromHtml("#C00000");
                riskCell.Style.Font.Bold = true;
            }
            else if (item.TagCanhBaoRuiRo.Contains("🟡"))
            {
                riskCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");
                riskCell.Style.Font.FontColor = XLColor.FromHtml("#806000");
                riskCell.Style.Font.Bold = true;
            }
            else
            {
                riskCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2EFDA");
                riskCell.Style.Font.FontColor = XLColor.FromHtml("#375623");
                riskCell.Style.Font.Bold = true;
            }

            worksheet.Cell(rowIdx, 12).Value = item.DeXuatHanhDong;
            worksheet.Cell(rowIdx, 13).Value = item.TenHopDong;
            worksheet.Cell(rowIdx, 14).Value = item.GiaTriHopDong;
            worksheet.Cell(rowIdx, 14).Style.NumberFormat.Format = "#,##0.##";
            rowIdx++;
        }

        if (report.DanhSachChiTiet.Count > 0)
        {
            int endRow = rowIdx - 1;
            worksheet.Cell(rowIdx, 1).Value = "TỔNG CỘNG";
            worksheet.Range(rowIdx, 1, rowIdx, 13).Merge();
            worksheet.Range(rowIdx, 1, rowIdx, 13).Style.Font.Bold = true;
            worksheet.Range(rowIdx, 1, rowIdx, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(rowIdx, 14).FormulaA1 = $"=SUM(N5:N{endRow})";
            worksheet.Cell(rowIdx, 14).Style.NumberFormat.Format = "#,##0.##";
            worksheet.Cell(rowIdx, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var totalRowRange = worksheet.Range(rowIdx, 1, rowIdx, 14);
            totalRowRange.Style.Font.Bold = true;
            totalRowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
            totalRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            totalRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            rowIdx++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportLicenseSlaReportCsvAsync(int? statusFilter = null, string? search = null, string? donViTinh = null)
    {
        var report = await GetLicenseSlaReportAsync(statusFilter, search, donViTinh);
        var csv = new System.Text.StringBuilder();

        csv.AppendLine("STT,MaLicense,TenHeThong,NhaCungCap,LoaiLicense,ThongTinThietBi,SoLuong,NgayBatDau,NgayKetThuc,SoNgayConLai,TagCanhBaoRuiRo,DeXuatHanhDong,TenHopDong,GiaTriHopDong");

        int stt = 1;
        foreach (var item in report.DanhSachChiTiet)
        {
            csv.AppendLine($"{stt++},\"{item.MaLicense}\",\"{item.TenHeThong}\",\"{item.TenNhaCungCap}\",\"{item.TenLoaiLicense}\",\"{item.ThongTinThietBi}\",{item.SoLuong ?? 0},\"{item.NgayBatDau:dd/MM/yyyy}\",\"{item.NgayKetThuc:dd/MM/yyyy}\",{item.SoNgayConLai},\"{item.TagCanhBaoRuiRo}\",\"{item.DeXuatHanhDong}\",\"{item.TenHopDong}\",{item.GiaTriHopDong}");
        }

        return System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    public async Task<byte[]> ExportLicenseSlaReportHtmlAsync(int? statusFilter = null, string? search = null, string? donViTinh = null)
    {
        var report = await GetLicenseSlaReportAsync(statusFilter, search, donViTinh);
        var html = new System.Text.StringBuilder();
        html.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\" /><style>body{font-family:sans-serif;margin:20px;} table{width:100%;border-collapse:collapse;} th,td{border:1px solid #ccc;padding:6px;} th{background:#1F4E78;color:#fff;}</style></head><body>");
        html.AppendLine($"<h2>{System.Web.HttpUtility.HtmlEncode(report.TieuDe)}</h2>");
        html.AppendLine($"<p><b>Đã hết hạn:</b> {report.TongHop.SoLicenseDaHetHan} | <b>Sắp hết hạn:</b> {report.TongHop.SoLicenseSapHetHan90Ngay} | <b>An toàn:</b> {report.TongHop.SoLicenseAnToan}</p>");
        html.AppendLine("<table><thead><tr><th>STT</th><th>Mã</th><th>Tên Hệ Thống</th><th>Nhà Cung Cấp</th><th>Loại License</th><th>Hạn Sử Dụng</th><th>Còn Lại (Ngày)</th><th>Trạng Thái</th><th>Đề Xuất</th></tr></thead><tbody>");

        int stt = 1;
        foreach (var r in report.DanhSachChiTiet)
        {
            html.AppendLine($"<tr><td>{stt++}</td><td>{System.Web.HttpUtility.HtmlEncode(r.MaLicense)}</td><td>{System.Web.HttpUtility.HtmlEncode(r.TenHeThong)}</td><td>{System.Web.HttpUtility.HtmlEncode(r.TenNhaCungCap)}</td><td>{System.Web.HttpUtility.HtmlEncode(r.TenLoaiLicense)}</td><td>{r.NgayKetThuc:dd/MM/yyyy}</td><td>{r.SoNgayConLai}</td><td>{System.Web.HttpUtility.HtmlEncode(r.TagCanhBaoRuiRo)}</td><td>{System.Web.HttpUtility.HtmlEncode(r.DeXuatHanhDong)}</td></tr>");
        }
        html.AppendLine("</tbody></table></body></html>");

        return System.Text.Encoding.UTF8.GetBytes(html.ToString());
    }

    #endregion


}
