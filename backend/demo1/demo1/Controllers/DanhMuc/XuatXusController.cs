using demo1.DTOs.DanhMuc;
using demo1.Services.Interfaces.DanhMuc;

namespace demo1.Controllers.DanhMuc;

public class XuatXusController : CrudControllerBase<XuatXuDto, CreateXuatXuDto, UpdateXuatXuDto>
{
    public XuatXusController(IXuatXuService service) : base(service)
    {
    }
}
