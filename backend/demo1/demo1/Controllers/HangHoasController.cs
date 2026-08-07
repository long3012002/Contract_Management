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
/// API Quản lý Hàng hóa / Thiết bị (Danh sách hàng hóa, Thêm mới hàng loạt, Tra cứu theo Hợp đồng/Gói thầu).
/// </summary>
[Authorize]
[Route("api/NghiepVu/hang-hoa")]
public class HangHoasController : CrudControllerBase<HangHoaDichVuDto, CreateHangHoaDichVuDto, UpdateHangHoaDichVuDto>
{
    private readonly IHangHoaService _hangHoaService;

    public HangHoasController(IHangHoaService service) : base(service)
    {
        _hangHoaService = service;
    }

    /// <summary>
    /// Lấy danh sách Hàng hóa theo ID của đối tượng cha (vd: ID Hợp đồng hoặc ID Gói thầu).
    /// </summary>
    /// <param name="idParent">Mã định danh đối tượng cha (GUID)</param>
    /// <returns>Danh sách hàng hóa</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("parent/{idParent:guid}")]
    [ProducesResponseType(typeof(IEnumerable<HangHoaDichVuDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HangHoaDichVuDto>>> GetByIdParent(Guid idParent)
    {
        var result = await _hangHoaService.GetByIdParentAsync(idParent);
        return Ok(result);
    }

    /// <summary>
    /// Thêm mới nhiều Hàng hóa cùng lúc.
    /// </summary>
    /// <param name="dtos">Danh sách dữ liệu hàng hóa cần tạo</param>
    /// <returns>Danh sách hàng hóa đã tạo</returns>
    /// <response code="200">Tạo danh sách hàng hóa thành công</response>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(IEnumerable<HangHoaDichVuDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HangHoaDichVuDto>>> CreateBatch([FromBody] IEnumerable<CreateHangHoaDichVuDto> dtos)
    {
        var result = await _hangHoaService.CreateRangeAsync(dtos);
        return Ok(result);
    }
}
