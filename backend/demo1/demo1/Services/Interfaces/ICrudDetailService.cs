using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface ICrudDetailService<TDto, in TCreateDto, in TUpdateDto>
    : ICrudService<TDto, TCreateDto, TUpdateDto>
    where TDto : IHasId
{
    Task<IEnumerable<TDto>> GetByParentIdAsync(Guid parentId);
    Task<PagedResult<TDto>> GetByParentIdPagedAsync(Guid parentId, string? search, int page, int pageSize, string? cursor = null);
    Task<bool> DeleteByParentIdAsync(Guid parentId);
}
