using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs.HangHoaDichVu;
using demo1.Entity;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Hợp nhất Hàng hóa - Dịch vụ - License (Hỗ trợ tra cứu có bộ lọc động theo Loại hoặc Lấy tất cả).
/// </summary>
[Authorize]
[Route("api/NghiepVu/hang-hoa-dich-vu")]
public class HangHoaDichVusController : CrudControllerBase<HangHoaDichVuDto, CreateHangHoaDichVuDto, UpdateHangHoaDichVuDto>
{
    private readonly IHangHoaDichVuService _hangHoaDichVuService;

    public HangHoaDichVusController(IHangHoaDichVuService service) : base(service)
    {
        _hangHoaDichVuService = service;
    }

    /// <summary>
    /// Lấy danh sách sản phẩm/dịch vụ/license theo ID Hợp đồng, hỗ trợ lọc theo Loại (Hàng hóa, Dịch vụ, License hoặc Tất cả).
    /// </summary>
    /// <param name="idParent">Mã định danh Hợp đồng (GUID)</param>
    /// <param name="loai">Loại sản phẩm cần lọc: 1 = Hàng hóa, 2 = License, 3 = Dịch vụ. Nếu để trống hoặc null sẽ trả về tất cả.</param>
    /// <returns>Danh sách Hàng hóa/Dịch vụ/License</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("parent/{idParent:guid}")]
    [ProducesResponseType(typeof(IEnumerable<HangHoaDichVuDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HangHoaDichVuDto>>> GetByIdParent(Guid idParent, [FromQuery] LoaiHangHoaDichVu? loai = null)
    {
        var result = await _hangHoaDichVuService.GetByIdParentAsync(idParent, loai);
        return Ok(result);
    }

    /// <summary>
    /// Thêm mới nhiều sản phẩm/dịch vụ/license cùng lúc.
    /// </summary>
    /// <param name="dtos">Danh sách dữ liệu cần tạo (mỗi DTO tự khai báo loại tương ứng qua trường Loai)</param>
    /// <returns>Danh sách đã tạo</returns>
    /// <response code="200">Tạo thành công</response>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(IEnumerable<HangHoaDichVuDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HangHoaDichVuDto>>> CreateBatch([FromBody] IEnumerable<CreateHangHoaDichVuDto> dtos)
    {
        var result = await _hangHoaDichVuService.CreateRangeAsync(dtos);
        return Ok(result);
    }
}
