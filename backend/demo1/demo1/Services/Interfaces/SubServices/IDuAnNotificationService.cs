using System;
using System.Threading.Tasks;
using demo1.Entity;

namespace demo1.Services.Interfaces.SubServices;

public interface IDuAnNotificationService
{
    Task<bool> ChangeOwnerAsync(Guid projectId, Guid newOwnerId);
    Task NotifyProjectOwnerAssignedAsync(DuAn project, Guid newOwnerId, string? actorName = null);
}

