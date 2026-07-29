using demo1.DTOs.DichVu;

namespace demo1.Services.Interfaces;

public interface IDichVuService : ICrudService<DichVuDto, CreateDichVuDto, UpdateDichVuDto>
{
    Task<IEnumerable<DichVuDto>> GetByIdParentAsync(Guid idParent);
}
