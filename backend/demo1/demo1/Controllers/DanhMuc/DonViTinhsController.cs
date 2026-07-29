using demo1.DTOs.DanhMuc;
using demo1.Services.Interfaces.DanhMuc;

namespace demo1.Controllers.DanhMuc;

public class DonViTinhsController : CrudControllerBase<DonViTinhDto, CreateDonViTinhDto, UpdateDonViTinhDto>
{
    public DonViTinhsController(IDonViTinhService service) : base(service)
    {
    }
}
