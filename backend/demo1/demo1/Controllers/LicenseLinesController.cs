using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs.HangHoaDichVu;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý License Line Items trong Hợp đồng/Gói thầu.
/// </summary>
[Authorize]
[Route("api/NghiepVu/license-lines")]
public class LicenseLinesController : CrudControllerBase<HangHoaDichVuDto, CreateHangHoaDichVuDto, UpdateHangHoaDichVuDto>
{
    private readonly ILicenseLineService _licenseLineService;

    public LicenseLinesController(ILicenseLineService service) : base(service)
    {
        _licenseLineService = service;
    }

    /// <summary>
    /// Lấy danh sách License Line theo ID của đối tượng cha (vd: ID Hợp đồng hoặc ID Gói thầu).
    /// </summary>
    /// <param name="idParent">Mã định danh đối tượng cha (GUID)</param>
    /// <returns>Danh sách License Line</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("parent/{idParent:guid}")]
    [ProducesResponseType(typeof(IEnumerable<HangHoaDichVuDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HangHoaDichVuDto>>> GetByIdParent(Guid idParent)
    {
        var result = await _licenseLineService.GetByIdParentAsync(idParent);
        return Ok(result);
    }

    /// <summary>
    /// Thêm mới nhiều License Line cùng lúc.
    /// </summary>
    /// <param name="dtos">Danh sách dữ liệu License Line cần tạo</param>
    /// <returns>Danh sách License Line đã tạo</returns>
    /// <response code="200">Tạo danh sách thành công</response>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(IEnumerable<HangHoaDichVuDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HangHoaDichVuDto>>> CreateBatch([FromBody] IEnumerable<CreateHangHoaDichVuDto> dtos)
    {
        var result = await _licenseLineService.CreateRangeAsync(dtos);
        return Ok(result);
    }
}
