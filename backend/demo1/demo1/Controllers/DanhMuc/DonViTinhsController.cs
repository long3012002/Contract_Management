using demo1.DTOs.DanhMuc;
using demo1.Services.Interfaces.DanhMuc;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers.DanhMuc;

/// <summary>
/// API Quản lý Danh mục Đơn vị tính (vd: Cái, Bộ, Máy, Gói, Tháng...).
/// </summary>
[Route("api/DanhMuc/don-vi-tinh")]
public class DonViTinhsController : CrudControllerBase<DonViTinhDto, CreateDonViTinhDto, UpdateDonViTinhDto>
{
    public DonViTinhsController(IDonViTinhService service) : base(service)
    {
    }
}
