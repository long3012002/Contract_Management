using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface IDoiTacService : ICrudService<DoiTacDto, CreateDoiTacDto, UpdateDoiTacDto>
{
    Task<DoiTacDto?> CheckTaxCodeAsync(string taxCode, Guid? excludeId = null);
}
