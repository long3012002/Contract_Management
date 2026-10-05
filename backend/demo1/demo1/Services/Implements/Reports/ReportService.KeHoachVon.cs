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
            .Include(d => d.DanhSachNguonVon).ThenInclude(nv => nv.NguonVon)
            .Where(d => d.IsActive && !d.IsDeleted && d.TrangThai != 10)
            .Where(d => (!d.NgayBatDau.HasValue && !d.NamBatDau.HasValue) || (d.NgayBatDau.HasValue ? d.NgayBatDau.Value.Year <= selectedYear : d.NamBatDau!.Value <= selectedYear))
            .Where(d => (d.NgayKetThucThucTe.HasValue ? d.NgayKetThucThucTe.Value.Year >= selectedYear : d.NgayKetThuc.HasValue ? d.NgayKetThuc.Value.Year >= selectedYear : !d.NamKetThuc.HasValue || d.NamKetThuc.Value >= selectedYear))
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
                decimal duToan = (proj.DuToanPheDuyet > 0 ? proj.DuToanPheDuyet : (proj.DanhSachNguonVon != null && proj.DanhSachNguonVon.Any() ? proj.DanhSachNguonVon.Sum(x => x.SoTien) : 0m)) / factor;
                var (nhomCode, nhomTen) = ClassifyNhomKyThuat(proj, pType.Type);

                decimal vonDieuLe = 0;
                decimal quyPhucLoi = 0;
                decimal quyDauTuPhatTrien = 0;
                decimal nguonKhac = 0;

                if (proj.DanhSachNguonVon != null && proj.DanhSachNguonVon.Any())
                {
                    foreach (var nvItem in proj.DanhSachNguonVon)
                    {
                        string nvNameStr = (nvItem.NguonVon?.Name ?? string.Empty).Trim().ToLower();
                        string nvCodeStr = (nvItem.NguonVon?.Code ?? string.Empty).Trim().ToLower();
                        decimal itemVal = nvItem.SoTien / factor;

                        if (nvCodeStr.Contains("quy_dtpt") || nvCodeStr.Contains("qdtpt") || nvCodeStr.Contains("dtpt") || nvNameStr.Contains("phát triển"))
                        {
                            quyDauTuPhatTrien += itemVal;
                        }
                        else if (nvCodeStr.Contains("phuc_loi") || nvCodeStr.Contains("qpl") || nvNameStr.Contains("phúc lợi"))
                        {
                            quyPhucLoi += itemVal;
                        }
                        else if (nvCodeStr.Contains("nv_khac") || nvNameStr.Contains("khác"))
                        {
                            nguonKhac += itemVal;
                        }
                        else
                        {
                            vonDieuLe += itemVal;
                        }
                    }
                }
                else
                {
                    vonDieuLe = duToan * 0.6m;
                    quyPhucLoi = duToan * 0.4m;
                }

                var row = new KeHoachVonReportRowDto
                {
                    Stt = stt++,
                    DuAnId = proj.Id,
                    DonViChiNhanh = proj.ChuDauTu ?? "Trụ sở chính",
                    TenDuAn = proj.Name,
                    QuyMoXaydung = proj.NoiDung,
                    SuCanThiet = proj.ThoiGianThucHien,
                    HangMucCongViec = proj.ToChucThucHien,
                    VonDieuLeVaQuyDuTru = vonDieuLe,
                    QuyPhucLoi = quyPhucLoi,
                    QuyDauTuPhatTrien = quyDauTuPhatTrien,
                    NguonKhac = nguonKhac,
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


}
