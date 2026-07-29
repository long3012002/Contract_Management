using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Gói thầu (Thông tin Gói thầu, Lọc danh sách, Trạng thái đấu thầu và Nhà thầu tham gia).
/// </summary>
[Route("api/NghiepVu/goi-thau")]
[FeatureAuthorize("BID_PACKAGE")] // Keep BID_PACKAGE feature code for authorization purposes
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
    /// Phương thức cơ sở từ CrudControllerBase được ẩn khỏi Swagger API Explorer để tránh xung đột route.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    public override Task<ActionResult<PagedResult<GoiThauDto>>> GetAll(string? search, int page = 1, int pageSize = 20, string? cursor = null)
    {
        return base.GetAll(search, page, pageSize, cursor);
    }
}
