using System;
using System.Collections.Generic;
using System.Linq;
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
            .Select(e => new { Value = (int)e, Code = e.ToString(), Label = e.GetDisplayName() });

        var loaiDuAnOptions = new[]
        {
            new { Value = 1, Code = "Nguon", Label = "Dự án nguồn" },
            new { Value = 2, Code = "TrienKhai", Label = "Dự án triển khai" }
        };

        return Ok(new
        {
            TrangThaiOptions = trangThaiOptions,
            LoaiDuAnOptions = loaiDuAnOptions
        });
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
    /// Điều chỉnh ngân sách/tổng mức đầu tư của dự án.
    /// </summary>
    /// <param name="id">Mã định danh Dự án (GUID)</param>
    /// <param name="dto">Thông tin kinh phí điều chỉnh, lý do và quyết định phê duyệt</param>
    /// <returns>Thông tin lịch sử điều chỉnh kinh phí dự án</returns>
    /// <response code="200">Điều chỉnh kinh phí thành công</response>
    /// <response code="400">Số tiền hoặc lý do không hợp lệ</response>
    /// <response code="404">Không tìm thấy dự án</response>
    [HttpPost("{id:guid}/dieu-chinh")]
    [ProducesResponseType(typeof(DieuChinhDuAnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DieuChinhDuAnDto>> AdjustBudget(Guid id, [FromBody] CreateDieuChinhDuAnDto dto)
    {
        try
        {
            var result = await _duAnService.AdjustBudgetAsync(id, dto);
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
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách lịch sử các lần điều chỉnh kinh phí của dự án.
    /// </summary>
    /// <param name="id">Mã định danh Dự án (GUID)</param>
    /// <returns>Danh sách các đợt điều chỉnh kinh phí</returns>
    /// <response code="200">Lấy lịch sử điều chỉnh thành công</response>
    [HttpGet("{id:guid}/dieu-chinh")]
    [ProducesResponseType(typeof(IReadOnlyList<DieuChinhDuAnDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DieuChinhDuAnDto>>> GetAdjustments(Guid id)
    {
        var result = await _duAnService.GetAdjustmentsAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Chuyển trạng thái dự án sang giai đoạn tiếp theo (vd: Chuẩn bị -> Thực hiện -> Hoàn thành).
    /// </summary>
    /// <param name="id">Mã định danh Dự án (GUID)</param>
    /// <returns>Thông tin dự án với trạng thái mới</returns>
    /// <response code="200">Chuyển trạng thái thành công</response>
    /// <response code="400">Dự án đã ở trạng thái cuối hoặc chưa đủ điều kiện chuyển</response>
    /// <response code="404">Không tìm thấy dự án</response>
    [HttpPost("{id:guid}/advance-status")]
    [ProducesResponseType(typeof(DuAnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    /// <response code="404">Không tìm thấy dự án</response>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(DuAnDto), StatusCodes.Status200OK)]
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
    }

    /// <summary>
    /// Lấy danh sách các Dự án Nguồn (Nguồn vốn/Dự án mua sắm) liên kết với Dự án Triển khai.
    /// </summary>
    /// <param name="id">Mã định danh Dự án (GUID)</param>
    /// <returns>Danh sách dự án nguồn liên kết</returns>
    /// <response code="200">Lấy danh sách dự án nguồn thành công</response>
    [HttpGet("{id:guid}/du-an-nguon")]
    [ProducesResponseType(typeof(IReadOnlyList<DuAnNguonSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DuAnNguonSummaryDto>>> GetSourceProjects(Guid id)
    {
        var result = await _duAnService.GetSourceProjectsByProjectIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các Gói thầu thuộc Dự án.
    /// </summary>
    /// <param name="id">Mã định danh Dự án (GUID)</param>
    /// <returns>Danh sách gói thầu thuộc dự án</returns>
    /// <response code="200">Lấy danh sách gói thầu thành công</response>
    [HttpGet("{id:guid}/goi-thau")]
    [ProducesResponseType(typeof(IReadOnlyList<GoiThauDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GoiThauDto>>> GetGoiThaus(Guid id)
    {
        var result = await _duAnService.GetGoiThausByProjectIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các Hợp đồng thuộc Dự án.
    /// </summary>
    /// <param name="id">Mã định danh Dự án (GUID)</param>
    /// <returns>Danh sách hợp đồng thuộc dự án</returns>
    /// <response code="200">Lấy danh sách hợp đồng thành công</response>
    [HttpGet("{id:guid}/hop-dong")]
    [ProducesResponseType(typeof(IReadOnlyList<HopDongDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HopDongDto>>> GetHopDongs(Guid id)
    {
        var result = await _duAnService.GetHopDongsByProjectIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách Audit Log (nhật ký thay đổi) của Dự án.
    /// </summary>
    /// <param name="id">Mã định danh Dự án (GUID)</param>
    /// <returns>Danh sách audit logs</returns>
    /// <response code="200">Lấy audit logs thành công</response>
    [HttpGet("{id:guid}/audit-log")]
    [ProducesResponseType(typeof(IReadOnlyList<demo1.Entity.AuditLog>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<demo1.Entity.AuditLog>>> GetAuditLogs(Guid id)
    {
        var result = await _duAnService.GetAuditLogsByProjectIdAsync(id);
        return Ok(result);
    }
}
