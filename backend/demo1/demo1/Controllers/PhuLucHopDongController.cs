using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Phụ lục Hợp đồng (Contract Addendums).
/// </summary>
[ApiController]
[Route("api/NghiepVu")]
[FeatureAuthorize("QUAN_LY_HOP_DONG")]
public class PhuLucHopDongController : ControllerBase
{
    private readonly IPhuLucHopDongService _phuLucService;

    public PhuLucHopDongController(IPhuLucHopDongService phuLucService)
    {
        _phuLucService = phuLucService;
    }

    /// <summary>
    /// Lấy danh sách phụ lục hợp đồng của một Hợp đồng.
    /// </summary>
    [HttpGet("hop-dong/{hopDongId}/phu-luc")]
    [ProducesResponseType(typeof(List<PhuLucHopDongDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PhuLucHopDongDto>>> GetByHopDongId(Guid hopDongId, [FromQuery] TrangThaiPhuLuc? trangThai = null)
    {
        var result = await _phuLucService.GetByHopDongIdAsync(hopDongId, trangThai);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết một Phụ lục hợp đồng.
    /// </summary>
    [HttpGet("phu-luc/{id}")]
    [ProducesResponseType(typeof(PhuLucHopDongDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PhuLucHopDongDto>> GetById(Guid id)
    {
        var result = await _phuLucService.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Không tìm thấy phụ lục hợp đồng." });
        return Ok(result);
    }

    /// <summary>
    /// Tạo mới một Phụ lục hợp đồng.
    /// </summary>
    [HttpPost("phu-luc")]
    [ProducesResponseType(typeof(PhuLucHopDongDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PhuLucHopDongDto>> Create([FromBody] CreatePhuLucHopDongDto dto)
    {
        try
        {
            var result = await _phuLucService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật thông tin Phụ lục hợp đồng.
    /// </summary>
    [HttpPut("phu-luc/{id}")]
    [ProducesResponseType(typeof(PhuLucHopDongDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PhuLucHopDongDto>> Update(Guid id, [FromBody] UpdatePhuLucHopDongDto dto)
    {
        var result = await _phuLucService.UpdateAsync(id, dto);
        if (result == null) return NotFound(new { message = "Không tìm thấy phụ lục hợp đồng." });
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật trạng thái Phụ lục hợp đồng (Hiệu lực, Hết hiệu lực, Hủy).
    /// </summary>
    [HttpPatch("phu-luc/{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateStatus(Guid id, [FromQuery] TrangThaiPhuLuc trangThai)
    {
        var success = await _phuLucService.UpdateStatusAsync(id, trangThai);
        if (!success) return NotFound(new { message = "Không tìm thấy phụ lục hợp đồng." });
        return Ok(new { message = "Cập nhật trạng thái phụ lục thành công." });
    }

    /// <summary>
    /// Xóa một Phụ lục hợp đồng.
    /// </summary>
    [HttpDelete("phu-luc/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var success = await _phuLucService.DeleteAsync(id);
        if (!success) return NotFound(new { message = "Không tìm thấy phụ lục hợp đồng." });
        return Ok(new { message = "Xóa phụ lục hợp đồng thành công." });
    }
}
