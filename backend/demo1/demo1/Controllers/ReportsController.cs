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
[ApiController]
[Route("api/report")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("investment")]
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

    [HttpGet("investment/export")]
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

    [HttpGet("cong-viec-goi-thau/{idGoiThau:guid}")]
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

    [HttpGet("cong-viec-goi-thau/{idGoiThau:guid}/export")]
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

    [HttpGet("contract-payments")]
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

    [HttpGet("contract-payments/export")]
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


