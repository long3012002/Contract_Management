using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces
{
    public interface IChucVuService
    {
        Task<IEnumerable<ChucVuDto>> GetAllAsync();
        Task<ChucVuDto?> GetByIdAsync(Guid id);
        Task<ChucVuDto> CreateAsync(CreateChucVuDto dto);
        Task<IEnumerable<ChucVuDto>> CreateRangeAsync(IEnumerable<CreateChucVuDto> dtos);
        Task<bool> UpdateAsync(Guid id, UpdateChucVuDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
