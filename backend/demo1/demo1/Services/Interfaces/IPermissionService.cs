using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.DTOs.Permission;

namespace demo1.Services.Interfaces
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(Guid userId, string featureCode, string entityName, string entityId, string action);
        Task<PermissionRequestDto> CreateRequestAsync(Guid userId, CreatePermissionRequestDto dto);
        Task<IEnumerable<PermissionRequestDto>> GetUserRequestsAsync(Guid userId);
        Task<PagedResult<PermissionRequestDto>> GetAllRequestsAsync(string? status, string? search, int page = 1, int pageSize = 20);
        Task<PermissionRequestDto> ReviewRequestAsync(Guid requestId, Guid reviewerId, ReviewPermissionRequestDto dto);
        Task<UserPermissionDto> GrantUserPermissionAsync(Guid adminId, CreateUserPermissionDto dto);
        Task<bool> RevokeUserPermissionAsync(Guid permissionId);
        Task<IEnumerable<UserPermissionDto>> GetUserPermissionsAsync(Guid? userId, string? featureCode);
        Task<DuAnPermissionCheckDto> GetDuAnPermissionAsync(Guid userId, Guid duAnId);
        Task<IEnumerable<PermissionCatalogDto>> GetPermissionCatalogAsync();
    }
}
