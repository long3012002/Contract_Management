using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniExcelLibs;

namespace demo1.Controllers;

//[Authorize]
/// <summary>
/// API Báo cáo &amp; Thống kê (Báo cáo tổng mức đầu tư dự án, Tiến độ công việc gói thầu, Báo cáo theo dõi thanh toán hợp đồng và xuất Excel/CSV/HTML).
/// </summary>
[ApiController]
[Route("api/NghiepVu/report")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Lấy dữ liệu báo cáo tổng hợp tình hình thực hiện đầu tư (dự án, gói thầu, hợp đồng).
    /// </summary>
    /// <param name="year">Năm báo cáo (mặc định: năm hiện tại)</param>
    /// <param name="period">Kỳ báo cáo: 1 (6 tháng đầu năm), 2 (Cả năm)</param>
    /// <returns>Bảng tổng hợp kinh phí đầu tư và danh sách chi tiết các dự án</returns>
    /// <response code="200">Lấy dữ liệu báo cáo thành công</response>
    /// <response code="400">Kỳ báo cáo không hợp lệ</response>
    [HttpGet("investment")]
    [ProducesResponseType(typeof(ReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReportResponseDto>> GetInvestmentReport([FromQuery] int? year, [FromQuery] int period = 1)
    {
        int selectedYear = year ?? DateTime.UtcNow.Year;

        if (period != 1 && period != 2)
        {
            return BadRequest(new { message = "Kỳ báo cáo không hợp lệ. Chỉ chấp nhận 1 (6 tháng đầu năm) hoặc 2 (1 năm)." });
        }

        try
        {
            var report = await _reportService.GetInvestmentReportAsync(selectedYear, period);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo báo cáo.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Xuất file báo cáo đầu tư (Excel, CSV, HTML hoặc chuỗi mã hóa Base64).
    /// </summary>
    /// <param name="year">Năm báo cáo</param>
    /// <param name="period">Kỳ báo cáo: 1 (6 tháng), 2 (1 năm)</param>
    /// <param name="format">Định dạng xuất: xlsx, csv, html (mặc định: xlsx)</param>
    /// <param name="base64">Trả về chuỗi Base64 thay vì download file trực tiếp</param>
    /// <response code="200">Xuất file thành công</response>
    [HttpGet("investment/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportInvestmentReport([FromQuery] int? year, [FromQuery] int period = 1, [FromQuery] string format = "xlsx", [FromQuery] bool base64 = false)
    {
        int selectedYear = year ?? DateTime.UtcNow.Year;

        if (period != 1 && period != 2)
        {
            return BadRequest(new { message = "Kỳ báo cáo không hợp lệ. Chỉ chấp nhận 1 (6 tháng đầu năm) hoặc 2 (1 năm)." });
        }

        try
        {
            var report = await _reportService.GetInvestmentReportAsync(selectedYear, period);
            
            byte[] fileBytes;
            string contentType;
            string extension;
            string formatLower = format?.ToLower() ?? "xlsx";

            if (formatLower == "csv")
            {
                fileBytes = await _reportService.ExportInvestmentReportCsvAsync(selectedYear, period);
                contentType = "text/csv";
                extension = "csv";
            }
            else if (formatLower == "html")
            {
                fileBytes = await _reportService.ExportInvestmentReportHtmlAsync(selectedYear, period);
                contentType = "text/html";
                extension = "html";
            }
            else
            {
                fileBytes = await _reportService.ExportInvestmentReportExcelAsync(selectedYear, period);
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
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xuất báo cáo.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Lấy báo cáo trình tự thực hiện các công việc thuộc Gói thầu.
    /// </summary>
    /// <param name="idGoiThau">Mã định danh Gói thầu (GUID)</param>
    /// <returns>Danh sách các bước công việc, tiến độ thực hiện và văn bản liên quan</returns>
    /// <response code="200">Lấy dữ liệu thành công</response>
    /// <response code="404">Không tìm thấy gói thầu</response>
    [HttpGet("cong-viec-goi-thau/{idGoiThau:guid}")]
    [ProducesResponseType(typeof(CongViecGoiThauReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CongViecGoiThauReportDto>> GetCongViecGoiThauReport(Guid idGoiThau)
    {
        try
        {
            var report = await _reportService.GetCongViecGoiThauReportAsync(idGoiThau);
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
    /// Xuất file Excel báo cáo tiến độ công việc gói thầu.
    /// </summary>
    /// <param name="idGoiThau">Mã định danh Gói thầu (GUID)</param>
    /// <param name="base64">Trả về dữ liệu Base64 thay vì file trực tiếp</param>
    /// <response code="200">Xuất file Excel thành công</response>
    /// <response code="404">Không tìm thấy gói thầu</response>
    [HttpGet("cong-viec-goi-thau/{idGoiThau:guid}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportCongViecGoiThauReport(Guid idGoiThau, [FromQuery] bool base64 = false)
    {
        try
        {
            var report = await _reportService.GetCongViecGoiThauReportAsync(idGoiThau);
            var fileBytes = await _reportService.ExportCongViecGoiThauReportExcelAsync(idGoiThau);

            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = $"BaoCao_TrinhTuThucHien_{report.MaGoiThau}_{timestamp}.xlsx";

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

    /// <summary>
    /// Lấy báo cáo theo dõi giải ngân / đợt thanh toán hợp đồng.
    /// </summary>
    /// <param name="year">Năm thanh toán</param>
    /// <param name="loaiHopDong">Loại hợp đồng</param>
    /// <param name="search">Từ khóa tìm kiếm</param>
    /// <returns>Danh sách các đợt thanh toán hợp đồng và tổng hợp giá trị đã thanh toán</returns>
    /// <response code="200">Lấy dữ liệu thành công</response>
    [HttpGet("contract-payments")]
    [ProducesResponseType(typeof(ContractPaymentReportResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ContractPaymentReportResponseDto>> GetContractPaymentReport(
        [FromQuery] int? year, 
        [FromQuery] int? loaiHopDong, 
        [FromQuery] string? search)
    {
        int selectedYear = year ?? DateTime.UtcNow.Year;

        try
        {
            var report = await _reportService.GetContractPaymentReportAsync(selectedYear, loaiHopDong, search);
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
    /// <param name="search">Từ khóa tìm kiếm</param>
    /// <param name="format">Định dạng xuất: xlsx, csv, html</param>
    /// <param name="base64">Trả về dạng mã hóa Base64</param>
    /// <response code="200">Xuất file thành công</response>
    [HttpGet("contract-payments/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportContractPaymentReport(
        [FromQuery] int? year, 
        [FromQuery] int? loaiHopDong, 
        [FromQuery] string? search, 
        [FromQuery] string format = "xlsx", 
        [FromQuery] bool base64 = false)
    {
        int selectedYear = year ?? DateTime.UtcNow.Year;

        try
        {
            var report = await _reportService.GetContractPaymentReportAsync(selectedYear, loaiHopDong, search);
            
            byte[] fileBytes;
            string contentType;
            string extension;
            string formatLower = format?.ToLower() ?? "xlsx";

            if (formatLower == "csv")
            {
                fileBytes = await _reportService.ExportContractPaymentReportCsvAsync(selectedYear, loaiHopDong, search);
                contentType = "text/csv";
                extension = "csv";
            }
            else if (formatLower == "html")
            {
                fileBytes = await _reportService.ExportContractPaymentReportHtmlAsync(selectedYear, loaiHopDong, search);
                contentType = "text/html";
                extension = "html";
            }
            else
            {
                fileBytes = await _reportService.ExportContractPaymentReportExcelAsync(selectedYear, loaiHopDong, search);
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
}


