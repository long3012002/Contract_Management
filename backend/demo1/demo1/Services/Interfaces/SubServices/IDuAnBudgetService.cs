using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces.SubServices;

public interface IDuAnBudgetService
{
    Task<DieuChinhDuAnDto> AdjustBudgetAsync(Guid id, CreateDieuChinhDuAnDto dto);
    Task<IReadOnlyList<DieuChinhDuAnDto>> GetAdjustmentsAsync(Guid id);
}
