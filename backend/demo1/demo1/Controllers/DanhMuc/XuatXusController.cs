using demo1.DTOs.DanhMuc;
using demo1.Services.Interfaces.DanhMuc;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers.DanhMuc;

/// <summary>
/// API Quản lý Danh mục Xuất xứ Hàng hóa (vd: Việt Nam, Mỹ, Nhật Bản, Đức, Trung Quốc...).
/// </summary>
[Route("api/DanhMuc/xuat-xu")]
public class XuatXusController : CrudControllerBase<XuatXuDto, CreateXuatXuDto, UpdateXuatXuDto>
{
    public XuatXusController(IXuatXuService service) : base(service)
    {
    }
}
