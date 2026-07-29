using demo1.DTOs.DanhMuc;
using demo1.Services.Interfaces.DanhMuc;

namespace demo1.Controllers.DanhMuc;

public class HangSanXuatsController : CrudControllerBase<HangSanXuatDto, CreateHangSanXuatDto, UpdateHangSanXuatDto>
{
    public HangSanXuatsController(IHangSanXuatService service) : base(service)
    {
    }
}
