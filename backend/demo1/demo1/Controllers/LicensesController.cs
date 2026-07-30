using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Entity.DanhMuc;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý License Phần mềm (Danh sách License, Tạo mới, Tra cứu theo Dự án, Cảnh báo hết hạn và Thống kê).
/// </summary>
[Authorize]
[Route("api/NghiepVu/licenses")]
public class LicensesController : CrudControllerBase<LicenseDto, CreateLicenseDto, UpdateLicenseDto>
{
    private readonly ILicenseService _licenseService;

    public LicensesController(ILicenseService service) : base(service)
    {
        _licenseService = service;
    }

    /// <summary>
    /// Lấy danh sách các giá trị Enum quy định Loại License và Trạng thái License.
    /// </summary>
    /// <returns>Danh sách các Option Enum</returns>
    /// <response code="200">Lấy danh sách Enum thành công</response>
    [HttpGet("enums")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetLicenseEnums()
    {
        var loaiLicenseOptions = Enum.GetValues<LoaiLicense>()
            .Select(e => new { Value = (int)e, Code = e.ToString(), Label = e.GetDisplayName() });

        var trangThaiOptions = Enum.GetValues<TrangThaiLicense>()
            .Select(e => new { Value = (int)e, Code = e.ToString(), Label = e.GetDisplayName() });

        return Ok(new
        {
            LoaiLicenseOptions = loaiLicenseOptions,
            TrangThaiOptions = trangThaiOptions
        });
    }

    /// <summary>
    /// Tạo mới một License Phần mềm đơn lẻ.
    /// </summary>
    /// <param name="dto">Thông tin License mới</param>
    /// <returns>License vừa tạo</returns>
    /// <response code="200">Tạo mới thành công</response>
    /// <response code="400">Lỗi dữ liệu không hợp lệ</response>
    /// <response code="404">Không tìm thấy Dự án hoặc Hợp đồng liên quan</response>
    [HttpPost("single")]
    [ProducesResponseType(typeof(LicenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LicenseDto>> CreateSingle([FromBody] CreateLicenseDto dto)
    {
        try
        {
            var result = await _licenseService.CreateAsync(dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách License thuộc về một Dự án cụ thể.
    /// </summary>
    /// <param name="duAnId">Mã định danh Dự án (GUID)</param>
    /// <param name="search">Từ khóa tìm kiếm (tùy chọn)</param>
    /// <param name="page">Trang hiện tại (Mặc định: 1)</param>
    /// <param name="pageSize">Kích thước trang (Mặc định: 20)</param>
    /// <returns>Danh sách License phân trang</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("{duAnId:guid}")]
    [ProducesResponseType(typeof(PagedResult<LicenseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LicenseDto>>> GetByDuAnId(
        Guid duAnId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _licenseService.GetByDuAnIdAsync(duAnId, search, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách License sắp hết hạn trong khoảng số ngày chỉ định.
    /// </summary>
    /// <param name="daysThreshold">Số ngày ngưỡng hết hạn (Mặc định: 30 ngày)</param>
    /// <returns>Danh sách License sắp hết hạn</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("expiring")]
    [ProducesResponseType(typeof(IReadOnlyList<LicenseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LicenseDto>>> GetExpiringLicenses([FromQuery] int? daysThreshold)
    {
        var result = await _licenseService.GetExpiringLicensesAsync(daysThreshold);
        return Ok(result);
    }

    /// <summary>
    /// Lấy tổng quan thống kê số lượng và tình trạng License.
    /// </summary>
    /// <param name="duAnId">ID Dự án nếu muốn lọc theo Dự án cụ thể (tùy chọn)</param>
    /// <returns>Bảng tổng quan số lượng License</returns>
    /// <response code="200">Lấy thống kê thành công</response>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(LicenseSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LicenseSummaryDto>> GetLicenseSummary([FromQuery] Guid? duAnId)
    {
        var result = await _licenseService.GetLicenseSummaryAsync(duAnId);
        return Ok(result);
    }
}
