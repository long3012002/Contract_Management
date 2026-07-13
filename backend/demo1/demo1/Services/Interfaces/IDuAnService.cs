using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Entity;

namespace demo1.Services.Interfaces;

public interface IDuAnService : ICrudService<DuAnDto, CreateDuAnDto, UpdateDuAnDto>
{
    Task<DieuChinhDuAnDto> AdjustBudgetAsync(Guid id, CreateDieuChinhDuAnDto dto);
    Task<IReadOnlyList<DieuChinhDuAnDto>> GetAdjustmentsAsync(Guid id);
    Task<DuAnDto> AdvanceStatusAsync(Guid id);
    Task<DuAnDto> CloseProjectAsync(Guid id);
    Task<IReadOnlyList<GoiThauDto>> GetGoiThausByProjectIdAsync(Guid id);
    Task<IReadOnlyList<HopDongDto>> GetHopDongsByProjectIdAsync(Guid id);
    Task<IReadOnlyList<AuditLog>> GetAuditLogsByProjectIdAsync(Guid id);
}

