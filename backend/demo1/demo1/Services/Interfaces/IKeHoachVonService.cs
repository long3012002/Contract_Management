using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface IKeHoachVonService
{
    Task<PagedResult<KeHoachVonDto>> GetAllAsync(KeHoachVonFilterDto filter);
    Task<KeHoachVonDto?> GetByIdAsync(Guid id);
    Task<KeHoachVonDto> CreateAsync(CreateKeHoachVonDto dto, Guid? currentUserId);
    Task<bool> UpdateAsync(Guid id, UpdateKeHoachVonDto dto);
    Task<bool> DeleteAsync(Guid id);
    
    Task<KeHoachVonDto> SubmitAsync(Guid id);
    Task<KeHoachVonDto> ApproveAsync(Guid id, ApproveKeHoachVonDto dto);
    Task<KeHoachVonDto> RejectAsync(Guid id, string? ghiChu);

    Task<KeHoachVonDto> AddOrUpdateDuAnAsync(Guid id, AddDuAnToKHVDto dto);
    Task<KeHoachVonDto> RemoveDuAnAsync(Guid id, Guid duAnId);
    Task<List<KeHoachVonDuAnItemDto>> GetLichSuKeHoachVonByDuAnIdAsync(Guid duAnId);
}
