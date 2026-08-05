using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs.DichVu;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Dịch vụ (Danh sách dịch vụ kèm theo hợp đồng/gói thầu, Tạo mới hàng loạt).
/// </summary>
[Authorize]
[Route("api/NghiepVu/dich-vu")]
public class DichVusController : CrudControllerBase<DichVuDto, CreateDichVuDto, UpdateDichVuDto>
{
    private readonly IDichVuService _dichVuService;

    public DichVusController(IDichVuService service) : base(service)
    {
        _dichVuService = service;
    }

    /// <summary>
    /// Lấy danh sách Dịch vụ theo ID của đối tượng cha (vd: ID Hợp đồng hoặc ID Gói thầu).
    /// </summary>
    /// <param name="idParent">Mã định danh đối tượng cha (GUID)</param>
    /// <returns>Danh sách dịch vụ</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("parent/{idParent:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DichVuDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DichVuDto>>> GetByIdParent(Guid idParent)
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
    [ProducesResponseType(typeof(IEnumerable<DichVuDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DichVuDto>>> CreateBatch([FromBody] IEnumerable<CreateDichVuDto> dtos)
    {
        var result = await _dichVuService.CreateRangeAsync(dtos);
        return Ok(result);
    }
}
