using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Công việc Gói thầu (Danh sách công việc trình tự, Xác nhận hoàn thành, Chuyển tiếp công việc và Lịch sử).
/// </summary>
[Route("api/NghiepVu/cong-viec-goi-thau")]
[FeatureAuthorize("GOI_THAU")]
public class CongViecGoiThausController : CrudControllerBase<CongViecGoiThauDto, CreateCongViecGoiThauDto, UpdateCongViecGoiThauDto>
{
    private readonly ICongViecGoiThauService _congViecGoiThauService;

    public CongViecGoiThausController(ICongViecGoiThauService service) : base(service)
    {
        _congViecGoiThauService = service;
    }

    /// <summary>
    /// Lấy toàn bộ danh sách Công việc thuộc một Gói thầu.
    /// </summary>
    /// <param name="idGoiThau">Mã định danh Gói thầu (GUID)</param>
    /// <returns>Danh sách công việc gói thầu</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("goi-thau/{idGoiThau:guid}")]
    [ProducesResponseType(typeof(IEnumerable<CongViecGoiThauDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CongViecGoiThauDto>>> GetByGoiThauId(Guid idGoiThau)
    {
        var result = await _congViecGoiThauService.GetByParentIdAsync(idGoiThau);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách Công việc thuộc Gói thầu hỗ trợ phân trang và tìm kiếm.
    /// </summary>
    /// <param name="idGoiThau">Mã định danh Gói thầu (GUID)</param>
    /// <param name="search">Từ khóa tìm kiếm (tùy chọn)</param>
    /// <param name="page">Trang hiện tại (Mặc định: 1)</param>
    /// <param name="pageSize">Số lượng bản ghi trên một trang (Mặc định: 20)</param>
    /// <param name="cursor">Con trỏ phân trang (tùy chọn)</param>
    /// <returns>Danh sách công việc phân trang</returns>
    /// <response code="200">Lấy danh sách phân trang thành công</response>
    [HttpGet("{idGoiThau:guid}/paged")]
    [ProducesResponseType(typeof(PagedResult<CongViecGoiThauDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CongViecGoiThauDto>>> GetByGoiThauIdPaged(
        Guid idGoiThau,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? cursor = null)
    {
        var result = await _congViecGoiThauService.GetByParentIdPagedAsync(idGoiThau, search, page, pageSize, cursor);
        return Ok(result);
    }

    /// <summary>
    /// Xóa toàn bộ danh sách Công việc thuộc một Gói thầu.
    /// </summary>
    /// <param name="idGoiThau">Mã định danh Gói thầu (GUID)</param>
    /// <response code="204">Xóa thành công (No Content)</response>
    /// <response code="404">Không tìm thấy công việc thuộc gói thầu</response>
    [HttpDelete("goi-thau/{idGoiThau:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteByGoiThauId(Guid idGoiThau)
    {
        var success = await _congViecGoiThauService.DeleteByParentIdAsync(idGoiThau);
        return success ? NoContent() : NotFound(new { message = $"Không tìm thấy công việc nào cho gói thầu '{idGoiThau}'." });
    }

    /// <summary>
    /// Lấy Báo cáo tình hình tiến độ và hoàn thành công việc của Gói thầu.
    /// </summary>
    /// <param name="idGoiThau">Mã định danh Gói thầu (GUID)</param>
    /// <returns>Báo cáo tiến độ công việc gói thầu</returns>
    /// <response code="200">Lấy báo cáo thành công</response>
    [HttpGet("{idGoiThau:guid}/report")]
    [ProducesResponseType(typeof(CongViecGoiThauReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CongViecGoiThauReportDto>> GetReport(Guid idGoiThau)
    {
        var report = await _congViecGoiThauService.GetReportByGoiThauIdAsync(idGoiThau);
        return Ok(report);
    }

    /// <summary>
    /// Xác nhận hoàn thành một bước Công việc trong Gói thầu.
    /// </summary>
    /// <param name="id">Mã định danh Công việc (GUID)</param>
    /// <param name="context">DbContext injection</param>
    /// <param name="currentUserService">Dịch vụ người dùng hiện tại</param>
    /// <response code="200">Xác nhận công việc thành công</response>
    /// <response code="400">Không có quyền xác nhận hoặc dữ liệu không hợp lệ</response>
    [HttpPost("{id:guid}/xac-nhan")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmCongViec(
        Guid id,
        [FromServices] demo1.Data.AppDbContext context,
        [FromServices] ICurrentUserService currentUserService)
    {
        var username = currentUserService.GetUsername();
        var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.Users, u => u.Username == username);
        if (user == null) return Unauthorized(new { message = "Người dùng không hợp lệ." });

        var success = await _congViecGoiThauService.ConfirmCongViecAsync(id, user.Id);
        if (!success) return BadRequest(new { message = "Không thể xác nhận công việc này hoặc bạn không thuộc danh sách người liên quan." });

        return Ok(new { message = "Xác nhận công việc thành công." });
    }

    /// <summary>
    /// Chuyển tiếp công việc cho các cá nhân / nhân sự liên quan khác xử lý.
    /// </summary>
    /// <param name="id">Mã định danh Công việc (GUID)</param>
    /// <param name="body">Danh sách UserIds hoặc DTO thông tin chuyển tiếp kèm ghi chú</param>
    /// <param name="context">DbContext injection</param>
    /// <param name="currentUserService">Dịch vụ người dùng hiện tại</param>
    /// <response code="200">Chuyển tiếp công việc thành công</response>
    /// <response code="400">Danh sách người nhận không hợp lệ</response>
    [HttpPost("{id:guid}/forward")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForwardStakeholders(
        Guid id,
        [FromBody] System.Text.Json.JsonElement body,
        [FromServices] demo1.Data.AppDbContext context,
        [FromServices] ICurrentUserService currentUserService)
    {
        List<Guid> userIds = new List<Guid>();
        string? ghiChu = null;

        if (body.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            userIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(body.GetRawText()) ?? new List<Guid>();
        }
        else if (body.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            var request = System.Text.Json.JsonSerializer.Deserialize<ForwardStakeholdersRequestDto>(
                body.GetRawText(),
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (request != null)
            {
                userIds = request.UserIds;
                ghiChu = request.GhiChu;
            }
        }

        if (userIds == null || userIds.Count == 0)
        {
            return BadRequest(new { message = "Danh sách người liên quan không được để trống." });
        }

        var username = currentUserService.GetUsername();
        var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.Users, u => u.Username == username);
        Guid? currentUserId = user?.Id;

        var (success, message) = await _congViecGoiThauService.ForwardStakeholdersAsync(id, userIds, currentUserId, ghiChu);
        if (!success)
        {
            return BadRequest(new { message });
        }

        return Ok(new { message });
    }

    /// <summary>
    /// Lấy Lịch sử Chuyển tiếp công việc theo ID Công việc.
    /// </summary>
    /// <param name="id">Mã định danh Công việc (GUID)</param>
    /// <returns>Danh sách lịch sử chuyển tiếp</returns>
    /// <response code="200">Lấy lịch sử thành công</response>
    [HttpGet("{id:guid}/forward-history")]
    [ProducesResponseType(typeof(List<CongViecLichSuChuyenTiepDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CongViecLichSuChuyenTiepDto>>> GetForwardHistory(Guid id)
    {
        var history = await _congViecGoiThauService.GetForwardHistoryAsync(id);
        return Ok(history);
    }
}
