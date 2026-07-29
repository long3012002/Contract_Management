using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces
{
    public interface IDonViService
    {
        Task<IEnumerable<DonViDto>> GetAllAsync();
        Task<DonViDto?> GetByIdAsync(Guid id);
        Task<DonViDto> CreateAsync(CreateDonViDto dto);
        Task<IEnumerable<DonViDto>> CreateRangeAsync(IEnumerable<CreateDonViDto> dtos);
        Task<bool> UpdateAsync(Guid id, UpdateDonViDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
