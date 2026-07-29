using demo1.DTOs.DanhMuc;
using demo1.Entity.DanhMuc;

namespace demo1.Services.Interfaces.DanhMuc;

public interface IDonViTinhService : ICrudService<DonViTinhDto, CreateDonViTinhDto, UpdateDonViTinhDto>
{
}
