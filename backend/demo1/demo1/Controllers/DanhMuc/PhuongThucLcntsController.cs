using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Danh mục Phương thức Lựa chọn nhà thầu (LCNT).
/// </summary>
[Route("api/DanhMuc/phuong-thuc-lcnt")]
[FeatureAuthorize("DANH_MUC")]
public class PhuongThucLcntsController : CrudControllerBase<PhuongThucLcntDto, CreatePhuongThucLcntDto, UpdatePhuongThucLcntDto>
{
    public PhuongThucLcntsController(IPhuongThucLcntService service) : base(service)
    {
    }
}
