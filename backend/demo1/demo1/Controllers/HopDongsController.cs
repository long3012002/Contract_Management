using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Hợp đồng và Đợt thanh toán (Tạo hợp đồng, Tìm kiếm/Lọc nâng cao, Xác nhận thanh toán).
/// </summary>
[Route("api/NghiepVu/hop-dong")]
[FeatureAuthorize("CONTRACT_MANAGEMENT")]
public class HopDongsController : CrudControllerBase<HopDongDto, CreateHopDongDto, UpdateHopDongDto>
{
    private readonly IHopDongService _hopDongService;

    public HopDongsController(IHopDongService service) : base(service)
    {
        _hopDongService = service;
    }

    /// <summary>
    /// Lấy danh sách hợp đồng có bộ lọc nâng cao (Trạng thái, Từ ngày/Đến ngày, Giá trị, Từ khóa).
    /// </summary>
    /// <param name="filter">Bộ lọc danh sách hợp đồng</param>
    /// <returns>Danh sách hợp đồng phân trang</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<HopDongDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<HopDongDto>>> GetAll([FromQuery] HopDongFilterDto filter)
    {
        var result = await _hopDongService.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Phương thức cơ sở từ CrudControllerBase được vô hiệu hóa khỏi Routing và Swagger API Explorer để tránh xung đột route.
    /// </summary>
    [NonAction]
    [ApiExplorerSettings(IgnoreApi = true)]
    public override Task<ActionResult<PagedResult<HopDongDto>>> GetAll(string? search, int page = 1, int pageSize = 20, string? cursor = null)
    {
        return base.GetAll(search, page, pageSize, cursor);
    }

    /// <summary>
    /// Xác nhận hoàn tất thanh toán cho một Đợt thanh toán của Hợp đồng.
    /// </summary>
    /// <param name="dotThanhToanId">Mã định danh Đợt thanh toán (GUID)</param>
    /// <response code="200">Xác nhận thanh toán thành công</response>
    /// <response code="404">Không tìm thấy đợt thanh toán</response>
    [HttpPut("dot-thanh-toan/{dotThanhToanId:guid}/pay")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmPayment(Guid dotThanhToanId)
    {
        var success = await _hopDongService.ConfirmPaymentAsync(dotThanhToanId);
        return success 
            ? Ok(new { message = "Xác nhận thanh toán thành công." }) 
            : NotFound(new { message = "Không tìm thấy đợt thanh toán." });
    }
}
