using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Entity;

namespace demo1.Services.Interfaces
{
    public interface IAdminService
    {
        Task<bool> IsSystemAdminAsync(string username);
        Task<IEnumerable<Role>> GetRolesAsync();
        Task<Role> CreateRoleAsync(CreateRoleDto dto);
        Task<Role> UpdateRoleAsync(Guid roleId, UpdateRoleDto dto);
        Task<IEnumerable<Feature>> GetFeaturesAsync();
        Task<Feature> CreateFeatureAsync(CreateFeatureDto dto);
        Task<Feature> UpdateFeatureAsync(Guid featureId, UpdateFeatureDto dto);
        Task DeleteFeatureAsync(Guid featureId);
        Task<IEnumerable<RolePermissionDto>> GetRolePermissionsAsync(Guid roleId);
        Task UpdateRolePermissionsAsync(Guid roleId, List<UpdateRolePermissionDto> permissions);
        Task<PagedResult<UserWithRolesDto>> GetUsersWithRolesAsync(string? search, int page, int pageSize);
        Task<IEnumerable<Guid>> GetUserRolesAsync(Guid userId);
        Task UpdateUserRolesAsync(Guid userId, UserRolesUpdateDto dto);
    }
}
