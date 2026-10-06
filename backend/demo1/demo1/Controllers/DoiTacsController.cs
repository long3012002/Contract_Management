using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Danh mục Đối tác / Nhà thầu (Thông tin đối tác, Tìm kiếm, Thêm mới, Cập nhật, Xóa).
/// </summary>
[Route("api/DanhMuc/doi-tac")]
[FeatureAuthorize("DOI_TAC")]
public class DoiTacsController : CrudControllerBase<DoiTacDto, CreateDoiTacDto, UpdateDoiTacDto>
{
    private readonly IDoiTacService _doiTacService;

    public DoiTacsController(IDoiTacService service) : base(service)
    {
        _doiTacService = service;
    }

    /// <summary>
    /// Kiểm tra Mã số thuế đã tồn tại trong hệ thống hay chưa để chống trùng lặp nhà thầu.
    /// </summary>
    /// <param name="taxCode">Mã số thuế cần kiểm tra</param>
    /// <param name="excludeId">ID nhà thầu cần loại trừ (khi sửa)</param>
    /// <returns>Thông tin nhà thầu đã tồn tại nếu trùng, hoặc null nếu chưa có</returns>
    [HttpGet("check-tax-code")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<DoiTacDto?>> CheckTaxCode(
        [FromQuery] string taxCode,
        [FromQuery] Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(taxCode))
        {
            return Ok(null);
        }

        var matched = await _doiTacService.CheckTaxCodeAsync(taxCode, excludeId);
        return Ok(matched);
    }
}
