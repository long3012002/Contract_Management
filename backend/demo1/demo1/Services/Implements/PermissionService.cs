using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.DTOs;
using demo1.DTOs.Permission;
using demo1.Entity;
using demo1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements
{
    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _context;

        public PermissionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasPermissionAsync(Guid userId, string featureCode, string entityName, string entityId, string action)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null) return false;
            if (user.IsSystemAdmin) return true;

            var actCode = NormalizeActionCode(action);
            if (actCode == "VIEW" || actCode == "CREATE") return true;

            return await _context.UserPermissions
                .AsNoTracking()
                .Include(up => up.Permission)
                .AnyAsync(up =>
                    up.UserId == userId &&
                    (string.IsNullOrEmpty(featureCode) || up.FeatureCode == featureCode) &&
                    (string.IsNullOrEmpty(entityName) || up.EntityName.ToLower() == entityName.ToLower()) &&
                    up.EntityId == entityId &&
                    up.Permission != null && up.Permission.Code == actCode);
        }

        public async Task<PermissionRequestDto> CreateRequestAsync(Guid userId, CreatePermissionRequestDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new KeyNotFoundException("Không tìm thấy người dùng.");

            Guid? duAnId = dto.DuAnId;
            if (!duAnId.HasValue && dto.EntityName.Equals("DuAn", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(dto.EntityId, out var parsedDuAnId))
            {
                duAnId = parsedDuAnId;
            }

            var reqActionCode = NormalizeActionCode(dto.RequestedAction);

            // Find requested permission catalog entry
            var targetPermCatalog = await _context.Permissions.FirstOrDefaultAsync(p => p.Code == reqActionCode)
                                    ?? await _context.Permissions.FirstOrDefaultAsync(p => p.Code == "EDIT");

            if (targetPermCatalog == null)
            {
                throw new InvalidOperationException("Danh mục quyền hệ thống chưa được khởi tạo.");
            }

            // Check if user already has this specific permission
            var userPerm = await _context.UserPermissions
                .Include(up => up.Permission)
                .FirstOrDefaultAsync(up =>
                    up.UserId == userId &&
                    up.PermissionId == targetPermCatalog.Id &&
                    (duAnId.HasValue && up.DuAnId == duAnId.Value || (up.EntityName == dto.EntityName && up.EntityId == dto.EntityId)));

            // Check if there is already a pending request for the same entity and action
            var existingPending = await _context.PermissionRequests
                .Include(pr => pr.RequestedPermission)
                .FirstOrDefaultAsync(pr =>
                    pr.UserId == userId &&
                    pr.EntityName == dto.EntityName &&
                    pr.EntityId == dto.EntityId &&
                    pr.RequestedAction == reqActionCode &&
                    pr.Status == "Pending");

            if (existingPending != null)
            {
                existingPending.DuAnId = duAnId;
                existingPending.PermissionId = userPerm?.Id;
                existingPending.RequestedPermissionId = targetPermCatalog.Id;
                await _context.SaveChangesAsync();
                return MapToRequestDto(existingPending, user, null, targetPermCatalog);
            }

            var request = new PermissionRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FeatureCode = dto.FeatureCode ?? string.Empty,
                EntityName = dto.EntityName ?? string.Empty,
                EntityId = dto.EntityId ?? string.Empty,
                DuAnId = duAnId,
                PermissionId = userPerm?.Id,
                RequestedPermissionId = targetPermCatalog.Id,
                EntityTitle = dto.EntityTitle ?? string.Empty,
                RequestedAction = reqActionCode,
                Reason = dto.Reason ?? string.Empty,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.PermissionRequests.Add(request);
            await _context.SaveChangesAsync();

            return MapToRequestDto(request, user, null, targetPermCatalog);
        }

        public async Task<IEnumerable<PermissionRequestDto>> GetUserRequestsAsync(Guid userId)
        {
            var requests = await _context.PermissionRequests
                .Include(r => r.User)
                .Include(r => r.Reviewer)
                .Include(r => r.RequestedPermission)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return requests.Select(r => MapToRequestDto(r, r.User, r.Reviewer, r.RequestedPermission));
        }

        public async Task<PagedResult<PermissionRequestDto>> GetAllRequestsAsync(string? status, string? search, int page = 1, int pageSize = 20)
        {
            var query = _context.PermissionRequests
                .Include(r => r.User)
                .Include(r => r.Reviewer)
                .Include(r => r.RequestedPermission)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status.ToLower() == status.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(r =>
                    r.EntityTitle.ToLower().Contains(s) ||
                    (r.User != null && r.User.FullName.ToLower().Contains(s)) ||
                    (r.User != null && r.User.Username.ToLower().Contains(s)) ||
                    r.Reason.ToLower().Contains(s));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = items.Select(r => MapToRequestDto(r, r.User, r.Reviewer, r.RequestedPermission)).ToList();

            return new PagedResult<PermissionRequestDto>
            {
                Items = dtos,
                TotalItems = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PermissionRequestDto> ReviewRequestAsync(Guid requestId, Guid reviewerId, ReviewPermissionRequestDto dto)
        {
            var request = await _context.PermissionRequests
                .Include(r => r.User)
                .Include(r => r.RequestedPermission)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) throw new KeyNotFoundException("Không tìm thấy yêu cầu cấp quyền.");

            var reviewer = await _context.Users.FirstOrDefaultAsync(u => u.Id == reviewerId);

            request.Status = dto.IsApproved ? "Approved" : "Rejected";
            request.ReviewerId = reviewerId;
            request.ReviewNote = dto.ReviewNote;
            request.ReviewedAt = DateTime.UtcNow;

            if (dto.IsApproved)
            {
                var reqActionCode = NormalizeActionCode(request.RequestedAction);
                var permCatalog = request.RequestedPermission
                                  ?? await _context.Permissions.FirstOrDefaultAsync(p => p.Code == reqActionCode)
                                  ?? await _context.Permissions.FirstOrDefaultAsync(p => p.Code == "EDIT");

                if (permCatalog != null)
                {
                    var existingPerm = await _context.UserPermissions.FirstOrDefaultAsync(up =>
                        up.UserId == request.UserId &&
                        up.PermissionId == permCatalog.Id &&
                        (request.DuAnId.HasValue && up.DuAnId == request.DuAnId.Value || (up.EntityName == request.EntityName && up.EntityId == request.EntityId)));

                    if (existingPerm == null)
                    {
                        existingPerm = new UserPermission
                        {
                            Id = Guid.NewGuid(),
                            UserId = request.UserId,
                            PermissionId = permCatalog.Id,
                            FeatureCode = request.FeatureCode,
                            EntityName = request.EntityName,
                            EntityId = request.EntityId,
                            DuAnId = request.DuAnId,
                            GrantedAt = DateTime.UtcNow,
                            GrantedByUserId = reviewerId
                        };
                        _context.UserPermissions.Add(existingPerm);
                    }
                    else
                    {
                        existingPerm.GrantedAt = DateTime.UtcNow;
                        existingPerm.GrantedByUserId = reviewerId;
                    }

                    request.PermissionId = existingPerm.Id;
                }
            }

            // Create notification for requester
            var notiStatusText = dto.IsApproved ? "được phê duyệt" : "bị từ chối";
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Title = $"Yêu cầu cấp quyền {notiStatusText}",
                Content = $"Yêu cầu cấp quyền cho bản ghi '{request.EntityTitle}' đã {notiStatusText} bởi quản trị viên. {(!string.IsNullOrEmpty(dto.ReviewNote) ? $"Ghi chú: {dto.ReviewNote}" : "")}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            return MapToRequestDto(request, request.User, reviewer, request.RequestedPermission);
        }

        public async Task<UserPermissionDto> GrantUserPermissionAsync(Guid adminId, CreateUserPermissionDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
            if (user == null) throw new KeyNotFoundException("Không tìm thấy người dùng.");

            var permCatalog = await _context.Permissions.FirstOrDefaultAsync(p => p.Id == dto.PermissionId);
            if (permCatalog == null) throw new KeyNotFoundException("Không tìm thấy quyền trong danh mục.");

            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminId);

            Guid? duAnId = dto.DuAnId;
            if (!duAnId.HasValue && dto.EntityName.Equals("DuAn", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(dto.EntityId, out var parsedId))
            {
                duAnId = parsedId;
            }

            var existingPerm = await _context.UserPermissions.FirstOrDefaultAsync(up =>
                up.UserId == dto.UserId &&
                up.PermissionId == dto.PermissionId &&
                (duAnId.HasValue && up.DuAnId == duAnId.Value || (up.EntityName == dto.EntityName && up.EntityId == dto.EntityId)));

            if (existingPerm != null)
            {
                existingPerm.DuAnId = duAnId;
                existingPerm.GrantedAt = DateTime.UtcNow;
                existingPerm.GrantedByUserId = adminId;
                await _context.SaveChangesAsync();
                return MapToUserPermissionDto(existingPerm, user, permCatalog, admin?.Username);
            }

            var perm = new UserPermission
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                PermissionId = dto.PermissionId,
                FeatureCode = dto.FeatureCode,
                EntityName = dto.EntityName,
                EntityId = dto.EntityId,
                DuAnId = duAnId,
                GrantedAt = DateTime.UtcNow,
                GrantedByUserId = adminId
            };

            _context.UserPermissions.Add(perm);
            await _context.SaveChangesAsync();

            return MapToUserPermissionDto(perm, user, permCatalog, admin?.Username);
        }

        public async Task<bool> RevokeUserPermissionAsync(Guid permissionId)
        {
            var perm = await _context.UserPermissions.FirstOrDefaultAsync(up => up.Id == permissionId);
            if (perm == null) return false;

            _context.UserPermissions.Remove(perm);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UserPermissionDto>> GetUserPermissionsAsync(Guid? userId, string? featureCode)
        {
            var query = _context.UserPermissions
                .Include(up => up.User)
                .Include(up => up.Permission)
                .Include(up => up.GrantedByUser)
                .AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(up => up.UserId == userId.Value);
            }

            if (!string.IsNullOrWhiteSpace(featureCode))
            {
                query = query.Where(up => up.FeatureCode == featureCode);
            }

            var items = await query.OrderByDescending(up => up.GrantedAt).ToListAsync();

            return items.Select(up => MapToUserPermissionDto(up, up.User, up.Permission, up.GrantedByUser?.Username));
        }

        public async Task<DuAnPermissionCheckDto> GetDuAnPermissionAsync(Guid userId, Guid duAnId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null)
            {
                throw new KeyNotFoundException("Không tìm thấy người dùng.");
            }

            var duAnStr = duAnId.ToString();

            if (user.IsSystemAdmin)
            {
                var allPerms = await _context.Permissions.AsNoTracking().ToListAsync();
                return new DuAnPermissionCheckDto
                {
                    DuAnId = duAnId,
                    UserId = userId,
                    IsAdmin = true,
                    HasPermission = true,
                    CanEdit = true,
                    CanDelete = true,
                    GrantedPermissionCodes = allPerms.Select(p => p.Code).ToList(),
                    GrantedPermissionIds = allPerms.Select(p => p.Id).ToList(),
                    RequestStatus = "Approved",
                    RequestId = null
                };
            }

            var grantedUserPerms = await _context.UserPermissions
                .AsNoTracking()
                .Include(up => up.Permission)
                .Where(up => up.UserId == userId && (up.DuAnId == duAnId || up.EntityId == duAnStr))
                .ToListAsync();

            var grantedCodes = grantedUserPerms.Where(p => p.Permission != null).Select(p => p.Permission!.Code).Distinct().ToList();
            var grantedIds = grantedUserPerms.Select(p => p.PermissionId).Distinct().ToList();

            var canEdit = grantedCodes.Contains("EDIT");
            var canDelete = grantedCodes.Contains("DELETE");

            var latestRequest = await _context.PermissionRequests
                .AsNoTracking()
                .Where(pr => pr.UserId == userId && (pr.DuAnId == duAnId || pr.EntityId == duAnStr))
                .OrderByDescending(pr => pr.CreatedAt)
                .FirstOrDefaultAsync();

            return new DuAnPermissionCheckDto
            {
                DuAnId = duAnId,
                UserId = userId,
                IsAdmin = false,
                HasPermission = canEdit || canDelete || grantedCodes.Count > 0,
                CanEdit = canEdit,
                CanDelete = canDelete,
                GrantedPermissionCodes = grantedCodes,
                GrantedPermissionIds = grantedIds,
                RequestStatus = latestRequest?.Status ?? (grantedUserPerms.Any() ? "Approved" : "None"),
                RequestId = latestRequest?.Id
            };
        }

        public async Task<IEnumerable<PermissionCatalogDto>> GetPermissionCatalogAsync()
        {
            var items = await _context.Permissions.AsNoTracking().OrderBy(p => p.Code).ToListAsync();
            return items.Select(p => new PermissionCatalogDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description
            });
        }

        private static string NormalizeActionCode(string action)
        {
            if (string.IsNullOrWhiteSpace(action)) return "EDIT";
            var act = action.Trim().ToUpper();
            if (act == "UPDATE" || act == "PUT" || act == "EDIT") return "EDIT";
            if (act == "DELETE" || act == "REMOVE") return "DELETE";
            if (act == "CREATE" || act == "POST") return "CREATE";
            if (act == "VIEW" || act == "GET") return "VIEW";
            return act;
        }

        private static PermissionRequestDto MapToRequestDto(PermissionRequest r, User? user, User? reviewer, Permission? requestedPerm)
        {
            return new PermissionRequestDto
            {
                Id = r.Id,
                UserId = r.UserId,
                Username = user?.Username ?? string.Empty,
                UserFullName = user?.FullName ?? string.Empty,
                FeatureCode = r.FeatureCode,
                EntityName = r.EntityName,
                EntityId = r.EntityId,
                DuAnId = r.DuAnId,
                PermissionId = r.PermissionId,
                RequestedPermissionId = r.RequestedPermissionId,
                RequestedPermissionCode = requestedPerm?.Code ?? r.RequestedAction,
                RequestedPermissionName = requestedPerm?.Name ?? r.RequestedAction,
                EntityTitle = r.EntityTitle,
                RequestedAction = r.RequestedAction,
                Reason = r.Reason,
                Status = r.Status,
                ReviewerId = r.ReviewerId,
                ReviewerName = reviewer?.FullName ?? reviewer?.Username,
                ReviewNote = r.ReviewNote,
                ReviewedAt = r.ReviewedAt,
                CreatedAt = r.CreatedAt
            };
        }

        private static UserPermissionDto MapToUserPermissionDto(UserPermission up, User? user, Permission? perm, string? grantedByUsername)
        {
            return new UserPermissionDto
            {
                Id = up.Id,
                UserId = up.UserId,
                Username = user?.Username ?? string.Empty,
                UserFullName = user?.FullName ?? string.Empty,
                PermissionId = up.PermissionId,
                PermissionCode = perm?.Code ?? string.Empty,
                PermissionName = perm?.Name ?? string.Empty,
                FeatureCode = up.FeatureCode,
                EntityName = up.EntityName,
                EntityId = up.EntityId,
                DuAnId = up.DuAnId,
                GrantedAt = up.GrantedAt,
                GrantedByUsername = grantedByUsername
            };
        }
    }
}
