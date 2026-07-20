using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface ILicenseService : ICrudService<LicenseDto, CreateLicenseDto, UpdateLicenseDto>
{
    Task<PagedResult<LicenseDto>> GetByDuAnIdAsync(Guid duAnId, string? search, int page, int pageSize);
    Task<IReadOnlyList<LicenseDto>> GetExpiringLicensesAsync(int? daysThreshold = null);
    Task<LicenseSummaryDto> GetLicenseSummaryAsync(Guid? duAnId = null);
}
