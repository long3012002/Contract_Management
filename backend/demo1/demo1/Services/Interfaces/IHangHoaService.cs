using demo1.DTOs.HangHoa;

namespace demo1.Services.Interfaces;

public interface IHangHoaService : ICrudService<HangHoaDto, CreateHangHoaDto, UpdateHangHoaDto>
{
    Task<IEnumerable<HangHoaDto>> GetByIdParentAsync(Guid idParent);
}
