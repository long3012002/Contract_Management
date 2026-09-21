using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Dự án (Thông tin Dự án, Điều chỉnh kinh phí, Chuyển giai đoạn, Đóng dự án, Tra cứu Gói thầu/Hợp đồng thuộc Dự án).
/// </summary>
[Route("api/NghiepVu/du-an")]
[FeatureAuthorize("DU_AN")] // Keep DU_AN feature code for authorization purposes
public class DuAnsController : CrudControllerBase<DuAnDto, CreateDuAnDto, UpdateDuAnDto>
{
    private readonly IDuAnService _duAnService;

    public DuAnsController(IDuAnService service) : base(service)
    {
        _duAnService = service;
    }

    /// <summary>
    /// Lấy danh sách các Enum/Option trạng thái và loại dự án.
    /// </summary>
    /// <returns>Danh sách các Option Enum của Dự án</returns>
    /// <response code="200">Lấy danh sách Enum thành công</response>
    [HttpGet("enums")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetDuAnEnums()
    {
        var trangThaiOptions = Enum.GetValues<TrangThaiDuAn>()
            .Distinct()
            .GroupBy(e => (int)e)
            .Select(g => g.First())
            .Select(e => new { Value = (int)e, Code = e.ToString(), Label = e.GetDisplayName() });

        return Ok(new
        {
            TrangThaiOptions = trangThaiOptions
        });
    }

    /// <summary>
    /// Lấy danh sách dự án thu gọn (Id, Code, Name) phục vụ Dropdown / Bộ lọc.
    /// </summary>
    /// <returns>Danh sách dự án thu gọn</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(IReadOnlyList<DuAnLookupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DuAnLookupDto>>> GetLookup()
    {
        var result = await _duAnService.GetLookupAsync();
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách dự án với bộ lọc nâng cao (Loại dự án, Từ khóa...).
    /// </summary>
    /// <param name="filter">Bộ lọc danh sách dự án</param>
    /// <returns>Danh sách dự án phân trang</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DuAnDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DuAnDto>>> GetAll([FromQuery] DuAnFilterDto filter)
    {
        var result = await _duAnService.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Phương thức cơ sở từ CrudControllerBase được vô hiệu hóa khỏi Routing và Swagger API Explorer để tránh xung đột route.
    /// </summary>
    [NonAction]
    [ApiExplorerSettings(IgnoreApi = true)]
    public override Task<ActionResult<PagedResult<DuAnDto>>> GetAll(string? search, int page = 1, int pageSize = 20, string? cursor = null)
    {
        return base.GetAll(search, page, pageSize, cursor);
    }



    /// <summary>
    /// Chuyển trạng thái dự án sang giai đoạn tiếp theo (vd: Chuẩn bị -> Thực hiện -> Hoàn thành).
    /// </summary>
    /// <param name="id">Mã định danh Dự án (GUID)</param>
    /// <returns>Thông tin dự án với trạng thái mới</returns>
    /// <response code="200">Chuyển trạng thái thành công</response>
    /// <response code="400">Dự án đã ở trạng thái cuối hoặc chưa đủ điều kiện chuyển</response>
    /// <response code="403">Không có quyền thực hiện</response>
    /// <response code="404">Không tìm thấy dự án</response>
    [HttpPost("{id:guid}/advance-status")]
    [ProducesResponseType(typeof(DuAnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DuAnDto>> AdvanceStatus(Guid id)
    {
        try
        {
            var result = await _duAnService.AdvanceStatusAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Quyết toán và Đóng dự án.
    /// </summary>
    /// <param name="id">Mã định danh Dự án (GUID)</param>
    /// <returns>Thông tin dự án đã được đóng</returns>
    /// <response code="200">Đóng dự án thành công</response>
    /// <response code="403">Không có quyền thực hiện</response>
    /// <response code="404">Không tìm thấy dự án</response>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(DuAnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DuAnDto>> CloseProject(Guid id)
    {
        try
        {
            var result = await _duAnService.CloseProjectAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }



    /// <summary>
    /// Lấy danh sách các Gói thầu thuộc Dự án.
    /// </summary>
    /// <param name="id">Mã định danh Dự án (GUID)</param>
    /// <returns>Danh sách gói thầu thuộc dự án</returns>
    /// <response code="200">Lấy danh sách gói thầu thành công</response>
    /// <response code="403">Không có quyền truy cập</response>
    /// <response code="404">Không tìm thấy dự án</response>
    [HttpGet("{id:guid}/goi-thau")]
    [ProducesResponseType(typeof(IReadOnlyList<GoiThauDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<GoiThauDto>>> GetGoiThaus(Guid id)
    {
        try
        {
            var result = await _duAnService.GetGoiThausByProjectIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách các Hợp đồng thuộc Dự án.
    /// </summary>
    /// <param name="id">Mã định danh Dự án (GUID)</param>
    /// <returns>Danh sách hợp đồng thuộc dự án</returns>
    /// <response code="200">Lấy danh sách hợp đồng thành công</response>
    /// <response code="403">Không có quyền truy cập</response>
    /// <response code="404">Không tìm thấy dự án</response>
    [HttpGet("{id:guid}/hop-dong")]
    [ProducesResponseType(typeof(IReadOnlyList<HopDongDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<HopDongDto>>> GetHopDongs(Guid id)
    {
        try
        {
            var result = await _duAnService.GetHopDongsByProjectIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách Audit Log (nhật ký thay đổi) của Dự án.
    /// </summary>
    /// <param name="id">Mã định danh Dự án (GUID)</param>
    /// <returns>Danh sách audit logs</returns>
    /// <response code="200">Lấy audit logs thành công</response>
    /// <response code="403">Không có quyền truy cập</response>
    /// <response code="404">Không tìm thấy dự án</response>
    [HttpGet("{id:guid}/audit-log")]
    [ProducesResponseType(typeof(IReadOnlyList<demo1.Entity.AuditLog>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<demo1.Entity.AuditLog>>> GetAuditLogs(Guid id)
    {
        try
        {
            var result = await _duAnService.GetAuditLogsByProjectIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Thay đổi chủ dự án (Yêu cầu là Admin hoặc Chủ dự án hiện tại).
    /// </summary>
    /// <param name="id">Mã định danh Dự án (GUID)</param>
    /// <param name="newOwnerId">Mã định danh Chủ dự án mới (GUID)</param>
    /// <response code="200">Thay đổi chủ dự án thành công</response>
    /// <response code="400">Yêu cầu không hợp lệ hoặc chủ dự án mới không hoạt động</response>
    /// <response code="403">Không có quyền thực hiện</response>
    /// <response code="404">Không tìm thấy dự án hoặc người dùng mới</response>
    [HttpPost("{id:guid}/ThayDoiChuDuAn/{newOwnerId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ThayDoiChuDuAn(Guid id, Guid newOwnerId)
    {
        try
        {
            var success = await _duAnService.ChangeOwnerAsync(id, newOwnerId);
            return success ? Ok(new { message = "Thay đổi chủ dự án thành công." }) : BadRequest(new { message = "Không thể thay đổi chủ dự án." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Thực hiện Gộp Dự án hiện tại ({id}) vào Dự án đích (TargetDuAnId).
    /// </summary>
    /// <param name="id">Mã định danh Dự án nguồn bị gộp (GUID)</param>
    /// <param name="dto">Thông tin dự án đích và lý do gộp</param>
    /// <response code="200">Gộp dự án thành công</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc dự án đã bị gộp từ trước</response>
    /// <response code="404">Không tìm thấy dự án nguồn hoặc dự án đích</response>
    [HttpPost("{id:guid}/gop-du-an")]
    [ProducesResponseType(typeof(DuAnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DuAnDto>> GopDuAn(Guid id, [FromBody] GopDuAnDto dto)
    {
        var currentUserIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        Guid.TryParse(currentUserIdStr, out var currentUserId);

        var result = await _duAnService.GopDuAnAsync(id, dto, currentUserId);
        return Ok(result);
    }
}

