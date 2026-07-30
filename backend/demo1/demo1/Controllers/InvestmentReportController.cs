using System;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Báo cáo Tổng hợp Tình hình Đầu tư Dự án.
/// </summary>
[Authorize]
[ApiController]
[Route("api/NghiepVu/report/investment")]
public class InvestmentReportController(IReportService reportService) : ControllerBase
{
    /// <summary>
    /// Lấy dữ liệu báo cáo tổng hợp tình hình thực hiện đầu tư (dự án, gói thầu, hợp đồng).
    /// </summary>
    /// <param name="year">Năm báo cáo (mặc định: năm hiện tại)</param>
    /// <param name="period">Kỳ báo cáo: 1 (6 tháng đầu năm), 2 (Cả năm)</param>
    /// <returns>Bảng tổng hợp kinh phí đầu tư và danh sách chi tiết các dự án</returns>
    /// <response code="200">Lấy dữ liệu báo cáo thành công</response>
    /// <response code="400">Kỳ báo cáo không hợp lệ</response>
    [HttpGet]
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
            var report = await reportService.GetInvestmentReportAsync(selectedYear, period);
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
    [HttpGet("export")]
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
            var report = await reportService.GetInvestmentReportAsync(selectedYear, period);
            
            byte[] fileBytes;
            string contentType;
            string extension;
            string formatLower = format?.ToLower() ?? "xlsx";

            if (formatLower == "csv")
            {
                fileBytes = await reportService.ExportInvestmentReportCsvAsync(selectedYear, period);
                contentType = "text/csv";
                extension = "csv";
            }
            else if (formatLower == "html")
            {
                fileBytes = await reportService.ExportInvestmentReportHtmlAsync(selectedYear, period);
                contentType = "text/html";
                extension = "html";
            }
            else
            {
                fileBytes = await reportService.ExportInvestmentReportExcelAsync(selectedYear, period);
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
}
