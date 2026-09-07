using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Báo cáo &amp; Thống kê Quản lý Hợp đồng &amp; Dự án.
/// Quy hoạch toàn bộ các API báo cáo dưới Base Route /api/NghiepVu/reports với hỗ trợ đầy đủ Legacy Route Aliases.
/// </summary>
[Authorize]
[ApiController]
[Route("api/NghiepVu/reports")]
public class ReportsController(IReportService reportService) : ControllerBase
{
    #region 1. Báo cáo Tổng hợp Tình hình Đầu tư Dự án

    /// <summary>
    /// Lấy dữ liệu báo cáo tổng hợp tình hình thực hiện đầu tư (dự án, gói thầu, hợp đồng).
    /// </summary>
    /// <param name="year">Năm báo cáo (mặc định: năm hiện tại)</param>
    /// <param name="period">Kỳ báo cáo: 1 (6 tháng đầu năm), 2 (Cả năm)</param>
    /// <param name="donViTinh">Đơn vị tính (1 hoặc đồng: Đồng, 2 hoặc nghìn: Nghìn đồng, 3 hoặc triệu: Triệu đồng, 4 hoặc tỷ: Tỷ đồng)</param>
    /// <returns>Bảng tổng hợp kinh phí đầu tư và danh sách chi tiết các dự án</returns>
    [HttpGet("dau-tu")]
    [HttpGet("/api/NghiepVu/report/investment")]
    [ProducesResponseType(typeof(ReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReportResponseDto>> GetInvestmentReport(
        [FromQuery] int? year,
        [FromQuery] int period = 1,
        [FromQuery] string? donViTinh = null)
    {
        int selectedYear = year ?? DateTime.UtcNow.Year;

        if (period != 1 && period != 2)
        {
            return BadRequest(new { message = "Kỳ báo cáo không hợp lệ. Chỉ chấp nhận 1 (6 tháng đầu năm) hoặc 2 (1 năm)." });
        }

        try
        {
            var report = await reportService.GetInvestmentReportAsync(selectedYear, period, donViTinh);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo báo cáo đầu tư.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Xuất file báo cáo đầu tư (Excel, CSV, HTML hoặc chuỗi mã hóa Base64).
    /// </summary>
    /// <param name="year">Năm báo cáo</param>
    /// <param name="period">Kỳ báo cáo: 1 (6 tháng), 2 (1 năm)</param>
    /// <param name="format">Định dạng xuất: xlsx, csv, html (mặc định: xlsx)</param>
    /// <param name="base64">Trả về chuỗi Base64 thay vì download file trực tiếp</param>
    /// <param name="donViTinh">Đơn vị tính (mặc định: đồng, các giá trị khác: triệu, tỷ, nghìn)</param>
    [HttpGet("dau-tu/export")]
    [HttpGet("/api/NghiepVu/report/investment/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportInvestmentReport(
        [FromQuery] int? year,
        [FromQuery] int period = 1,
        [FromQuery] string format = "xlsx",
        [FromQuery] bool base64 = false,
        [FromQuery] string? donViTinh = null)
    {
        int selectedYear = year ?? DateTime.UtcNow.Year;

        if (period != 1 && period != 2)
        {
            return BadRequest(new { message = "Kỳ báo cáo không hợp lệ. Chỉ chấp nhận 1 (6 tháng đầu năm) hoặc 2 (1 năm)." });
        }

        try
        {
            var report = await reportService.GetInvestmentReportAsync(selectedYear, period, donViTinh);

            byte[] fileBytes;
            string contentType;
            string extension;
            string formatLower = format?.ToLower() ?? "xlsx";

            if (formatLower == "csv")
            {
                fileBytes = await reportService.ExportInvestmentReportCsvAsync(selectedYear, period, donViTinh);
                contentType = "text/csv";
                extension = "csv";
            }
            else if (formatLower == "html")
            {
                fileBytes = await reportService.ExportInvestmentReportHtmlAsync(selectedYear, period, donViTinh);
                contentType = "text/html";
                extension = "html";
            }
            else
            {
                fileBytes = await reportService.ExportInvestmentReportExcelAsync(selectedYear, period, donViTinh);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                extension = "xlsx";
            }

            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = $"BaoCaoDauTu_{selectedYear}_{report.PeriodName}_{timestamp}.{extension}";

            if (base64)
            {
                var base64Data = Convert.ToBase64String(fileBytes);
                return Ok(new
                {
                    fileName,
                    contentType,
                    base64Data
                });
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo đầu tư.", detail = ex.Message });
        }
    }

    #endregion

    #region 2. Báo cáo Theo dõi Giải ngân & Thanh toán Hợp đồng

    /// <summary>
    /// Lấy báo cáo theo dõi giải ngân / đợt thanh toán hợp đồng.
    /// </summary>
    /// <param name="year">Năm thanh toán</param>
    /// <param name="loaiHopDong">Loại hợp đồng</param>
    /// <param name="loaiHopDongIds">Danh sách ID loại hợp đồng</param>
    /// <param name="search">Từ khóa tìm kiếm</param>
    /// <param name="donViTinh">Đơn vị tính (1 hoặc đồng: Đồng, 2 hoặc nghìn: Nghìn đồng, 3 hoặc triệu: Triệu đồng, 4 hoặc tỷ: Tỷ đồng)</param>
    [HttpGet("thanh-toan-hop-dong")]
    [HttpGet("/api/NghiepVu/report/contract-payments")]
    [ProducesResponseType(typeof(ContractPaymentReportResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ContractPaymentReportResponseDto>> GetContractPaymentReport(
        [FromQuery] int? year,
        [FromQuery] int? loaiHopDong,
        [FromQuery] List<Guid>? loaiHopDongIds,
        [FromQuery] string? search,
        [FromQuery] string? donViTinh = null)
    {
        int selectedYear = year ?? DateTime.UtcNow.Year;

        try
        {
            var report = await reportService.GetContractPaymentReportAsync(selectedYear, loaiHopDong, loaiHopDongIds, search, donViTinh);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy báo cáo theo dõi thanh toán hợp đồng.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Xuất báo cáo theo dõi thanh toán hợp đồng (Excel, CSV, HTML).
    /// </summary>
    /// <param name="year">Năm thanh toán</param>
    /// <param name="loaiHopDong">Loại hợp đồng</param>
    /// <param name="loaiHopDongIds">Danh sách ID loại hợp đồng</param>
    /// <param name="search">Từ khóa tìm kiếm</param>
    /// <param name="format">Định dạng xuất: xlsx, csv, html</param>
    /// <param name="base64">Trả về dạng mã hóa Base64</param>
    /// <param name="donViTinh">Đơn vị tính (mặc định: đồng, các giá trị khác: triệu, tỷ, nghìn)</param>
    [HttpGet("thanh-toan-hop-dong/export")]
    [HttpGet("/api/NghiepVu/report/contract-payments/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportContractPaymentReport(
        [FromQuery] int? year,
        [FromQuery] int? loaiHopDong,
        [FromQuery] List<Guid>? loaiHopDongIds,
        [FromQuery] string? search,
        [FromQuery] string format = "xlsx",
        [FromQuery] bool base64 = false,
        [FromQuery] string? donViTinh = null)
    {
        int selectedYear = year ?? DateTime.UtcNow.Year;

        try
        {
            byte[] fileBytes;
            string contentType;
            string extension;
            string formatLower = format?.ToLower() ?? "xlsx";

            if (formatLower == "csv")
            {
                fileBytes = await reportService.ExportContractPaymentReportCsvAsync(selectedYear, loaiHopDong, loaiHopDongIds, search, donViTinh);
                contentType = "text/csv";
                extension = "csv";
            }
            else if (formatLower == "html")
            {
                fileBytes = await reportService.ExportContractPaymentReportHtmlAsync(selectedYear, loaiHopDong, loaiHopDongIds, search, donViTinh);
                contentType = "text/html";
                extension = "html";
            }
            else
            {
                fileBytes = await reportService.ExportContractPaymentReportExcelAsync(selectedYear, loaiHopDong, loaiHopDongIds, search, donViTinh);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                extension = "xlsx";
            }

            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = $"BaoCao_TheoDoiThanhToanHopDong_{selectedYear}_{timestamp}.{extension}";

            if (base64)
            {
                var base64Data = Convert.ToBase64String(fileBytes);
                return Ok(new
                {
                    fileName,
                    contentType,
                    base64Data
                });
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo theo dõi thanh toán hợp đồng.", detail = ex.Message });
        }
    }

    #endregion

    #region 3. Báo cáo Theo dõi Hợp đồng (Mẫu Theo_dõi_HĐ.xlsx)

    /// <summary>
    /// Lấy báo cáo theo dõi hợp đồng và các đợt thanh toán liên quan.
    /// </summary>
    /// <param name="year">Năm báo cáo</param>
    /// <param name="cutoffDate">Mốc thời gian dự kiến thanh toán đến (Mặc định 31/12/{year})</param>
    /// <param name="loaiHopDongIds">Danh sách ID loại hợp đồng</param>
    /// <param name="search">Từ khóa tìm kiếm</param>
    /// <param name="donViTinh">Đơn vị tính (1 hoặc đồng: Đồng, 2 hoặc nghìn: Nghìn đồng, 3 hoặc triệu: Triệu đồng, 4 hoặc tỷ: Tỷ đồng)</param>
    [HttpGet("theo-doi-hop-dong")]
    [HttpGet("/api/NghiepVu/report/theo-doi-hop-dong")]
    [HttpGet("/api/NghiepVu/reportTheoDoiHopDong")]
    [ProducesResponseType(typeof(TheoDoiHopDongReportResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TheoDoiHopDongReportResponseDto>> GetTheoDoiHopDongReport(
        [FromQuery] int? year,
        [FromQuery] DateTime? cutoffDate,
        [FromQuery] List<Guid>? loaiHopDongIds,
        [FromQuery] string? search,
        [FromQuery] string? donViTinh = null)
    {
        try
        {
            var report = await reportService.GetTheoDoiHopDongReportAsync(year, cutoffDate, loaiHopDongIds, search, donViTinh);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy báo cáo theo dõi hợp đồng.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Xuất file báo cáo theo dõi hợp đồng (Excel, CSV, HTML hoặc JSON Base64).
    /// </summary>
    /// <param name="year">Năm báo cáo</param>
    /// <param name="cutoffDate">Mốc thời gian dự kiến thanh toán đến (Mặc định 31/12/{year})</param>
    /// <param name="loaiHopDongIds">Danh sách ID loại hợp đồng</param>
    /// <param name="search">Từ khóa tìm kiếm</param>
    /// <param name="format">Định dạng xuất: xlsx, csv, html</param>
    /// <param name="base64">Trả về dạng mã hóa Base64</param>
    /// <param name="donViTinh">Đơn vị tính (mặc định: đồng, các giá trị khác: triệu, tỷ, nghìn)</param>
    [HttpGet("theo-doi-hop-dong/export")]
    [HttpGet("/api/NghiepVu/report/theo-doi-hop-dong/export")]
    [HttpGet("/api/NghiepVu/reportTheoDoiHopDong/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportTheoDoiHopDongReport(
        [FromQuery] int? year,
        [FromQuery] DateTime? cutoffDate,
        [FromQuery] List<Guid>? loaiHopDongIds,
        [FromQuery] string? search,
        [FromQuery] string format = "xlsx",
        [FromQuery] bool base64 = false,
        [FromQuery] string? donViTinh = null)
    {
        try
        {
            byte[] fileBytes;
            string contentType;
            string extension;
            string formatLower = format?.ToLower() ?? "xlsx";

            if (formatLower == "csv")
            {
                fileBytes = await reportService.ExportTheoDoiHopDongReportCsvAsync(year, cutoffDate, loaiHopDongIds, search, donViTinh);
                contentType = "text/csv";
                extension = "csv";
            }
            else if (formatLower == "html")
            {
                fileBytes = await reportService.ExportTheoDoiHopDongReportHtmlAsync(year, cutoffDate, loaiHopDongIds, search, donViTinh);
                contentType = "text/html";
                extension = "html";
            }
            else
            {
                fileBytes = await reportService.ExportTheoDoiHopDongReportExcelAsync(year, cutoffDate, loaiHopDongIds, search, donViTinh);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                extension = "xlsx";
            }

            int selectedYear = year ?? DateTime.Now.Year;
            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = $"BaoCao_TheoDoiHopDong_{selectedYear}_{timestamp}.{extension}";

            if (base64)
            {
                var base64Data = Convert.ToBase64String(fileBytes);
                return Ok(new
                {
                    fileName,
                    contentType,
                    base64Data
                });
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo theo dõi hợp đồng.", detail = ex.Message });
        }
    }

    #endregion

    #region 4. Báo cáo Tiến độ Công việc Gói thầu

    /// <summary>
    /// Lấy báo cáo trình tự thực hiện các công việc thuộc Gói thầu.
    /// </summary>
    /// <param name="idGoiThau">Mã định danh Gói thầu (GUID)</param>
    /// <param name="donViTinh">Đơn vị tính (1 hoặc đồng: Đồng, 2 hoặc nghìn: Nghìn đồng, 3 hoặc triệu: Triệu đồng, 4 hoặc tỷ: Tỷ đồng)</param>
    [HttpGet("cong-viec-goi-thau/{idGoiThau:guid}")]
    [HttpGet("/api/NghiepVu/report/cong-viec-goi-thau/{idGoiThau:guid}")]
    [ProducesResponseType(typeof(CongViecGoiThauReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CongViecGoiThauReportDto>> GetCongViecGoiThauReport(Guid idGoiThau, [FromQuery] string? donViTinh = null)
    {
        try
        {
            var report = await reportService.GetCongViecGoiThauReportAsync(idGoiThau, donViTinh);
            return Ok(report);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy báo cáo công việc gói thầu.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Xuất file báo cáo tiến độ công việc gói thầu (Excel, CSV, HTML).
    /// </summary>
    /// <param name="idGoiThau">Mã định danh Gói thầu (GUID)</param>
    /// <param name="format">Định dạng xuất: xlsx, csv, html</param>
    /// <param name="base64">Trả về dữ liệu Base64 thay vì file trực tiếp</param>
    /// <param name="donViTinh">Đơn vị tính (1 hoặc đồng: Đồng, 2 hoặc nghìn: Nghìn đồng, 3 hoặc triệu: Triệu đồng, 4 hoặc tỷ: Tỷ đồng)</param>
    [HttpGet("cong-viec-goi-thau/{idGoiThau:guid}/export")]
    [HttpGet("/api/NghiepVu/report/cong-viec-goi-thau/{idGoiThau:guid}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportCongViecGoiThauReport(
        Guid idGoiThau,
        [FromQuery] string format = "xlsx",
        [FromQuery] bool base64 = false,
        [FromQuery] string? donViTinh = null)
    {
        try
        {
            var report = await reportService.GetCongViecGoiThauReportAsync(idGoiThau, donViTinh);

            byte[] fileBytes;
            string contentType;
            string extension;
            string formatLower = format?.ToLower() ?? "xlsx";

            if (formatLower == "csv")
            {
                fileBytes = await reportService.ExportCongViecGoiThauReportCsvAsync(idGoiThau, donViTinh);
                contentType = "text/csv";
                extension = "csv";
            }
            else if (formatLower == "html")
            {
                fileBytes = await reportService.ExportCongViecGoiThauReportHtmlAsync(idGoiThau, donViTinh);
                contentType = "text/html";
                extension = "html";
            }
            else
            {
                fileBytes = await reportService.ExportCongViecGoiThauReportExcelAsync(idGoiThau, donViTinh);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                extension = "xlsx";
            }

            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = $"BaoCao_TrinhTuThucHien_{report.MaGoiThau}_{timestamp}.{extension}";

            if (base64)
            {
                var base64Data = Convert.ToBase64String(fileBytes);
                return Ok(new
                {
                    fileName,
                    contentType,
                    base64Data
                });
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo công việc gói thầu.", detail = ex.Message });
        }
    }

    #endregion

    #region 5. Báo cáo Kế hoạch vốn Đầu tư & Mua sắm (Mẫu Phụ lục 01-05 & Phụ biểu 01)

    /// <summary>
    /// Lấy dữ liệu Báo cáo Kế hoạch vốn Đầu tư &amp; Mua sắm (Theo mẫu Phụ lục 01-05 &amp; Phụ biểu 01).
    /// </summary>
    [HttpGet("ke-hoach-von")]
    [ProducesResponseType(typeof(KeHoachVonReportResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<KeHoachVonReportResponseDto>> GetKeHoachVonReport(
        [FromQuery] int? year,
        [FromQuery] int? phuLuc,
        [FromQuery] string? donViTinh = null)
    {
        try
        {
            var report = await reportService.GetKeHoachVonReportAsync(year, phuLuc, donViTinh);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy báo cáo kế hoạch vốn.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Xuất file Báo cáo Kế hoạch vốn Đầu tư &amp; Mua sắm (Excel, CSV, HTML).
    /// </summary>
    [HttpGet("ke-hoach-von/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportKeHoachVonReport(
        [FromQuery] int? year,
        [FromQuery] int? phuLuc,
        [FromQuery] string format = "xlsx",
        [FromQuery] bool base64 = false,
        [FromQuery] string? donViTinh = null)
    {
        try
        {
            byte[] fileBytes;
            string contentType;
            string extension;
            string formatLower = format?.ToLower() ?? "xlsx";

            if (formatLower == "csv")
            {
                fileBytes = await reportService.ExportKeHoachVonReportCsvAsync(year, phuLuc, donViTinh);
                contentType = "text/csv";
                extension = "csv";
            }
            else if (formatLower == "html")
            {
                fileBytes = await reportService.ExportKeHoachVonReportHtmlAsync(year, phuLuc, donViTinh);
                contentType = "text/html";
                extension = "html";
            }
            else
            {
                fileBytes = await reportService.ExportKeHoachVonReportExcelAsync(year, phuLuc, donViTinh);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                extension = "xlsx";
            }

            int selectedYear = year ?? DateTime.Now.Year;
            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = $"BaoCao_KeHoachVon_{selectedYear}_{timestamp}.{extension}";

            if (base64)
            {
                var base64Data = Convert.ToBase64String(fileBytes);
                return Ok(new { fileName, contentType, base64Data });
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo kế hoạch vốn.", detail = ex.Message });
        }
    }

    #endregion

    #region 6. Báo cáo Tổng hợp & Phân kỳ Kế hoạch vốn CNTT Giai đoạn (Nghị quyết 16-NQ-NHHT)

    /// <summary>
    /// Lấy Báo cáo Tổng hợp &amp; Phân kỳ Kế hoạch vốn CNTT Giai đoạn.
    /// </summary>
    [HttpGet("ke-hoach-von-cntt")]
    [ProducesResponseType(typeof(KeHoachVonCnttReportResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<KeHoachVonCnttReportResponseDto>> GetKeHoachVonCnttReport(
        [FromQuery] int? fromYear,
        [FromQuery] int? toYear,
        [FromQuery] int? groupStatus,
        [FromQuery] string? donViTinh = null,
        [FromQuery] string? keyword = null,
        [FromQuery] string? projectType = null)
    {
        try
        {
            var report = await reportService.GetKeHoachVonCnttReportAsync(fromYear, toYear, groupStatus, donViTinh, keyword, projectType);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy báo cáo kế hoạch vốn CNTT.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Xuất file Báo cáo Kế hoạch vốn CNTT Giai đoạn (Excel, CSV, HTML).
    /// </summary>
    [HttpGet("ke-hoach-von-cntt/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportKeHoachVonCnttReport(
        [FromQuery] int? fromYear,
        [FromQuery] int? toYear,
        [FromQuery] int? groupStatus,
        [FromQuery] string format = "xlsx",
        [FromQuery] bool base64 = false,
        [FromQuery] string? donViTinh = null,
        [FromQuery] string? keyword = null,
        [FromQuery] string? projectType = null)
    {
        try
        {
            byte[] fileBytes;
            string contentType;
            string extension;
            string formatLower = format?.ToLower() ?? "xlsx";

            if (formatLower == "csv")
            {
                fileBytes = await reportService.ExportKeHoachVonCnttReportCsvAsync(fromYear, toYear, groupStatus, donViTinh, keyword, projectType);
                contentType = "text/csv";
                extension = "csv";
            }
            else if (formatLower == "html")
            {
                fileBytes = await reportService.ExportKeHoachVonCnttReportHtmlAsync(fromYear, toYear, groupStatus, donViTinh, keyword, projectType);
                contentType = "text/html";
                extension = "html";
            }
            else
            {
                fileBytes = await reportService.ExportKeHoachVonCnttReportExcelAsync(fromYear, toYear, groupStatus, donViTinh, keyword, projectType);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                extension = "xlsx";
            }

            int endY = toYear ?? DateTime.Now.Year;
            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = $"BaoCao_KeHoachVonCNTT_{endY}_{timestamp}.{extension}";

            if (base64)
            {
                var base64Data = Convert.ToBase64String(fileBytes);
                return Ok(new { fileName, contentType, base64Data });
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo kế hoạch vốn CNTT.", detail = ex.Message });
        }
    }

    #endregion

    #region 7. Báo cáo Quản lý Hạn License / Bảo trì & SLA Nhà thầu

    /// <summary>
    /// Lấy Báo cáo Quản lý Hạn License / Bảo trì &amp; SLA Nhà thầu.
    /// </summary>
    /// <param name="statusFilter">Bộ lọc trạng thái: 1 (Đã hết hạn), 2 (Sắp hết hạn), 3 (An toàn)</param>
    /// <param name="search">Từ khóa tìm kiếm</param>
    /// <param name="donViTinh">Đơn vị tính</param>
    [HttpGet("license-sla")]
    [HttpGet("han-license-bao-tri")]
    [HttpGet("/api/NghiepVu/report/license-sla")]
    [HttpGet("/api/NghiepVu/report/han-license-bao-tri")]
    [ProducesResponseType(typeof(LicenseSlaReportResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LicenseSlaReportResponseDto>> GetLicenseSlaReport(
        [FromQuery] int? statusFilter,
        [FromQuery] string? search,
        [FromQuery] string? donViTinh = null)
    {
        try
        {
            var report = await reportService.GetLicenseSlaReportAsync(statusFilter, search, donViTinh);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy báo cáo Hạn License & SLA nhà thầu.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Xuất file Báo cáo Quản lý Hạn License / Bảo trì &amp; SLA Nhà thầu (Excel, CSV, HTML).
    /// </summary>
    [HttpGet("license-sla/export")]
    [HttpGet("han-license-bao-tri/export")]
    [HttpGet("/api/NghiepVu/report/license-sla/export")]
    [HttpGet("/api/NghiepVu/report/han-license-bao-tri/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportLicenseSlaReport(
        [FromQuery] int? statusFilter,
        [FromQuery] string? search,
        [FromQuery] string format = "xlsx",
        [FromQuery] bool base64 = false,
        [FromQuery] string? donViTinh = null)
    {
        try
        {
            byte[] fileBytes;
            string contentType;
            string extension;
            string formatLower = format?.ToLower() ?? "xlsx";

            if (formatLower == "csv")
            {
                fileBytes = await reportService.ExportLicenseSlaReportCsvAsync(statusFilter, search, donViTinh);
                contentType = "text/csv";
                extension = "csv";
            }
            else if (formatLower == "html")
            {
                fileBytes = await reportService.ExportLicenseSlaReportHtmlAsync(statusFilter, search, donViTinh);
                contentType = "text/html";
                extension = "html";
            }
            else
            {
                fileBytes = await reportService.ExportLicenseSlaReportExcelAsync(statusFilter, search, donViTinh);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                extension = "xlsx";
            }

            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = $"BaoCao_LicenseSLA_{timestamp}.{extension}";

            if (base64)
            {
                var base64Data = Convert.ToBase64String(fileBytes);
                return Ok(new { fileName, contentType, base64Data });
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo License & SLA nhà thầu.", detail = ex.Message });
        }
    }

    #endregion
}
