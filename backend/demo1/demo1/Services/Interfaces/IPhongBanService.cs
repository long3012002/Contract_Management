using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces
{
    public interface IPhongBanService
    {
        Task<IEnumerable<PhongBanDto>> GetAllAsync();
        Task<PhongBanDto?> GetByIdAsync(Guid id);
        Task<PhongBanDto> CreateAsync(CreatePhongBanDto dto);
        Task<IEnumerable<PhongBanDto>> CreateRangeAsync(IEnumerable<CreatePhongBanDto> dtos);
        Task<bool> UpdateAsync(Guid id, UpdatePhongBanDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
