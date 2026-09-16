using System;
using System.Threading.Tasks;

namespace demo1.Services.Interfaces.SubServices;

public interface IDuAnNotificationService
{
    Task<bool> ChangeOwnerAsync(Guid projectId, Guid newOwnerId);
}
