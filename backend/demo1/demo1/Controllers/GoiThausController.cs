using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Gói thầu (Thông tin Gói thầu, Lọc danh sách, Trạng thái đấu thầu và Nhà thầu tham gia).
/// </summary>
[Route("api/NghiepVu/goi-thau")]
[FeatureAuthorize("GOI_THAU")] // Keep GOI_THAU feature code for authorization purposes
public class GoiThausController : CrudControllerBase<GoiThauDto, CreateGoiThauDto, UpdateGoiThauDto>
{
    private readonly IGoiThauService _goiThauService;

    public GoiThausController(IGoiThauService service) : base(service)
    {
        _goiThauService = service;
    }

    /// <summary>
    /// Lấy danh sách gói thầu với bộ lọc nâng cao (Hình thức lựa chọn nhà thầu, Trạng thái, Từ khóa).
    /// </summary>
    /// <param name="filter">Bộ lọc danh sách gói thầu</param>
    /// <returns>Danh sách gói thầu phân trang</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<GoiThauDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<GoiThauDto>>> GetAll([FromQuery] GoiThauFilterDto filter)
    {
        var result = await _goiThauService.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết của gói thầu và danh sách công việc kèm theo số lượng bình luận.
    /// </summary>
    /// <param name="id">Mã định danh gói thầu (GUID)</param>
    /// <returns>Thông tin chi tiết gói thầu và danh sách công việc</returns>
    /// <response code="200">Lấy dữ liệu thành công</response>
    /// <response code="404">Không tìm thấy gói thầu</response>
    [HttpGet("{id:guid}/chi-tiet-va-cong-viec")]
    [ProducesResponseType(typeof(GoiThauDetailWithTasksDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GoiThauDetailWithTasksDto>> GetDetailWithTasks(Guid id)
    {
        var result = await _goiThauService.GetDetailWithTasksAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Phương thức cơ sở từ CrudControllerBase được vô hiệu hóa khỏi Routing và Swagger API Explorer để tránh xung đột route.
    /// </summary>
    [NonAction]
    [ApiExplorerSettings(IgnoreApi = true)]
    public override Task<ActionResult<PagedResult<GoiThauDto>>> GetAll(string? search, int page = 1, int pageSize = 20, string? cursor = null)
    {
        return base.GetAll(search, page, pageSize, cursor);
    }
}
