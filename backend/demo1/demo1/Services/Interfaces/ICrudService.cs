using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface ICrudService<TDto, in TCreateDto, in TUpdateDto>
    where TDto : IHasId
{
    Task<PagedResult<TDto>> GetAllAsync(string? search, int page, int pageSize);
    Task<IReadOnlyList<TDto>> GetAllItemsAsync();
    Task<TDto?> GetByIdAsync(Guid id);
    Task<TDto> CreateAsync(TCreateDto dto);
    Task<bool> UpdateAsync(Guid id, TUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
}
