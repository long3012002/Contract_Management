using demo1.DTOs.DanhMuc;
using demo1.Services.Interfaces.DanhMuc;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers.DanhMuc;

/// <summary>
/// API Quản lý Danh mục Hãng sản xuất / Nhà cung cấp thiết bị (vd: IBM, Cisco, Dell, Oracle...).
/// </summary>
[Route("api/DanhMuc/hang-san-xuat")]
public class HangSanXuatsController : CrudControllerBase<HangSanXuatDto, CreateHangSanXuatDto, UpdateHangSanXuatDto>
{
    public HangSanXuatsController(IHangSanXuatService service) : base(service)
    {
    }
}
