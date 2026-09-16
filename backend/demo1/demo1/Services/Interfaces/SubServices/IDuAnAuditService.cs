using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.Entity;

namespace demo1.Services.Interfaces.SubServices;

public interface IDuAnAuditService
{
    Task<IReadOnlyList<AuditLog>> GetAuditLogsByProjectIdAsync(Guid id);
}
