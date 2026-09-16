using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace demo1.Services.Interfaces.SubServices;

public interface IDuAnCascadeService
{
    Task<bool> DeleteAsync(Guid id);
    Task<bool> SoftDeleteAsync(Guid id);
    Task<bool> SoftDeleteAsync(IEnumerable<Guid> ids);
    Task<bool> RestoreAsync(Guid id);
    Task<bool> RestoreAsync(IEnumerable<Guid> ids);
}
