using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces
{
    public interface IToNhomService
    {
        Task<IEnumerable<ToNhomDto>> GetAllAsync();
        Task<ToNhomDto?> GetByIdAsync(Guid id);
        Task<ToNhomDto> CreateAsync(CreateToNhomDto dto);
        Task<IEnumerable<ToNhomDto>> CreateRangeAsync(IEnumerable<CreateToNhomDto> dtos);
        Task<bool> UpdateAsync(Guid id, UpdateToNhomDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
