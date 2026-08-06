using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý và Tra cứu Đợt thanh toán của Hợp đồng.
/// </summary>
[Authorize]
[ApiController]
[Route("api/NghiepVu/dot-thanh-toan")]
public class DotThanhToansController(IDotThanhToanService dotThanhToanService) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách đợt thanh toán theo năm có bộ lọc nâng cao (Khoảng thời gian, Trạng thái thanh toán, Từ khóa tìm kiếm).
    /// </summary>
    /// <param name="filter">Bộ lọc danh sách đợt thanh toán</param>
    /// <returns>Danh sách các đợt thanh toán phân trang</returns>
    /// <response code="200">Lấy dữ liệu thành công</response>
    [HttpGet("by-year")]
    [ProducesResponseType(typeof(PagedResult<DotThanhToanReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DotThanhToanReportDto>>> GetDotThanhToanByYear([FromQuery] DotThanhToanFilterDto filter)
    {
        var result = await dotThanhToanService.GetFilteredPaymentPhasesAsync(filter);
        return Ok(result);
    }
}
