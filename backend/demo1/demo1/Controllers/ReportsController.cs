using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace demo1.Controllers;

/// <summary>
/// API Báo cáo &amp; Thống kê Quản lý Hợp đồng &amp; Dự án.
/// Quy hoạch toàn bộ các API báo cáo dưới Base Route /api/NghiepVu/reports với hỗ trợ đầy đủ Legacy Route Aliases.
/// </summary>
[Authorize]
[FeatureAuthorize("BAO_CAO")]
[ApiController]
[Route("api/NghiepVu/reports")]
public class ReportsController(IReportService reportService, IWebHostEnvironment env, ILogger<ReportsController> logger) : ControllerBase
{
    #region 1. Báo cáo Tổng hợp Tình hình Đầu tư Dự án

    /// <summary>
    /// Lấy dữ liệu báo cáo tổng hợp tình hình thực hiện đầu tư (dự án, gói thầu, hợp đồng).
    /// </summary>
    /// <param name="year">Năm báo cáo (mặc định: năm hiện tại)</param>
    /// <param name="period">Kỳ báo cáo: 1 (6 tháng đầu năm), 2 (Cả năm)</param>
    /// <param name="donViTinh">Đơn vị tính (1 hoặc đồng: Đồng, 2 hoặc nghìn: Nghìn đồng, 3 hoặc triệu: Triệu đồng, 4 hoặc tỷ: Tỷ đồng)</param>
    /// <returns>Bảng tổng hợp kinh phí đầu tư và danh sách chi tiết các dự án</returns>
    [HttpGet("dau-tu", Name = "GetInvestmentReport")]
    [FeatureAuthorize("BAO_CAO_DAU_TU")]
    [ProducesResponseType(typeof(ReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReportResponseDto>> GetInvestmentReport(
        [FromQuery] int? year,
        [FromQuery] int period = 1,
        [FromQuery] string? donViTinh = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery(Name = "from_date")] DateTime? fromDateAlt = null,
        [FromQuery(Name = "to_date")] DateTime? toDateAlt = null,
        [FromQuery(Name = "from")] DateTime? fromAlt = null,
        [FromQuery(Name = "to")] DateTime? toAlt = null)
    {
        var effectiveFromDate = fromDate ?? fromDateAlt ?? fromAlt;
        var effectiveToDate = toDate ?? toDateAlt ?? toAlt;
        int selectedYear = year ?? (effectiveFromDate?.Year ?? DateTime.UtcNow.Year);

        if (!IsValidPeriod(period, effectiveFromDate.HasValue || effectiveToDate.HasValue))
        {
            return BadRequest(new { message = "Kỳ báo cáo không hợp lệ." });
        }

        try
        {
            var report = await reportService.GetInvestmentReportAsync(selectedYear, period, donViTinh, effectiveFromDate, effectiveToDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo báo cáo đầu tư.", detail = env.IsDevelopment() ? ex.Message : null });
        }
    }

    /// <summary>
    /// Xuất file báo cáo đầu tư (Excel, CSV, HTML hoặc chuỗi mã hóa Base64).
    /// </summary>
    /// <param name="year">Năm báo cáo</param>
    /// <param name="period">Kỳ báo cáo</param>
    /// <param name="format">Định dạng xuất: xlsx, csv, html (mặc định: xlsx)</param>
    /// <param name="base64">Trả về chuỗi Base64 thay vì download file trực tiếp</param>
    /// <param name="donViTinh">Đơn vị tính (mặc định: đồng, các giá trị khác: triệu, tỷ, nghìn)</param>
    /// <param name="version">Phiên bản mẫu xuất</param>
    /// <param name="fromDate">Từ ngày</param>
    /// <param name="toDate">Đến ngày</param>
    /// <param name="fromDateAlt">Từ ngày (tùy chọn 2)</param>
    /// <param name="toDateAlt">Đến ngày (tùy chọn 2)</param>
    /// <param name="fromAlt">Từ ngày (tùy chọn 3)</param>
    /// <param name="toAlt">Đến ngày (tùy chọn 3)</param>
    [HttpGet("dau-tu/export", Name = "ExportInvestmentReport")]
    [FeatureAuthorize("BAO_CAO_DAU_TU")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportInvestmentReport(
        [FromQuery] int? year,
        [FromQuery] int period = 1,
        [FromQuery] string format = "xlsx",
        [FromQuery] bool base64 = false,
        [FromQuery] string? donViTinh = null,
        [FromQuery] int version = 1,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery(Name = "from_date")] DateTime? fromDateAlt = null,
        [FromQuery(Name = "to_date")] DateTime? toDateAlt = null,
        [FromQuery(Name = "from")] DateTime? fromAlt = null,
        [FromQuery(Name = "to")] DateTime? toAlt = null)
    {
        var effectiveFromDate = fromDate ?? fromDateAlt ?? fromAlt;
        var effectiveToDate = toDate ?? toDateAlt ?? toAlt;
        int selectedYear = year ?? (effectiveFromDate?.Year ?? DateTime.UtcNow.Year);

        if (!IsValidPeriod(period, effectiveFromDate.HasValue || effectiveToDate.HasValue))
        {
            return BadRequest(new { message = "Kỳ báo cáo không hợp lệ." });
        }

        try
        {
            byte[] fileBytes;
            string contentType;
            string extension;
            string formatLower = format?.ToLower() ?? "xlsx";

            if (formatLower == "csv")
            {
                fileBytes = await reportService.ExportInvestmentReportCsvAsync(selectedYear, period, donViTinh, effectiveFromDate, effectiveToDate);
                contentType = "text/csv";
                extension = "csv";
            }
            else if (formatLower == "html")
            {
                fileBytes = await reportService.ExportInvestmentReportHtmlAsync(selectedYear, period, donViTinh, effectiveFromDate, effectiveToDate);
                contentType = "text/html";
                extension = "html";
            }
            else
            {
                fileBytes = await reportService.ExportInvestmentReportExcelAsync(selectedYear, period, donViTinh, version, effectiveFromDate, effectiveToDate);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                extension = "xlsx";
            }

            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string periodLabel = (effectiveFromDate.HasValue || effectiveToDate.HasValue || period == 0) ? "TuyChon" : $"K{period}";
            string fileName = version == 2
                ? $"Bao_cao_Tien_do_Du_an_{timestamp}.{extension}"
                : $"BaoCaoDauTu_{selectedYear}_{periodLabel}_{timestamp}.{extension}";

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
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo đầu tư.", detail = env.IsDevelopment() ? ex.Message : null });
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
    [HttpGet("thanh-toan-hop-dong", Name = "GetContractPaymentReport")]
    [FeatureAuthorize("BAO_CAO_THANH_TOAN")]
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
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy báo cáo theo dõi thanh toán hợp đồng.", detail = env.IsDevelopment() ? ex.Message : null });
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
    [HttpGet("thanh-toan-hop-dong/export", Name = "ExportContractPaymentReport")]
    [FeatureAuthorize("BAO_CAO_THANH_TOAN")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportContractPaymentReport(
        [FromQuery] int? year,
        [FromQuery] int? loaiHopDong,
        [FromQuery] List<Guid>? loaiHopDongIds,
        [FromQuery] string? search,
        [FromQuery] string format = "xlsx",
        [FromQuery] bool base64 = false,
        [FromQuery] string? donViTinh = null,
        [FromQuery] int version = 1)
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
                fileBytes = await reportService.ExportContractPaymentReportExcelAsync(selectedYear, loaiHopDong, loaiHopDongIds, search, donViTinh, version);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                extension = "xlsx";
            }

            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = version == 2
                ? $"Bao_cao_Dot_thanh_toan_{timestamp}.{extension}"
                : $"BaoCao_TheoDoiThanhToanHopDong_{selectedYear}_{timestamp}.{extension}";

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
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo theo dõi thanh toán hợp đồng.", detail = env.IsDevelopment() ? ex.Message : null });
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
    [HttpGet("theo-doi-hop-dong", Name = "GetTheoDoiHopDongReport")]
    [FeatureAuthorize("BAO_CAO_HOP_DONG")]
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
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy báo cáo theo dõi hợp đồng.", detail = env.IsDevelopment() ? ex.Message : null });
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
    [HttpGet("theo-doi-hop-dong/export", Name = "ExportTheoDoiHopDongReport")]
    [FeatureAuthorize("BAO_CAO_HOP_DONG")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportTheoDoiHopDongReport(
        [FromQuery] int? year,
        [FromQuery] DateTime? cutoffDate,
        [FromQuery] List<Guid>? loaiHopDongIds,
        [FromQuery] string? search,
        [FromQuery] string format = "xlsx",
        [FromQuery] bool base64 = false,
        [FromQuery] string? donViTinh = null,
        [FromQuery] int version = 1)
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
                fileBytes = await reportService.ExportTheoDoiHopDongReportExcelAsync(year, cutoffDate, loaiHopDongIds, search, donViTinh, version);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                extension = "xlsx";
            }

            int selectedYear = year ?? DateTime.Now.Year;
            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = version == 2
                ? $"Bao_cao_Quan_ly_Hop_dong_{timestamp}.{extension}"
                : $"BaoCao_TheoDoiHopDong_{selectedYear}_{timestamp}.{extension}";

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
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo theo dõi hợp đồng.", detail = env.IsDevelopment() ? ex.Message : null });
        }
    }

    #endregion

    #region 4. Báo cáo Tiến độ Công việc Gói thầu

    /// <summary>
    /// Lấy báo cáo trình tự thực hiện các công việc thuộc Gói thầu.
    /// </summary>
    /// <param name="idGoiThau">Mã định danh Gói thầu (GUID)</param>
    /// <param name="donViTinh">Đơn vị tính (1 hoặc đồng: Đồng, 2 hoặc nghìn: Nghìn đồng, 3 hoặc triệu: Triệu đồng, 4 hoặc tỷ: Tỷ đồng)</param>
    [HttpGet("cong-viec-goi-thau/{idGoiThau:guid}", Name = "GetCongViecGoiThauReport")]
    [FeatureAuthorize("BAO_CAO_TIEN_DO")]
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
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy báo cáo công việc gói thầu.", detail = env.IsDevelopment() ? ex.Message : null });
        }
    }

    /// <summary>
    /// Xuất file báo cáo tiến độ công việc gói thầu (Excel, CSV, HTML).
    /// </summary>
    /// <param name="idGoiThau">Mã định danh Gói thầu (GUID)</param>
    /// <param name="format">Định dạng xuất: xlsx, csv, html</param>
    /// <param name="base64">Trả về dữ liệu Base64 thay vì file trực tiếp</param>
    /// <param name="donViTinh">Đơn vị tính (1 hoặc đồng: Đồng, 2 hoặc nghìn: Nghìn đồng, 3 hoặc triệu: Triệu đồng, 4 hoặc tỷ: Tỷ đồng)</param>
    [HttpGet("cong-viec-goi-thau/{idGoiThau:guid}/export", Name = "ExportCongViecGoiThauReport")]
    [FeatureAuthorize("BAO_CAO_TIEN_DO")]
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
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo công việc gói thầu.", detail = env.IsDevelopment() ? ex.Message : null });
        }
    }

    #endregion

    #region 5. Báo cáo Kế hoạch vốn Đầu tư & Mua sắm (Mẫu Phụ lục 01-05 & Phụ biểu 01)

    /// <summary>
    /// Lấy dữ liệu Báo cáo Kế hoạch vốn Đầu tư &amp; Mua sắm (Theo mẫu Phụ lục 01-05 &amp; Phụ biểu 01).
    /// </summary>
    [HttpGet("ke-hoach-von", Name = "GetKeHoachVonReport")]
    [FeatureAuthorize("BAO_CAO_VON")]
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
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy báo cáo kế hoạch vốn.", detail = env.IsDevelopment() ? ex.Message : null });
        }
    }

    /// <summary>
    /// Xuất file Báo cáo Kế hoạch vốn Đầu tư &amp; Mua sắm (Excel, CSV, HTML).
    /// </summary>
    [HttpGet("ke-hoach-von/export", Name = "ExportKeHoachVonReport")]
    [FeatureAuthorize("BAO_CAO_VON")]
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
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo kế hoạch vốn.", detail = env.IsDevelopment() ? ex.Message : null });
        }
    }

    #endregion

    #region 6. Báo cáo Tổng hợp & Phân kỳ Kế hoạch vốn CNTT Giai đoạn (Nghị quyết 16-NQ-NHHT)

    /// <summary>
    /// Lấy Báo cáo Tổng hợp &amp; Phân kỳ Kế hoạch vốn CNTT Giai đoạn.
    /// </summary>
    [HttpGet("ke-hoach-von-cntt", Name = "GetKeHoachVonCnttReport")]
    [FeatureAuthorize("BAO_CAO_VON")]
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
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy báo cáo kế hoạch vốn CNTT.", detail = env.IsDevelopment() ? ex.Message : null });
        }
    }

    /// <summary>
    /// Xuất file Báo cáo Kế hoạch vốn CNTT Giai đoạn (Excel, CSV, HTML).
    /// </summary>
    [HttpGet("ke-hoach-von-cntt/export", Name = "ExportKeHoachVonCnttReport")]
    [FeatureAuthorize("BAO_CAO_VON")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportKeHoachVonCnttReport(
        [FromQuery] int? fromYear,
        [FromQuery] int? toYear,
        [FromQuery] int? groupStatus,
        [FromQuery] string format = "xlsx",
        [FromQuery] bool base64 = false,
        [FromQuery] string? donViTinh = null,
        [FromQuery] string? keyword = null,
        [FromQuery] string? projectType = null,
        [FromQuery] int version = 1)
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
                fileBytes = await reportService.ExportKeHoachVonCnttReportExcelAsync(fromYear, toYear, groupStatus, donViTinh, keyword, projectType, version);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                extension = "xlsx";
            }

            int endY = toYear ?? DateTime.Now.Year;
            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = version == 2
                ? $"Bao_cao_Phan_bo_va_Ke_hoach_Von_{timestamp}.{extension}"
                : $"BaoCao_KeHoachVonCNTT_{endY}_{timestamp}.{extension}";

            if (base64)
            {
                var base64Data = Convert.ToBase64String(fileBytes);
                return Ok(new { fileName, contentType, base64Data });
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo kế hoạch vốn CNTT.", detail = env.IsDevelopment() ? ex.Message : null });
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
    [HttpGet("license-sla", Name = "GetLicenseSlaReport")]
    [FeatureAuthorize("BAO_CAO_PHE_DUYET")]
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
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy báo cáo Hạn License & SLA nhà thầu.", detail = env.IsDevelopment() ? ex.Message : null });
        }
    }

    /// <summary>
    /// Xuất file Báo cáo Quản lý Hạn License / Bảo trì &amp; SLA Nhà thầu (Excel, CSV, HTML).
    /// </summary>
    [HttpGet("license-sla/export", Name = "ExportLicenseSlaReport")]
    [FeatureAuthorize("BAO_CAO_PHE_DUYET")]
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
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo License & SLA nhà thầu.", detail = env.IsDevelopment() ? ex.Message : null });
        }
    }

    #endregion

    #region 8. Báo cáo Kế hoạch & Kết quả Lựa chọn Nhà thầu (Gói thầu) LCNT (Mẫu Báo cáo 3)

    /// <summary>
    /// Lấy Báo cáo Kế hoạch &amp; Kết quả Lựa chọn Nhà thầu (Gói thầu) LCNT.
    /// </summary>
    /// <param name="year">Năm triển khai / phê duyệt</param>
    /// <param name="duAnId">Lọc theo Dự án triển khai</param>
    /// <param name="search">Từ khóa tìm kiếm gói thầu hoặc dự án</param>
    /// <param name="donViTinh">Đơn vị tính (1 hoặc đồng: Đồng, 2 hoặc nghìn: Nghìn đồng, 3 hoặc triệu: Triệu đồng, 4 hoặc tỷ: Tỷ đồng)</param>
    [HttpGet("goi-thau-lcnt", Name = "GetGoiThauLcntReport")]
    [FeatureAuthorize("BAO_CAO_DAU_THAU")]
    [ProducesResponseType(typeof(GoiThauLcntReportResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GoiThauLcntReportResponseDto>> GetGoiThauLcntReport(
        [FromQuery] int? year,
        [FromQuery] Guid? duAnId,
        [FromQuery] string? search,
        [FromQuery] string? donViTinh = null)
    {
        try
        {
            var report = await reportService.GetGoiThauLcntReportAsync(year, duAnId, search, donViTinh);
            return Ok(report);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy báo cáo kế hoạch và kết quả lựa chọn nhà thầu.", detail = env.IsDevelopment() ? ex.Message : null });
        }
    }

    /// <summary>
    /// Xuất file Báo cáo Lựa chọn Nhà thầu (LCNT) ra Excel.
    /// </summary>
    [HttpGet("goi-thau-lcnt/export", Name = "ExportGoiThauLcntReport")]
    [FeatureAuthorize("BAO_CAO_DAU_THAU")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportGoiThauLcntReport(
        [FromQuery] int? year,
        [FromQuery] Guid? duAnId,
        [FromQuery] string? search,
        [FromQuery] string format = "xlsx",
        [FromQuery] bool base64 = false,
        [FromQuery] string? donViTinh = null,
        [FromQuery] int version = 2)
    {
        try
        {
            byte[] fileBytes = await reportService.ExportGoiThauLcntReportExcelAsync(year, duAnId, search, donViTinh, version);
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            string extension = "xlsx";

            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = $"Bao_cao_Lua_chon_Nha_thau_LCNT_{timestamp}.{extension}";

            if (base64)
            {
                var base64Data = Convert.ToBase64String(fileBytes);
                return Ok(new { fileName, contentType, base64Data });
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo lựa chọn nhà thầu.", detail = env.IsDevelopment() ? ex.Message : null });
        }
    }


    #region 9. Báo cáo Theo dõi Tiến độ Thanh toán các Dự án Thầu (Mẫu Excel)

    /// <summary>
    /// Lấy dữ liệu Báo cáo Theo dõi Tiến độ Thanh toán các Dự án Thầu.
    /// </summary>
    /// <param name="year">Năm ký / hiệu lực hợp đồng</param>
    /// <param name="duAnId">Mã Dự án cần lọc (nếu có)</param>
    /// <param name="search">Từ khóa tìm kiếm (Dự án, Gói thầu, Nhà thầu, Hợp đồng)</param>
    /// <param name="donViTinh">Đơn vị tính (đồng, triệu...)</param>
    [HttpGet("tien-do-thanh-toan-du-an-thau", Name = "GetTienDoThanhToanDuAnThauReport")]
    [FeatureAuthorize("BAO_CAO_DU_AN_THAU")]
    [ProducesResponseType(typeof(TienDoThanhToanDuAnThauReportResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TienDoThanhToanDuAnThauReportResponseDto>> GetTienDoThanhToanDuAnThauReport(
        [FromQuery] int? year,
        [FromQuery] Guid? duAnId,
        [FromQuery] string? search,
        [FromQuery] string? donViTinh = null)
    {
        try
        {
            var report = await reportService.GetTienDoThanhToanDuAnThauReportAsync(year, duAnId, search, donViTinh);
            return Ok(report);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy báo cáo tiến độ thanh toán dự án thầu.", detail = env.IsDevelopment() ? ex.Message : null });
        }
    }

    /// <summary>
    /// Lấy danh sách tùy chọn bộ lọc dự án (Dropdown filter options) cho Báo cáo Theo dõi Tiến độ Thanh toán các Dự án Thầu.
    /// </summary>
    [HttpGet("tien-do-thanh-toan-du-an-thau/filter-options", Name = "GetTienDoThanhToanFilterOptions")]
    [FeatureAuthorize("BAO_CAO_DU_AN_THAU")]
    [ProducesResponseType(typeof(IReadOnlyList<DuAnLookupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DuAnLookupDto>>> GetTienDoThanhToanFilterOptions()
    {
        try
        {
            var options = await reportService.GetTienDoThanhToanFilterOptionsAsync();
            return Ok(options);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi API Reports filter-options: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy tùy chọn bộ lọc dự án.", detail = env.IsDevelopment() ? ex.Message : null });
        }
    }

    /// <summary>
    /// Xuất file Báo cáo Theo dõi Tiến độ Thanh toán các Dự án Thầu ra Excel / CSV / HTML / Base64.
    /// </summary>
    [HttpGet("tien-do-thanh-toan-du-an-thau/export", Name = "ExportTienDoThanhToanDuAnThauReport")]
    [FeatureAuthorize("BAO_CAO_DU_AN_THAU")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportTienDoThanhToanDuAnThauReport(
        [FromQuery] int? year,
        [FromQuery] Guid? duAnId,
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
                fileBytes = await reportService.ExportTienDoThanhToanDuAnThauReportCsvAsync(year, duAnId, search, donViTinh);
                contentType = "text/csv";
                extension = "csv";
            }
            else if (formatLower == "html")
            {
                fileBytes = await reportService.ExportTienDoThanhToanDuAnThauReportHtmlAsync(year, duAnId, search, donViTinh);
                contentType = "text/html";
                extension = "html";
            }
            else
            {
                fileBytes = await reportService.ExportTienDoThanhToanDuAnThauReportExcelAsync(year, duAnId, search, donViTinh);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                extension = "xlsx";
            }

            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = $"BaoCao_TienDoThanhToan_DuAnThau_{timestamp}.{extension}";

            if (base64)
            {
                var base64Data = Convert.ToBase64String(fileBytes);
                return Ok(new { fileName, contentType, base64Data });
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi API Reports: {Message}", ex.Message);
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo tiến độ thanh toán dự án thầu.", detail = env.IsDevelopment() ? ex.Message : null });
        }
    }

    #endregion

    private static bool IsValidPeriod(int period, bool hasCustomDates)
    {
        if (hasCustomDates || period == 0) return true;
        if (period >= 1 && period <= 7) return true;
        if (period >= 11 && period <= 22) return true;
        return false;
    }

    #endregion
}
