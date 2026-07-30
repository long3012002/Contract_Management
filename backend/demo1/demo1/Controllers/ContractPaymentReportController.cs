using System;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Báo cáo Theo dõi Giải ngân &amp; Thanh toán Hợp đồng.
/// </summary>
[Authorize]
[ApiController]
[Route("api/NghiepVu/report/contract-payments")]
public class ContractPaymentReportController(IReportService reportService) : ControllerBase
{
    /// <summary>
    /// Lấy báo cáo theo dõi giải ngân / đợt thanh toán hợp đồng.
    /// </summary>
    /// <param name="year">Năm thanh toán</param>
    /// <param name="loaiHopDong">Loại hợp đồng</param>
    /// <param name="search">Từ khóa tìm kiếm</param>
    /// <returns>Danh sách các đợt thanh toán hợp đồng và tổng hợp giá trị đã thanh toán</returns>
    /// <response code="200">Lấy dữ liệu thành công</response>
    [HttpGet]
    [ProducesResponseType(typeof(ContractPaymentReportResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ContractPaymentReportResponseDto>> GetContractPaymentReport(
        [FromQuery] int? year, 
        [FromQuery] int? loaiHopDong, 
        [FromQuery] string? search)
    {
        int selectedYear = year ?? DateTime.UtcNow.Year;

        try
        {
            var report = await reportService.GetContractPaymentReportAsync(selectedYear, loaiHopDong, search);
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
    [HttpGet("export")]
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
            var report = await reportService.GetContractPaymentReportAsync(selectedYear, loaiHopDong, search);
            
            byte[] fileBytes;
            string contentType;
            string extension;
            string formatLower = format?.ToLower() ?? "xlsx";

            if (formatLower == "csv")
            {
                fileBytes = await reportService.ExportContractPaymentReportCsvAsync(selectedYear, loaiHopDong, search);
                contentType = "text/csv";
                extension = "csv";
            }
            else if (formatLower == "html")
            {
                fileBytes = await reportService.ExportContractPaymentReportHtmlAsync(selectedYear, loaiHopDong, search);
                contentType = "text/html";
                extension = "html";
            }
            else
            {
                fileBytes = await reportService.ExportContractPaymentReportExcelAsync(selectedYear, loaiHopDong, search);
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
