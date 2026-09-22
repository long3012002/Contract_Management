using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Entity;

namespace demo1.Services.Interfaces;

public interface IDuAnService : ICrudService<DuAnDto, CreateDuAnDto, UpdateDuAnDto>
{
    Task<PagedResult<DuAnDto>> GetAllAsync(DuAnFilterDto filter);
    Task<DuAnDto> AdvanceStatusAsync(Guid id);
    Task<DuAnDto> CloseProjectAsync(Guid id);
    Task<DuAnDto> GopDuAnAsync(Guid sourceId, GopDuAnDto dto, Guid currentUserId);
    Task<DuAnDto> HuyGopDuAnAsync(Guid targetDuAnId, HuyGopDuAnDto dto, Guid currentUserId);
    Task<IReadOnlyList<GoiThauDto>> GetGoiThausByProjectIdAsync(Guid id);
    Task<IReadOnlyList<HopDongDto>> GetHopDongsByProjectIdAsync(Guid id);
    Task<IReadOnlyList<AuditLog>> GetAuditLogsByProjectIdAsync(Guid id);
    Task<bool> ChangeOwnerAsync(Guid projectId, Guid newOwnerId);
    Task<IReadOnlyList<DuAnLookupDto>> GetLookupAsync();
}
