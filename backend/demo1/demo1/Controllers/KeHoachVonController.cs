using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Kế hoạch Vốn (Lập kế hoạch vốn, Trình duyệt, Phê duyệt/Trả về, Thêm/Xóa dự án khỏi đợt KHV).
/// </summary>
[Route("api/NghiepVu/ke-hoach-von")]
[Authorize]
[ApiController]
public class KeHoachVonController : ControllerBase
{
    private readonly IKeHoachVonService _keHoachVonService;

    public KeHoachVonController(IKeHoachVonService keHoachVonService)
    {
        _keHoachVonService = keHoachVonService;
    }

    /// <summary>
    /// Lấy danh sách các đợt Kế hoạch vốn có lọc theo Năm, Loại KHV (1: 6T, 2: Cả năm, 3: Bổ sung) và Trạng thái.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<KeHoachVonDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<KeHoachVonDto>>> GetAll([FromQuery] KeHoachVonFilterDto filter)
    {
        var result = await _keHoachVonService.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết một đợt Kế hoạch vốn kèm danh sách Dự án thuộc đợt đó.
    /// </summary>
    [HttpGet("GetById/{id:guid}")]
    [ProducesResponseType(typeof(KeHoachVonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KeHoachVonDto>> GetById(Guid id)
    {
        var result = await _keHoachVonService.GetByIdAsync(id);
        return result == null ? NotFound(new { message = $"Không tìm thấy Kế hoạch vốn với ID [{id}]." }) : Ok(result);
    }

    /// <summary>
    /// Lấy lịch sử các đợt Kế hoạch vốn đã được duyệt của 1 Dự án cụ thể.
    /// </summary>
    [HttpGet("du-an/{duAnId:guid}")]
    [ProducesResponseType(typeof(List<KeHoachVonDuAnItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<KeHoachVonDuAnItemDto>>> GetLichSuByDuAn(Guid duAnId)
    {
        var result = await _keHoachVonService.GetLichSuKeHoachVonByDuAnIdAsync(duAnId);
        return Ok(result);
    }

    /// <summary>
    /// Lập mới một đợt Kế hoạch vốn (Trạng thái mặc định: Bản nháp / Draft).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(KeHoachVonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<KeHoachVonDto>> Create([FromBody] CreateKeHoachVonDto dto)
    {
        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("id")?.Value;

        Guid? currentUserId = null;
        if (Guid.TryParse(currentUserIdStr, out var parsedId) && parsedId != Guid.Empty)
        {
            currentUserId = parsedId;
        }

        var result = await _keHoachVonService.CreateAsync(dto, currentUserId);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật thông tin đợt Kế hoạch vốn (Chỉ cho phép khi ở trạng thái Draft).
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateKeHoachVonDto dto)
    {
        var success = await _keHoachVonService.UpdateAsync(id, dto);
        return success ? Ok(new { message = "Cập nhật Kế hoạch vốn thành công." }) : NotFound(new { message = $"Không tìm thấy Kế hoạch vốn với ID [{id}]." });
    }

    /// <summary>
    /// Xóa đợt Kế hoạch vốn (Soft delete, chỉ cho phép khi ở trạng thái Draft).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _keHoachVonService.DeleteAsync(id);
        return success ? Ok(new { message = "Xóa Kế hoạch vốn thành công." }) : NotFound(new { message = $"Không tìm thấy Kế hoạch vốn với ID [{id}]." });
    }

    /// <summary>
    /// Trình duyệt Kế hoạch vốn (Chuyển trạng thái từ Nháp -> Đã trình).
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(typeof(KeHoachVonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<KeHoachVonDto>> Submit(Guid id)
    {
        var result = await _keHoachVonService.SubmitAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Phê duyệt Kế hoạch vốn (Chuyển trạng thái từ Đã trình -> Đã duyệt, chốt hạn mức ngân sách được duyệt).
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(KeHoachVonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<KeHoachVonDto>> Approve(Guid id, [FromBody] ApproveKeHoachVonDto dto)
    {
        var result = await _keHoachVonService.ApproveAsync(id, dto);
        return Ok(result);
    }

    /// <summary>
    /// Trả về Kế hoạch vốn (Chuyển trạng thái từ Đã trình -> Trả về / Rejected).
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(KeHoachVonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<KeHoachVonDto>> Reject(Guid id, [FromQuery] string? ghiChu)
    {
        var result = await _keHoachVonService.RejectAsync(id, ghiChu);
        return Ok(result);
    }

    /// <summary>
    /// Thêm hoặc Cập nhật số tiền nhu cầu xin cấp của 1 Dự án vào đợt Kế hoạch vốn.
    /// </summary>
    [HttpPost("{id:guid}/du-an")]
    [ProducesResponseType(typeof(KeHoachVonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<KeHoachVonDto>> AddOrUpdateDuAn(Guid id, [FromBody] AddDuAnToKHVDto dto)
    {
        var result = await _keHoachVonService.AddOrUpdateDuAnAsync(id, dto);
        return Ok(result);
    }

    /// <summary>
    /// Xóa một Dự án khỏi đợt Kế hoạch vốn (Chỉ khi KHV ở trạng thái Nháp).
    /// </summary>
    [HttpDelete("{id:guid}/du-an/{duAnId:guid}")]
    [ProducesResponseType(typeof(KeHoachVonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<KeHoachVonDto>> RemoveDuAn(Guid id, Guid duAnId)
    {
        var result = await _keHoachVonService.RemoveDuAnAsync(id, duAnId);
        return Ok(result);
    }
}
