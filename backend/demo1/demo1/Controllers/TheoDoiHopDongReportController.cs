using System;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Báo cáo Theo dõi Hợp đồng (Dựa trên mẫu Theo_dõi_HĐ.xlsx)
/// </summary>
[Authorize]
[ApiController]
[Route("api/NghiepVu/report/theo-doi-hop-dong")]
public class TheoDoiHopDongReportController(IReportService reportService) : ControllerBase
{
    /// <summary>
    /// Lấy báo cáo theo dõi hợp đồng và các đợt thanh toán liên quan (Mẫu Theo_dõi_HĐ.xlsx).
    /// </summary>
    /// <param name="year">Năm báo cáo</param>
    /// <param name="cutoffDate">Mốc thời gian dự kiến thanh toán đến (Mặc định 31/12/{year})</param>
    /// <param name="loaiHopDong">Phân loại hợp đồng</param>
    /// <param name="search">Từ khóa tìm kiếm</param>
    /// <returns>Báo cáo chi tiết các hợp đồng và các đợt thanh toán</returns>
    [HttpGet]
    [HttpGet("/api/NghiepVu/reportTheoDoiHopDong")]
    [ProducesResponseType(typeof(TheoDoiHopDongReportResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TheoDoiHopDongReportResponseDto>> GetTheoDoiHopDongReport(
        [FromQuery] int? year,
        [FromQuery] DateTime? cutoffDate,
        [FromQuery] int? loaiHopDong,
        [FromQuery] string? search)
    {
        try
        {
            var report = await reportService.GetTheoDoiHopDongReportAsync(year, cutoffDate, loaiHopDong, search);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy báo cáo theo dõi hợp đồng.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Xuất file Excel báo cáo theo dõi hợp đồng theo mẫu Theo_dõi_HĐ.xlsx.
    /// </summary>
    /// <param name="year">Năm báo cáo</param>
    /// <param name="cutoffDate">Mốc thời gian dự kiến thanh toán đến (Mặc định 31/12/{year})</param>
    /// <param name="loaiHopDong">Phân loại hợp đồng</param>
    /// <param name="search">Từ khóa tìm kiếm</param>
    /// <param name="base64">Trả về dạng mã hóa Base64 thay vì file nhị phân</param>
    /// <returns>Tệp tin Excel (.xlsx) hoặc JSON Base64</returns>
    [HttpGet("export")]
    [HttpGet("/api/NghiepVu/reportTheoDoiHopDong/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportTheoDoiHopDongReport(
        [FromQuery] int? year,
        [FromQuery] DateTime? cutoffDate,
        [FromQuery] int? loaiHopDong,
        [FromQuery] string? search,
        [FromQuery] bool base64 = false)
    {
        try
        {
            byte[] fileBytes = await reportService.ExportTheoDoiHopDongReportExcelAsync(year, cutoffDate, loaiHopDong, search);
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            int selectedYear = year ?? DateTime.Now.Year;
            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = $"BaoCao_TheoDoiHopDong_{selectedYear}_{timestamp}.xlsx";

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
}
