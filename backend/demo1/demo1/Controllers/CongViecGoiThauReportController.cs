using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Báo cáo Tiến độ Công việc Gói thầu.
/// </summary>
[Authorize]
[ApiController]
[Route("api/NghiepVu/report/cong-viec-goi-thau")]
public class CongViecGoiThauReportController(IReportService reportService) : ControllerBase
{
    /// <summary>
    /// Lấy báo cáo trình tự thực hiện các công việc thuộc Gói thầu.
    /// </summary>
    /// <param name="idGoiThau">Mã định danh Gói thầu (GUID)</param>
    /// <returns>Danh sách các bước công việc, tiến độ thực hiện và văn bản liên quan</returns>
    /// <response code="200">Lấy dữ liệu thành công</response>
    /// <response code="404">Không tìm thấy gói thầu</response>
    [HttpGet("{idGoiThau:guid}")]
    [ProducesResponseType(typeof(CongViecGoiThauReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CongViecGoiThauReportDto>> GetCongViecGoiThauReport(Guid idGoiThau)
    {
        try
        {
            var report = await reportService.GetCongViecGoiThauReportAsync(idGoiThau);
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
    [HttpGet("{idGoiThau:guid}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportCongViecGoiThauReport(Guid idGoiThau, [FromQuery] bool base64 = false)
    {
        try
        {
            var report = await reportService.GetCongViecGoiThauReportAsync(idGoiThau);
            var fileBytes = await reportService.ExportCongViecGoiThauReportExcelAsync(idGoiThau);

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
}
