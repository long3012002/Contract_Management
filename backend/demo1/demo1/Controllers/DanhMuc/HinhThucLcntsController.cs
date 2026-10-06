using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Danh mục Hình thức Lựa chọn nhà thầu (LCNT).
/// </summary>
[Route("api/DanhMuc/hinh-thuc-lcnt")]
[FeatureAuthorize("DANH_MUC")]
public class HinhThucLcntsController : CrudControllerBase<HinhThucLcntDto, CreateHinhThucLcntDto, UpdateHinhThucLcntDto>
{
    public HinhThucLcntsController(IHinhThucLcntService service) : base(service)
    {
    }
}
