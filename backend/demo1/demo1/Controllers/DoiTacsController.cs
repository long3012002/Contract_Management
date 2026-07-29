using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Danh mục Đối tác / Nhà thầu (Thông tin đối tác, Tìm kiếm, Thêm mới, Cập nhật, Xóa).
/// </summary>
[Route("api/DanhMuc/doi-tac")]
[FeatureAuthorize("PARTNER")]
public class DoiTacsController : CrudControllerBase<DoiTacDto, CreateDoiTacDto, UpdateDoiTacDto>
{
    public DoiTacsController(IDoiTacService service) : base(service)
    {
    }
}
