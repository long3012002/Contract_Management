using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.Entities;

namespace demo1.Services
{
    public interface IBaseService<TEntity, TResponseDto, TCreateDto, TUpdateDto> 
        where TEntity : BaseEntity
    {
        Task<IEnumerable<TResponseDto>> GetAllAsync();
        Task<TResponseDto?> GetByIdAsync(Guid id);
        Task<TResponseDto> CreateAsync(TCreateDto dto);
        Task UpdateAsync(Guid id, TUpdateDto dto);
        Task DeleteAsync(Guid id);
    }
}
