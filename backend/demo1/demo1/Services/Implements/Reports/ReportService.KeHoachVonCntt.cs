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
    #region 6. Báo cáo Tổng hợp & Phân kỳ Vốn Đầu tư CNTT Giai đoạn (Nghị quyết 16-NQ-NHHT)

    public async Task<KeHoachVonCnttReportResponseDto> GetKeHoachVonCnttReportAsync(
        int? fromYear,
        int? toYear,
        int? groupStatus,
        string? donViTinh = null,
        string? keyword = null,
        string? projectType = null)
    {
        int endY = toYear ?? DateTime.Now.Year;
        int startY = fromYear ?? (endY - 2);
        var (factor, unitName) = ParseUnit(donViTinh ?? "1");

        // 1. Lấy danh sách Nguồn vốn đang hoạt động trong CSDL
        var dbNguonVons = await _context.NguonVons
            .AsNoTracking()
            .Where(nv => nv.IsActive && !nv.IsDeleted)
            .OrderBy(nv => nv.Code)
            .ToListAsync();

        var danhSachNguonVonDto = dbNguonVons.Select(nv => new NguonVonHeaderDto
        {
            Id = nv.Id,
            Code = nv.Code,
            Name = nv.Name
        }).ToList();

        var response = new KeHoachVonCnttReportResponseDto
        {
            Title = $"TỔNG HỢP KẾ HOẠCH VỐN ĐẦU TƯ CNTT GIAI ĐOẠN {startY}-{endY}",
            FromYear = startY,
            ToYear = endY,
            Unit = unitName,
            DanhSachNguonVon = danhSachNguonVonDto,
            Groups = new List<KeHoachVonCnttReportGroupDto>()
        };

        var query = _context.DuAns
            .AsNoTracking()
            .Include(d => d.NhomDuAn)
            .Include(d => d.PhanLoaiDuAn)
            .Include(d => d.DanhSachNguonVon).ThenInclude(nv => nv.NguonVon)
            .Where(d => d.IsActive && !d.IsDeleted && d.TrangThai != 10);

        // Filter groupStatus (1: Triển khai/phê duyệt, 2: Mới)
        if (groupStatus.HasValue && groupStatus.Value == 1)
        {
            query = query.Where(p => p.DaTrienKhai == true || p.TrangThai == 2);
        }

        // Filter keyword
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            string kw = keyword.Trim().ToLower();
            query = query.Where(p =>
                (p.Name != null && p.Name.ToLower().Contains(kw)) ||
                (p.NoiDung != null && p.NoiDung.ToLower().Contains(kw)) ||
                (p.SoQuyetDinh != null && p.SoQuyetDinh.ToLower().Contains(kw)) ||
                (p.ChuDauTu != null && p.ChuDauTu.ToLower().Contains(kw)));
        }

        var projects = await query.ToListAsync();

        // Target fixed groups
        var fixedGroups = new List<(string Key, string Name, string LoaiDuAnText)>
        {
            ("A", "HỆ THỐNG PHẦN MỀM", "Phần mềm"),
            ("B", "HẠ TẦNG THIẾT BỊ HỆ THỐNG", "Hạ tầng"),
            ("C", "TÍCH HỢP HỆ THỐNG", "Tích hợp"),
            ("D", "DỰ ÁN KHÁC", "Khác")
        };

        // Filter projectType if specified
        if (!string.IsNullOrWhiteSpace(projectType))
        {
            string pt = projectType.Trim().ToLower();
            fixedGroups = fixedGroups.Where(g =>
                g.Key.ToLower() == pt ||
                g.LoaiDuAnText.ToLower() == pt ||
                g.Name.ToLower().Contains(pt)).ToList();
        }

        // Helper method to classify projects into groups A, B, C, D
        (string Key, string LoaiDuAnText) ClassifyProject(DuAn p)
        {
            var plCode = (p.PhanLoaiDuAn?.Code ?? string.Empty).Trim().ToLower();
            var plName = (p.PhanLoaiDuAn?.Name ?? string.Empty).Trim().ToLower();
            var name = (p.Name ?? string.Empty).Trim().ToLower();
            var noiDung = (p.NoiDung ?? string.Empty).Trim().ToLower();

            // A. Phần mềm
            if (plCode.Contains("software") || plCode.Contains("phan_mem") || plCode.Contains("digital_banking") ||
                plName.Contains("phần mềm") || plName.Contains("ngân hàng số") || plName.Contains("software") ||
                name.Contains("phần mềm") || name.Contains("ứng dụng") || name.Contains("app") || noiDung.Contains("phần mềm"))
            {
                return ("A", "Phần mềm");
            }

            // B. Hạ tầng thiết bị
            if (plCode.Contains("ha_tang") || plCode.Contains("hardware") || plCode.Contains("thiet_bi") || plCode.Contains("security") ||
                plName.Contains("hạ tầng") || plName.Contains("thiết bị") || plName.Contains("bảo mật") || plName.Contains("an toàn thông tin") ||
                name.Contains("hạ tầng") || name.Contains("thiết bị") || name.Contains("máy chủ") || name.Contains("server") || name.Contains("bảo mật") || noiDung.Contains("hạ tầng"))
            {
                return ("B", "Hạ tầng");
            }

            // C. Tích hợp hệ thống
            if (plCode.Contains("tich_hop") || plCode.Contains("integration") ||
                plName.Contains("tích hợp") ||
                name.Contains("tích hợp") || name.Contains("gateway") || name.Contains("kết nối") || noiDung.Contains("tích hợp"))
            {
                return ("C", "Tích hợp");
            }

            // D. Khác
            return ("D", "Khác");
        }

        int groupIndex = 0;
        foreach (var gMeta in fixedGroups)
        {
            var groupProjs = projects
                .Where(p => ClassifyProject(p).Key == gMeta.Key)
                .OrderBy(p => p.Name)
                .ToList();

            if (!groupProjs.Any())
            {
                continue;
            }

            char groupLetter = (char)('A' + groupIndex);
            groupIndex++;

            var gDto = new KeHoachVonCnttReportGroupDto
            {
                NhomTrangThai = groupStatus ?? 1,
                TenNhom = $"{groupLetter}. {gMeta.Name}",
                LoaiDuAnKey = groupLetter.ToString(),
                Rows = new List<KeHoachVonCnttReportRowDto>()
            };

            int stt = 1;
            foreach (var proj in groupProjs)
            {
                var nguonVonInPeriod = proj.DanhSachNguonVon?
                    .Where(nv => !nv.Nam.HasValue || nv.Nam.Value == 0 || (nv.Nam.Value >= startY && nv.Nam.Value <= endY))
                    .ToList() ?? new List<DuAnNguonVon>();

                bool hasProjectNguonVon = proj.DanhSachNguonVon != null && proj.DanhSachNguonVon.Any();
                decimal totalInvestment = hasProjectNguonVon
                    ? (nguonVonInPeriod.Sum(nv => nv.SoTien) / factor)
                    : ((proj.DuToanPheDuyet > 0 ? proj.DuToanPheDuyet : 0m) / factor);

                decimal vonTuCo = 0;
                decimal quyDauTuPhatTrien = 0;
                decimal nguonKhac = 0;

                if (nguonVonInPeriod.Any())
                {
                    foreach (var nvItem in nguonVonInPeriod)
                    {
                        string nvNameStr = (nvItem.NguonVon?.Name ?? string.Empty).Trim().ToLower();
                        string nvCodeStr = (nvItem.NguonVon?.Code ?? string.Empty).Trim().ToLower();
                        decimal itemVal = nvItem.SoTien / factor;

                        if (nvCodeStr.Contains("quy_dtpt") || nvCodeStr.Contains("qdtpt") || nvCodeStr.Contains("dtpt") || nvNameStr.Contains("phát triển"))
                        {
                            quyDauTuPhatTrien += itemVal;
                        }
                        else if (nvCodeStr.Contains("nv_khac") || nvCodeStr.Contains("nv_qpl") || nvNameStr.Contains("phúc lợi") || nvNameStr.Contains("khác"))
                        {
                            nguonKhac += itemVal;
                        }
                        else
                        {
                            vonTuCo += itemVal;
                        }
                    }
                }
                else if (!hasProjectNguonVon)
                {
                    vonTuCo = totalInvestment;
                }

                var row = new KeHoachVonCnttReportRowDto
                {
                    Stt = stt++,
                    DuAnId = proj.Id,
                    NoiDung = proj.Name,
                    LoaiDuAn = gMeta.LoaiDuAnText,
                    PhanLoaiDuAnId = proj.PhanLoaiDuAnId,
                    PhanLoaiDuAnCode = proj.PhanLoaiDuAn?.Code,
                    TenPhanLoaiDuAn = proj.PhanLoaiDuAn?.Name ?? gMeta.LoaiDuAnText,
                    TongMucDauTu = totalInvestment,
                    NguonVonId = null,
                    TenNguonVon = null,
                    VonTuCo = vonTuCo,
                    QuyDauTuPhatTrien = quyDauTuPhatTrien,
                    NguonKhac = nguonKhac,
                    TrangThaiText = proj.DaTrienKhai == true ? "Đang triển khai" : "Đã phê duyệt chủ trương",
                    DonViDeXuatChiDao = proj.ChuDauTu ?? "Trung tâm CNTT",
                    GhiChu = proj.SoQuyetDinh,
                    NhomTrangThai = groupStatus ?? (proj.DaTrienKhai == true ? 1 : 2),
                    PhanKyDauTu = new List<KeHoachVonCnttPhanKyDto>()
                };

                // Điền chi tiết số tiền theo từng Nguồn vốn trong danh mục
                foreach (var nvHeader in danhSachNguonVonDto)
                {
                    row.NguonVonChiTiet[nvHeader.Id] = 0m;
                }

                if (nguonVonInPeriod.Any())
                {
                    foreach (var nvItem in nguonVonInPeriod)
                    {
                        if (row.NguonVonChiTiet.ContainsKey(nvItem.NguonVonId))
                        {
                            row.NguonVonChiTiet[nvItem.NguonVonId] += nvItem.SoTien / factor;
                        }
                        else
                        {
                            row.NguonVonChiTiet[nvItem.NguonVonId] = nvItem.SoTien / factor;
                        }
                    }
                }
                else if (!hasProjectNguonVon)
                {
                    var defaultNv = danhSachNguonVonDto.FirstOrDefault(nv => nv.Code.Contains("NV_NHHT") || nv.Code.Contains("VON_TU_CO") || nv.Name.Contains("NHHT") || nv.Name.Contains("tự có"));
                    if (defaultNv != null)
                    {
                        row.NguonVonChiTiet[defaultNv.Id] = totalInvestment;
                    }
                    else if (danhSachNguonVonDto.Any())
                    {
                        row.NguonVonChiTiet[danhSachNguonVonDto.First().Id] = totalInvestment;
                    }
                }

                // Phân kỳ vốn: lấy dữ liệu từ Danh sách vốn dự án (DuAnNguonVon), so sánh năm của Danh sách vốn dự án với năm của các cột
                for (int y = startY; y <= endY; y++)
                {
                    decimal valInYear = 0m;
                    if (proj.DanhSachNguonVon != null && proj.DanhSachNguonVon.Any())
                    {
                        var sumNguonVonInYear = proj.DanhSachNguonVon
                            .Where(nv => nv.Nam == y)
                            .Sum(nv => nv.SoTien);
                        valInYear = sumNguonVonInYear / factor;
                    }

                    row.PhanKyDauTu.Add(new KeHoachVonCnttPhanKyDto { Nam = y, GiaTri = valInYear });
                }

                gDto.Rows.Add(row);
            }

            gDto.TongMucDauTuNhom = gDto.Rows.Sum(r => r.TongMucDauTu);
            gDto.TongVonTuCoNhom = gDto.Rows.Sum(r => r.VonTuCo);
            gDto.TongQuyDauTuPhatTrienNhom = gDto.Rows.Sum(r => r.QuyDauTuPhatTrien);
            gDto.TongNguonKhacNhom = gDto.Rows.Sum(r => r.NguonKhac);

            foreach (var nvHeader in danhSachNguonVonDto)
            {
                gDto.TongNguonVonByDanhMucNhom[nvHeader.Id] = gDto.Rows.Sum(r => r.NguonVonChiTiet.TryGetValue(nvHeader.Id, out var val) ? val : 0m);
            }

            for (int y = startY; y <= endY; y++)
            {
                gDto.TongPhanKyNhom[y] = gDto.Rows.Sum(r => r.PhanKyDauTu.FirstOrDefault(pk => pk.Nam == y)?.GiaTri ?? 0);
            }

            response.Groups.Add(gDto);
        }

        response.TongSoDuAn = response.Groups.Sum(g => g.Rows.Count);
        response.TongCongMucDauTu = response.Groups.Sum(g => g.TongMucDauTuNhom);
        response.TongCongVonTuCo = response.Groups.Sum(g => g.TongVonTuCoNhom);
        response.TongCongQuyDauTuPhatTrien = response.Groups.Sum(g => g.TongQuyDauTuPhatTrienNhom);
        response.TongCongNguonKhac = response.Groups.Sum(g => g.TongNguonKhacNhom);

        foreach (var nvHeader in danhSachNguonVonDto)
        {
            response.TongCongNguonVonByDanhMuc[nvHeader.Id] = response.Groups.Sum(g => g.TongNguonVonByDanhMucNhom.TryGetValue(nvHeader.Id, out var val) ? val : 0m);
        }

        for (int y = startY; y <= endY; y++)
        {
            response.TongCongPhanKy[y] = response.Groups.Sum(g => g.TongPhanKyNhom.ContainsKey(y) ? g.TongPhanKyNhom[y] : 0);
        }

        // Bổ sung DanhSachPhanBoNguon cho Mẫu Báo cáo 2 Excel (Lịch sử gộp dự án)
        var targetProjectIds = projects.Select(p => p.Id).ToList();
        var allGopLinks = targetProjectIds.Any()
            ? await _context.DuAnGopLinks
                .AsNoTracking()
                .Where(g => targetProjectIds.Contains(g.TargetDuAnId) || targetProjectIds.Contains(g.SourceDuAnId))
                .Include(g => g.SourceDuAn)
                .Include(g => g.TargetDuAn)
                    .ThenInclude(t => t.DanhSachNguonVon)
                .ToListAsync()
            : new List<DuAnGopLink>();

        int sttPhanBo = 1;
        var phanBoList = new List<KeHoachVonPhanBoNguonRowDto>();

        foreach (var link in allGopLinks)
        {
            var srcDuAn = link.SourceDuAn;
            var targetDuAn = link.TargetDuAn;
            if (srcDuAn == null || targetDuAn == null) continue;

            decimal srcTotal = link.DuToanLucGop / factor;
            decimal targetAllocated = targetDuAn.DuToanPheDuyet / factor;

            var pkDict = new Dictionary<int, decimal>();
            for (int y = startY; y <= endY; y++)
            {
                var sumInYear = targetDuAn.DanhSachNguonVon?
                    .Where(nv => nv.Nam == y)
                    .Sum(nv => nv.SoTien) ?? 0m;
                pkDict[y] = sumInYear / factor;
            }

            phanBoList.Add(new KeHoachVonPhanBoNguonRowDto
            {
                Stt = sttPhanBo++,
                MaDuAnNguon = srcDuAn.Code,
                TenDuAnNguon = srcDuAn.Name,
                SoQuyetDinhPheDuyet = srcDuAn.SoQuyetDinh,
                TongVonPheDuyet = srcTotal,
                MaDuAnTrienKhaiLienKet = targetDuAn.Code,
                TenDuAnTrienKhai = targetDuAn.Name,
                VonPhanBoChoDaTrienKhai = targetAllocated,
                PhanKyVonTheoNam = pkDict,
                VonNguonConLaiChuaPhanBo = 0m,
                TrangThaiNguon = "Đã gộp vào dự án " + targetDuAn.Code
            });
        }
        response.DanhSachPhanBoNguon = phanBoList;

        return response;
    }

    public async Task<byte[]> ExportKeHoachVonCnttReportExcelAsync(
        int? fromYear,
        int? toYear,
        int? groupStatus,
        string? donViTinh = null,
        string? keyword = null,
        string? projectType = null,
        int version = 1)
    {
        if (version == 2)
        {
            return await ExportBaoCao2PhanBoVonV2Async(fromYear, toYear, groupStatus, donViTinh, keyword, projectType);
        }

        var report = await GetKeHoachVonCnttReportAsync(fromYear, toYear, groupStatus, donViTinh, keyword, projectType);

        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Kế hoạch vốn CNTT");
            worksheet.Cell("A1").Value = report.Title;
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 14;

            worksheet.Cell("A2").Value = $"Đơn vị tính: {report.Unit}";
            worksheet.Cell("A2").Style.Font.Italic = true;

            int numYears = report.ToYear >= report.FromYear ? (report.ToYear - report.FromYear + 1) : 0;
            int nvColCount = report.DanhSachNguonVon.Any() ? report.DanhSachNguonVon.Count : 2;
            int totalCols = 4 + nvColCount + numYears + 2;

            int row = 4;
            worksheet.Cell(row, 1).Value = "STT";
            worksheet.Cell(row, 2).Value = "Nội dung";
            worksheet.Cell(row, 3).Value = "Phân loại dự án";
            worksheet.Cell(row, 4).Value = "Tổng mức đầu tư";

            int col = 5;
            if (report.DanhSachNguonVon.Any())
            {
                foreach (var nv in report.DanhSachNguonVon)
                {
                    worksheet.Cell(row, col++).Value = nv.Name;
                }
            }
            else
            {
                worksheet.Cell(row, col++).Value = "Vốn tự có";
                worksheet.Cell(row, col++).Value = "Quỹ ĐTPT";
            }

            for (int y = report.FromYear; y <= report.ToYear; y++)
            {
                worksheet.Cell(row, col++).Value = $"Năm {y}";
            }

            worksheet.Cell(row, col++).Value = "Trạng thái";
            worksheet.Cell(row, col++).Value = "Ghi chú";

            var headerRange = worksheet.Range(row, 1, row, totalCols);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");

            row++;
            foreach (var g in report.Groups)
            {
                worksheet.Cell(row, 1).Value = g.TenNhom;
                worksheet.Range(row, 1, row, totalCols).Merge().Style.Font.Bold = true;
                row++;

                foreach (var r in g.Rows)
                {
                    worksheet.Cell(row, 1).Value = r.Stt;
                    worksheet.Cell(row, 2).Value = r.NoiDung;
                    worksheet.Cell(row, 3).Value = r.LoaiDuAn ?? r.TenPhanLoaiDuAn ?? "Chưa phân loại";
                    worksheet.Cell(row, 4).Value = r.TongMucDauTu;
                    worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.##";

                    col = 5;
                    if (report.DanhSachNguonVon.Any())
                    {
                        foreach (var nv in report.DanhSachNguonVon)
                        {
                            decimal val = r.NguonVonChiTiet.TryGetValue(nv.Id, out var v) ? v : 0m;
                            worksheet.Cell(row, col).Value = val;
                            worksheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.##";
                            col++;
                        }
                    }
                    else
                    {
                        worksheet.Cell(row, col).Value = r.VonTuCo;
                        worksheet.Cell(row, col++).Style.NumberFormat.Format = "#,##0.##";
                        worksheet.Cell(row, col).Value = r.QuyDauTuPhatTrien;
                        worksheet.Cell(row, col++).Style.NumberFormat.Format = "#,##0.##";
                    }

                    for (int y = report.FromYear; y <= report.ToYear; y++)
                    {
                        var pk = r.PhanKyDauTu.FirstOrDefault(p => p.Nam == y);
                        decimal pkVal = pk != null ? pk.GiaTri : 0m;
                        worksheet.Cell(row, col).Value = pkVal;
                        worksheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.##";
                        col++;
                    }

                    worksheet.Cell(row, col++).Value = r.TrangThaiText ?? "-";
                    worksheet.Cell(row, col++).Value = r.GhiChu ?? "";

                    row++;
                }

                // Hàng Cộng nhóm
                worksheet.Cell(row, 2).Value = $"CỘNG NHÓM ({g.TenNhom})";
                worksheet.Cell(row, 4).Value = g.TongMucDauTuNhom;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.##";

                col = 5;
                if (report.DanhSachNguonVon.Any())
                {
                    foreach (var nv in report.DanhSachNguonVon)
                    {
                        decimal val = g.TongNguonVonByDanhMucNhom.TryGetValue(nv.Id, out var v) ? v : 0m;
                        worksheet.Cell(row, col).Value = val;
                        worksheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.##";
                        col++;
                    }
                }
                else
                {
                    worksheet.Cell(row, col).Value = g.TongVonTuCoNhom;
                    worksheet.Cell(row, col++).Style.NumberFormat.Format = "#,##0.##";
                    worksheet.Cell(row, col).Value = g.TongQuyDauTuPhatTrienNhom;
                    worksheet.Cell(row, col++).Style.NumberFormat.Format = "#,##0.##";
                }

                for (int y = report.FromYear; y <= report.ToYear; y++)
                {
                    decimal yVal = g.TongPhanKyNhom.TryGetValue(y, out var v) ? v : 0m;
                    worksheet.Cell(row, col).Value = yVal;
                    worksheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.##";
                    col++;
                }

                worksheet.Range(row, 1, row, totalCols).Style.Font.Bold = true;
                worksheet.Range(row, 1, row, totalCols).Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF1F5");
                row++;
            }

            // Hàng Tổng cộng toàn bộ báo cáo
            worksheet.Cell(row, 2).Value = "TỔNG CỘNG TOÀN BỘ DỰ ÁN";
            worksheet.Cell(row, 4).Value = report.TongCongMucDauTu;
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.##";

            col = 5;
            if (report.DanhSachNguonVon.Any())
            {
                foreach (var nv in report.DanhSachNguonVon)
                {
                    decimal val = report.TongCongNguonVonByDanhMuc.TryGetValue(nv.Id, out var v) ? v : 0m;
                    worksheet.Cell(row, col).Value = val;
                    worksheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.##";
                    col++;
                }
            }
            else
            {
                worksheet.Cell(row, col).Value = report.TongCongVonTuCo;
                worksheet.Cell(row, col++).Style.NumberFormat.Format = "#,##0.##";
                worksheet.Cell(row, col).Value = report.TongCongQuyDauTuPhatTrien;
                worksheet.Cell(row, col++).Style.NumberFormat.Format = "#,##0.##";
            }

            for (int y = report.FromYear; y <= report.ToYear; y++)
            {
                decimal yVal = report.TongCongPhanKy.TryGetValue(y, out var v) ? v : 0m;
                worksheet.Cell(row, col).Value = yVal;
                worksheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.##";
                col++;
            }

            worksheet.Range(row, 1, row, totalCols).Style.Font.Bold = true;
            worksheet.Range(row, 1, row, totalCols).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");

            worksheet.Columns().AdjustToContents();

            using (var ms = new MemoryStream())
            {
                workbook.SaveAs(ms);
                return ms.ToArray();
            }
        }
    }

    public async Task<byte[]> ExportKeHoachVonCnttReportCsvAsync(
        int? fromYear,
        int? toYear,
        int? groupStatus,
        string? donViTinh = null,
        string? keyword = null,
        string? projectType = null)
    {
        var report = await GetKeHoachVonCnttReportAsync(fromYear, toYear, groupStatus, donViTinh, keyword, projectType);
        using (var ms = new MemoryStream())
        {
            using (var writer = new StreamWriter(ms, System.Text.Encoding.UTF8))
            {
                writer.Write('\uFEFF');
                await writer.WriteLineAsync($"\"{report.Title}\"");
                await writer.WriteLineAsync($"\"Đơn vị tính: {report.Unit}\"");
                await writer.WriteLineAsync();

                var nvHeaderNames = report.DanhSachNguonVon.Any()
                    ? string.Join(",", report.DanhSachNguonVon.Select(nv => $"\"{EscapeCsvField(nv.Name)}\""))
                    : "\"Vốn tự có\",\"Quỹ ĐTPT\"";

                var yearHeaderNames = string.Join(",", Enumerable.Range(report.FromYear, report.ToYear - report.FromYear + 1).Select(y => $"\"Năm {y}\""));

                foreach (var g in report.Groups)
                {
                    await writer.WriteLineAsync($"\"{g.TenNhom}\"");
                    await writer.WriteLineAsync($"\"STT\",\"Nội dung\",\"Phân loại dự án\",\"Tổng mức đầu tư\",{nvHeaderNames},{yearHeaderNames},\"Trạng thái\",\"Ghi chú\"");
                    foreach (var r in g.Rows)
                    {
                        var nvVals = report.DanhSachNguonVon.Any()
                            ? string.Join(",", report.DanhSachNguonVon.Select(nv => $"\"{r.NguonVonChiTiet.GetValueOrDefault(nv.Id)}\""))
                            : $"\"{r.VonTuCo}\",\"{r.QuyDauTuPhatTrien}\"";

                        var yearVals = string.Join(",", Enumerable.Range(report.FromYear, report.ToYear - report.FromYear + 1).Select(y => {
                            var pk = r.PhanKyDauTu.FirstOrDefault(p => p.Nam == y);
                            return $"\"{pk?.GiaTri ?? 0m}\"";
                        }));

                        await writer.WriteLineAsync($"\"{r.Stt}\",\"{EscapeCsvField(r.NoiDung)}\",\"{EscapeCsvField(r.LoaiDuAn ?? r.TenPhanLoaiDuAn ?? "Chưa phân loại")}\",\"{r.TongMucDauTu}\",{nvVals},{yearVals},\"{EscapeCsvField(r.TrangThaiText ?? "-")}\",\"{EscapeCsvField(r.GhiChu ?? "")}\"");
                    }
                    await writer.WriteLineAsync();
                }
                await writer.FlushAsync();
            }
            return ms.ToArray();
        }
    }

    public async Task<byte[]> ExportKeHoachVonCnttReportHtmlAsync(
        int? fromYear,
        int? toYear,
        int? groupStatus,
        string? donViTinh = null,
        string? keyword = null,
        string? projectType = null)
    {
        var report = await GetKeHoachVonCnttReportAsync(fromYear, toYear, groupStatus, donViTinh, keyword, projectType);
        var html = new System.Text.StringBuilder();
        html.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\" /><style>body{font-family:serif;margin:20px;} table{width:100%;border-collapse:collapse;} th,td{border:1px solid #ccc;padding:6px;} th{background:#f0f0f0;}</style></head><body>");
        html.AppendLine($"<h2>{System.Web.HttpUtility.HtmlEncode(report.Title)}</h2>");
        html.AppendLine($"<p><i>Đơn vị tính: {System.Web.HttpUtility.HtmlEncode(report.Unit)}</i></p>");

        var nvThs = report.DanhSachNguonVon.Any()
            ? string.Concat(report.DanhSachNguonVon.Select(nv => $"<th>{System.Web.HttpUtility.HtmlEncode(nv.Name)}</th>"))
            : "<th>Vốn tự có</th><th>Quỹ ĐTPT</th>";

        var yearThs = string.Concat(Enumerable.Range(report.FromYear, report.ToYear - report.FromYear + 1).Select(y => $"<th>Năm {y}</th>"));

        foreach (var g in report.Groups)
        {
            html.AppendLine($"<h3>{System.Web.HttpUtility.HtmlEncode(g.TenNhom)}</h3>");
            html.AppendLine($"<table><thead><tr><th>STT</th><th>Nội dung</th><th>Phân loại dự án</th><th>Tổng mức đầu tư</th>{nvThs}{yearThs}<th>Trạng thái</th><th>Ghi chú</th></tr></thead><tbody>");
            foreach (var r in g.Rows)
            {
                var nvTds = report.DanhSachNguonVon.Any()
                    ? string.Concat(report.DanhSachNguonVon.Select(nv => $"<td>{(r.NguonVonChiTiet.TryGetValue(nv.Id, out var val) ? val : 0m):#,##0.##}</td>"))
                    : $"<td>{r.VonTuCo:#,##0.##}</td><td>{r.QuyDauTuPhatTrien:#,##0.##}</td>";

                var yearTds = string.Concat(Enumerable.Range(report.FromYear, report.ToYear - report.FromYear + 1).Select(y => {
                    var pk = r.PhanKyDauTu.FirstOrDefault(p => p.Nam == y);
                    return $"<td>{(pk?.GiaTri ?? 0m):#,##0.##}</td>";
                }));

                html.AppendLine($"<tr><td>{r.Stt}</td><td>{System.Web.HttpUtility.HtmlEncode(r.NoiDung)}</td><td>{System.Web.HttpUtility.HtmlEncode(r.LoaiDuAn ?? r.TenPhanLoaiDuAn ?? "Chưa phân loại")}</td><td>{r.TongMucDauTu:#,##0.##}</td>{nvTds}{yearTds}<td>{System.Web.HttpUtility.HtmlEncode(r.TrangThaiText ?? "-")}</td><td>{System.Web.HttpUtility.HtmlEncode(r.GhiChu ?? "")}</td></tr>");
            }
            html.AppendLine("</tbody></table>");
        }
        html.AppendLine("</body></html>");
        return System.Text.Encoding.UTF8.GetBytes(html.ToString());
    }

    #endregion



    private async Task<byte[]> ExportBaoCao2PhanBoVonV2Async(
        int? fromYear,
        int? toYear,
        int? groupStatus,
        string? donViTinh,
        string? keyword,
        string? projectType)
    {
        var report = await GetKeHoachVonCnttReportAsync(fromYear, toYear, groupStatus, donViTinh, keyword, projectType);
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Phân bổ & Kế hoạch Vốn");
        worksheet.Style.Font.FontName = "Times New Roman";
        worksheet.Style.Font.FontSize = 11;

        // Title Row 1
        worksheet.Cell("C1").Value = "BÁO CÁO PHÂN BỔ & KẾ HOẠCH VỐN";
        worksheet.Cell("C1").Style.Font.Bold = true;
        worksheet.Cell("C1").Style.Font.FontSize = 14;
        worksheet.Cell("C1").Style.Font.FontColor = XLColor.FromHtml("#1F4E78");
        worksheet.Cell("C1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range("C1:N1").Merge();

        // Subtitle Row 2
        worksheet.Cell("C2").Value = $"(Đơn vị tính: {report.Unit})";
        worksheet.Cell("C2").Style.Font.Italic = true;
        worksheet.Cell("C2").Style.Font.FontSize = 10;
        worksheet.Cell("C2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range("C2:N2").Merge();

        int year1 = report.FromYear;
        int year2 = report.ToYear > report.FromYear ? report.ToYear : (report.FromYear + 1);

        // Header Row 3
        string[] headers = [
            "STT", "Mã dự án nguồn", "Tên dự án nguồn", "Số Quyết định phê duyệt",
            $"Tổng vốn phê duyệt ({report.Unit})", "Mã dự án triển khai liên kết", "Tên dự án triển khai",
            $"Vốn phân bổ cho DA triển khai ({report.Unit})", $"Phân kỳ vốn năm {year1} ({report.Unit})",
            $"Phân kỳ vốn năm {year2} ({report.Unit})", $"Vốn nguồn còn lại chưa phân bổ ({report.Unit})", "Trạng thái nguồn"
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

        if (report.DanhSachPhanBoNguon != null && report.DanhSachPhanBoNguon.Any())
        {
            foreach (var item in report.DanhSachPhanBoNguon)
            {
                worksheet.Cell(currentRow, 3).Value = stt++;
                worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 4).Value = item.MaDuAnNguon;
                worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 5).Value = item.TenDuAnNguon;
                worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                worksheet.Cell(currentRow, 6).Value = item.SoQuyetDinhPheDuyet ?? string.Empty;
                worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 7).Value = item.TongVonPheDuyet;
                worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(currentRow, 8).Value = item.MaDuAnTrienKhaiLienKet;
                worksheet.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 9).Value = item.TenDuAnTrienKhai;
                worksheet.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                worksheet.Cell(currentRow, 10).Value = item.VonPhanBoChoDaTrienKhai;
                worksheet.Cell(currentRow, 10).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                decimal pk1 = item.PhanKyVonTheoNam.TryGetValue(year1, out var val1) ? val1 : 0m;
                worksheet.Cell(currentRow, 11).Value = pk1;
                worksheet.Cell(currentRow, 11).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                decimal pk2 = item.PhanKyVonTheoNam.TryGetValue(year2, out var val2) ? val2 : 0m;
                worksheet.Cell(currentRow, 12).Value = pk2;
                worksheet.Cell(currentRow, 12).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(currentRow, 13).Value = item.VonNguonConLaiChuaPhanBo;
                worksheet.Cell(currentRow, 13).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(currentRow, 14).Value = item.TrangThaiNguon;
                worksheet.Cell(currentRow, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var rowRange = worksheet.Range(currentRow, 3, currentRow, 14);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                currentRow++;
            }

            // Dòng TỔNG CỘNG
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

            worksheet.Cell(currentRow, 11).FormulaA1 = $"=SUM(K{startRow}:K{endRow})";
            worksheet.Cell(currentRow, 11).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 12).FormulaA1 = $"=SUM(L{startRow}:L{endRow})";
            worksheet.Cell(currentRow, 12).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 13).FormulaA1 = $"=SUM(M{startRow}:M{endRow})";
            worksheet.Cell(currentRow, 13).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 14).Value = string.Empty;

            var totalRowRange = worksheet.Range(currentRow, 3, currentRow, 14);
            totalRowRange.Style.Font.Bold = true;
            totalRowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
            totalRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            totalRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            currentRow++;
        }

        worksheet.Columns(3, 14).AdjustToContents(10.0, 50.0);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }


}
