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
/// API Quản lý Dịch vụ (Danh sách dịch vụ kèm theo hợp đồng/gói thầu, Tạo mới hàng loạt).
/// </summary>
[Authorize]
[Route("api/NghiepVu/dich-vu")]
public class DichVusController : CrudControllerBase<HangHoaDichVuDto, CreateHangHoaDichVuDto, UpdateHangHoaDichVuDto>
{
    private readonly IDichVuService _dichVuService;

    public DichVusController(IDichVuService service) : base(service)
    {
        _dichVuService = service;
    }

    /// <summary>
    /// Lấy danh sách Dịch vụ theo ID Hợp đồng.
    /// </summary>
    /// <param name="idParent">Mã định danh Hợp đồng (GUID)</param>
    /// <returns>Danh sách dịch vụ</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("parent/{idParent:guid}")]
    [ProducesResponseType(typeof(IEnumerable<HangHoaDichVuDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HangHoaDichVuDto>>> GetByIdParent(Guid idParent)
    {
        var result = await _dichVuService.GetByIdParentAsync(idParent);
        return Ok(result);
    }

    /// <summary>
    /// Thêm mới nhiều Dịch vụ cùng lúc.
    /// </summary>
    /// <param name="dtos">Danh sách dữ liệu dịch vụ cần tạo</param>
    /// <returns>Danh sách dịch vụ đã tạo</returns>
    /// <response code="200">Tạo danh sách dịch vụ thành công</response>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(IEnumerable<HangHoaDichVuDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HangHoaDichVuDto>>> CreateBatch([FromBody] IEnumerable<CreateHangHoaDichVuDto> dtos)
    {
        var result = await _dichVuService.CreateRangeAsync(dtos);
        return Ok(result);
    }
}
