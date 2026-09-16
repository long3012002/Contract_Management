using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces.SubServices;

public interface IDuAnNguonLinkService
{
    Task<IReadOnlyList<DuAnNguonSummaryDto>> GetSourceProjectsByProjectIdAsync(Guid id);
    Task PopulateSourceProjectsAsync(List<DuAnDto> dtos);
    Task<HashSet<Guid>> GetLinkedSourceProjectIdsAsync(Guid? excludeTrienKhaiProjectId = null);
}
