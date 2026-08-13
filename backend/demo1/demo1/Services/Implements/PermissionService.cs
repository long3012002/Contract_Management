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
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(AppDbContext context, ICurrentUserService currentUserService, ILogger<PermissionService> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(Guid userId, string featureCode, string entityName, string entityId, string action)
        {
            try
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
                if (user == null) return false;
                if (user.IsSystemAdmin) return true;

                var actCode = NormalizeActionCode(action);
                if (actCode == "VIEW") return true;

                // Check if user is Project Owner or Related User (Stakeholder)
                if (Guid.TryParse(entityId, out var parsedEntityId))
                {
                    var isProjectOwner = await _context.DuAns.AsNoTracking().AnyAsync(da => da.Id == parsedEntityId && da.CreatedByUserId == userId);
                    if (isProjectOwner) return true;

                    var isRelatedUser = await _context.CongViecNguoiLienQuans.AsNoTracking()
                        .AnyAsync(n => n.UserId == userId && (n.CongViecGoiThauId == parsedEntityId || (n.CongViecGoiThau != null && n.CongViecGoiThau.GoiThau != null && (n.CongViecGoiThau.GoiThauId == parsedEntityId || n.CongViecGoiThau.GoiThau.DuAnId == parsedEntityId))));
                    if (isRelatedUser) return true;
                }

                var normFeatureCode = NormalizeFeatureCode(featureCode);
                return await _context.UserPermissions
                    .AsNoTracking()
                    .Include(up => up.Permission)
                    .AnyAsync(up =>
                        up.UserId == userId &&
                        (string.IsNullOrEmpty(normFeatureCode) || up.FeatureCode == normFeatureCode || (normFeatureCode == "DU_AN" && up.FeatureCode == "PROJECT")) &&
                        (string.IsNullOrEmpty(entityName) || up.EntityName.ToLower() == entityName.ToLower()) &&
                        up.EntityId == entityId &&
                        up.Permission != null && up.Permission.Code == actCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong HasPermissionAsync cho UserId {UserId}.", userId);
                throw;
            }
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
            var currentUsername = _currentUserService.GetUsername();
            var currentUser = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == currentUsername);

            var query = _context.PermissionRequests
                .Include(r => r.User)
                .Include(r => r.Reviewer)
                .Include(r => r.RequestedPermission)
                .AsQueryable();

            if (currentUser != null && !currentUser.IsSystemAdmin)
            {
                var callerChucVu = currentUser.IdChucVu.HasValue 
                    ? await _context.ChucVus.FindAsync(currentUser.IdChucVu.Value) 
                    : null;
                var callerLevel = callerChucVu?.Level ?? 999;

                query = from r in query
                        join u in _context.Users on r.UserId equals u.Id
                        join cv in _context.ChucVus on u.IdChucVu equals cv.Id into ucv
                        from cv in ucv.DefaultIfEmpty()
                        where r.UserId == currentUser.Id || (u.IsSystemAdmin == false && (u.IdChucVu == null || cv.Level >= callerLevel))
                        select r;
            }

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

                    if (request.FeatureCode == "DU_AN" && request.DuAnId.HasValue)
                    {
                        await CascadeProjectPermissionsAsync(request.UserId, permCatalog.Id, request.DuAnId.Value, reviewerId);
                    }
                }
            }

            // Create notification for requester
            var notiStatusText = dto.IsApproved ? "được duyệt" : "bị từ chối";
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Title = $"Quyền truy cập: { (dto.IsApproved ? "Được duyệt" : "Bị từ chối") }",
                Content = $"Yêu cầu quyền truy cập '{request.EntityTitle}' đã {notiStatusText}.{(!string.IsNullOrEmpty(dto.ReviewNote) ? $" Ghi chú: {dto.ReviewNote}" : "")}",
                FeatureCode = "PERMISSION_REQUEST",
                EntityName = "PermissionRequest",
                EntityId = request.Id.ToString(),
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
                if (dto.FeatureCode == "DU_AN" && duAnId.HasValue)
                {
                    await CascadeProjectPermissionsAsync(dto.UserId, dto.PermissionId, duAnId.Value, adminId);
                }
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
            if (dto.FeatureCode == "DU_AN" && duAnId.HasValue)
            {
                await CascadeProjectPermissionsAsync(dto.UserId, dto.PermissionId, duAnId.Value, adminId);
            }
            await _context.SaveChangesAsync();

            return MapToUserPermissionDto(perm, user, permCatalog, admin?.Username);
        }

        public async Task<IEnumerable<UserPermissionDto>> GrantUserPermissionsBatchAsync(Guid adminId, CreateBatchUserPermissionsDto dto)
        {
            if (dto.UserIds == null || !dto.UserIds.Any())
            {
                return Enumerable.Empty<UserPermissionDto>();
            }

            var permCatalog = await _context.Permissions.FirstOrDefaultAsync(p => p.Id == dto.PermissionId);
            if (permCatalog == null) throw new KeyNotFoundException("Không tìm thấy quyền trong danh mục.");

            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminId);

            Guid? duAnId = dto.DuAnId;
            if (!duAnId.HasValue && dto.EntityName.Equals("DuAn", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(dto.EntityId, out var parsedId))
            {
                duAnId = parsedId;
            }

            var users = await _context.Users.Where(u => dto.UserIds.Contains(u.Id)).ToListAsync();
            var foundUserIds = users.Select(u => u.Id).ToHashSet();
            var missingUserIds = dto.UserIds.Where(id => !foundUserIds.Contains(id)).ToList();
            if (missingUserIds.Any())
            {
                throw new KeyNotFoundException($"Không tìm thấy người dùng với các ID: {string.Join(", ", missingUserIds)}");
            }

            var result = new List<UserPermissionDto>();
            var now = DateTime.UtcNow;

            var existingPerms = await _context.UserPermissions
                .Where(up => dto.UserIds.Contains(up.UserId) && up.PermissionId == dto.PermissionId &&
                             (duAnId.HasValue && up.DuAnId == duAnId.Value || (up.EntityName == dto.EntityName && up.EntityId == dto.EntityId)))
                .ToListAsync();

            var existingPermsMap = existingPerms.ToDictionary(up => up.UserId);

            foreach (var user in users)
            {
                if (existingPermsMap.TryGetValue(user.Id, out var existingPerm))
                {
                    existingPerm.DuAnId = duAnId;
                    existingPerm.GrantedAt = now;
                    existingPerm.GrantedByUserId = adminId;
                    if (dto.FeatureCode == "DU_AN" && duAnId.HasValue)
                    {
                        await CascadeProjectPermissionsAsync(user.Id, dto.PermissionId, duAnId.Value, adminId);
                    }
                    result.Add(MapToUserPermissionDto(existingPerm, user, permCatalog, admin?.Username));
                }
                else
                {
                    var newPerm = new UserPermission
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        PermissionId = dto.PermissionId,
                        FeatureCode = dto.FeatureCode,
                        EntityName = dto.EntityName,
                        EntityId = dto.EntityId,
                        DuAnId = duAnId,
                        GrantedAt = now,
                        GrantedByUserId = adminId
                    };
                    _context.UserPermissions.Add(newPerm);
                    if (dto.FeatureCode == "DU_AN" && duAnId.HasValue)
                    {
                        await CascadeProjectPermissionsAsync(user.Id, dto.PermissionId, duAnId.Value, adminId);
                    }
                    result.Add(MapToUserPermissionDto(newPerm, user, permCatalog, admin?.Username));
                }
            }

            await _context.SaveChangesAsync();
            return result;
        }

        public async Task<bool> RevokeUserPermissionAsync(Guid permissionId)
        {
            var perm = await _context.UserPermissions.FirstOrDefaultAsync(up => up.Id == permissionId);
            if (perm == null) return false;

            _context.UserPermissions.Remove(perm);

            if (perm.FeatureCode == "DU_AN" && perm.DuAnId.HasValue)
            {
                var cascadedPerms = await _context.UserPermissions
                    .Where(up => up.UserId == perm.UserId &&
                                 up.PermissionId == perm.PermissionId &&
                                 up.DuAnId == perm.DuAnId &&
                                 (up.FeatureCode == "GOI_THAU" || up.FeatureCode == "QUAN_LY_HOP_DONG"))
                    .ToListAsync();

                if (cascadedPerms.Any())
                {
                    _context.UserPermissions.RemoveRange(cascadedPerms);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UserPermissionDto>> GetUserPermissionsAsync(Guid? userId, string? featureCode, bool includeChildren = true)
        {
            var currentUsername = _currentUserService.GetUsername();
            var currentUser = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == currentUsername);

            var targetUserId = userId ?? currentUser?.Id;

            var query = _context.UserPermissions
                .Include(up => up.User)
                .Include(up => up.Permission)
                .Include(up => up.GrantedByUser)
                .AsQueryable();

            if (currentUser != null && !currentUser.IsSystemAdmin)
            {
                var callerChucVu = currentUser.IdChucVu.HasValue 
                    ? await _context.ChucVus.FindAsync(currentUser.IdChucVu.Value) 
                    : null;
                var callerLevel = callerChucVu?.Level ?? 999;

                query = from up in query
                        join u in _context.Users on up.UserId equals u.Id
                        join cv in _context.ChucVus on u.IdChucVu equals cv.Id into ucv
                        from cv in ucv.DefaultIfEmpty()
                        where up.UserId == currentUser.Id || (u.IsSystemAdmin == false && (u.IdChucVu == null || cv.Level >= callerLevel))
                        select up;
            }

            if (userId.HasValue)
            {
                query = query.Where(up => up.UserId == userId.Value);
            }

            var rawFeatureCode = featureCode?.Trim();
            var normalizedFeatureCode = NormalizeFeatureCode(featureCode);
            if (!string.IsNullOrWhiteSpace(rawFeatureCode))
            {
                if (includeChildren && normalizedFeatureCode == "DU_AN")
                {
                    var parentAndChildren = new[] { "DU_AN", "PROJECT", "DUAN", "GOI_THAU", "PACKAGE", "GOITHAU", "QUAN_LY_HOP_DONG", "CONTRACT", "HOPDONG", "CONG_VIEC", "TASK", "CONGVIEC" };
                    query = query.Where(up => parentAndChildren.Contains(up.FeatureCode.ToUpper()));
                }
                else
                {
                    query = query.Where(up => 
                        up.FeatureCode == normalizedFeatureCode || 
                        up.FeatureCode.ToLower() == rawFeatureCode.ToLower() || 
                        (normalizedFeatureCode == "DU_AN" && (up.FeatureCode == "PROJECT" || up.FeatureCode == "DU_AN")) ||
                        (normalizedFeatureCode == "GOI_THAU" && (up.FeatureCode == "PACKAGE" || up.FeatureCode == "GOITHAU" || up.FeatureCode == "GOI_THAU")) ||
                        (normalizedFeatureCode == "QUAN_LY_HOP_DONG" && (up.FeatureCode == "CONTRACT" || up.FeatureCode == "HOPDONG" || up.FeatureCode == "HOP_DONG" || up.FeatureCode == "QUAN_LY_HOP_DONG")) ||
                        (normalizedFeatureCode == "CONG_VIEC" && (up.FeatureCode == "TASK" || up.FeatureCode == "CONGVIEC" || up.FeatureCode == "CONG_VIEC")) ||
                        (normalizedFeatureCode == "LICENSE" && (up.FeatureCode == "BAN_QUYEN" || up.FeatureCode == "LICENSE")) ||
                        (normalizedFeatureCode == "DOI_TAC" && (up.FeatureCode == "PARTNER" || up.FeatureCode == "DOI_TAC")) ||
                        (normalizedFeatureCode == "BAO_CAO" && (up.FeatureCode == "REPORT" || up.FeatureCode == "BAO_CAO"))
                    );
                }
            }

            var items = await query.OrderByDescending(up => up.GrantedAt).ToListAsync();
            var resultList = items.Select(up => MapToUserPermissionDto(up, up.User, up.Permission, up.GrantedByUser?.Username)).ToList();

            // Total synthesis for Project Owners & Stakeholders (NguoiLienQuan) so frontend menu & route guards grant access
            if (targetUserId.HasValue)
            {
                var targetUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == targetUserId.Value);
                if (targetUser != null)
                {
                    var ownedDuAnIds = await _context.DuAns.AsNoTracking()
                        .Where(da => da.CreatedByUserId == targetUserId.Value)
                        .Select(da => da.Id)
                        .ToListAsync();

                    var relatedDuAnIds = await _context.CongViecNguoiLienQuans.AsNoTracking()
                        .Where(n => n.UserId == targetUserId.Value && n.CongViecGoiThau != null && n.CongViecGoiThau.GoiThau != null && n.CongViecGoiThau.GoiThau.DuAnId.HasValue)
                        .Select(n => n.CongViecGoiThau!.GoiThau!.DuAnId!.Value)
                        .Distinct()
                        .ToListAsync();

                    var allProjectIds = ownedDuAnIds.Concat(relatedDuAnIds).Distinct().ToList();

                    if (allProjectIds.Any())
                    {
                        var permissionsCatalog = await _context.Permissions.AsNoTracking().ToListAsync();
                        var featuresToGrant = new[] { "DU_AN", "GOI_THAU", "QUAN_LY_HOP_DONG", "CONG_VIEC" };

                        foreach (var projId in allProjectIds)
                        {
                            foreach (var feat in featuresToGrant)
                            {
                                if (!string.IsNullOrWhiteSpace(rawFeatureCode))
                                {
                                    bool isMatch;
                                    if (includeChildren && normalizedFeatureCode == "DU_AN")
                                    {
                                        isMatch = true;
                                    }
                                    else
                                    {
                                        isMatch = string.Equals(normalizedFeatureCode, feat, StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(rawFeatureCode, feat, StringComparison.OrdinalIgnoreCase) ||
                                                   (feat == "DU_AN" && (string.Equals(rawFeatureCode, "PROJECT", StringComparison.OrdinalIgnoreCase) || string.Equals(rawFeatureCode, "DUAN", StringComparison.OrdinalIgnoreCase))) ||
                                                   (feat == "GOI_THAU" && (string.Equals(rawFeatureCode, "PACKAGE", StringComparison.OrdinalIgnoreCase) || string.Equals(rawFeatureCode, "GOITHAU", StringComparison.OrdinalIgnoreCase))) ||
                                                   (feat == "QUAN_LY_HOP_DONG" && (string.Equals(rawFeatureCode, "CONTRACT", StringComparison.OrdinalIgnoreCase) || string.Equals(rawFeatureCode, "HOPDONG", StringComparison.OrdinalIgnoreCase) || string.Equals(rawFeatureCode, "HOP_DONG", StringComparison.OrdinalIgnoreCase))) ||
                                                   (feat == "CONG_VIEC" && (string.Equals(rawFeatureCode, "TASK", StringComparison.OrdinalIgnoreCase) || string.Equals(rawFeatureCode, "CONGVIEC", StringComparison.OrdinalIgnoreCase)));
                                    }

                                    if (!isMatch)
                                    {
                                        continue;
                                    }
                                }

                                foreach (var perm in permissionsCatalog)
                                {
                                    bool exists = resultList.Any(r => r.UserId == targetUserId.Value && r.FeatureCode == feat && (r.DuAnId == projId || r.EntityId == projId.ToString()) && r.PermissionCode == perm.Code);
                                    if (!exists)
                                    {
                                        resultList.Add(new UserPermissionDto
                                        {
                                            Id = Guid.Empty,
                                            UserId = targetUserId.Value,
                                            Username = targetUser.Username,
                                            UserFullName = targetUser.FullName,
                                            PermissionId = perm.Id,
                                            PermissionCode = perm.Code,
                                            PermissionName = perm.Name,
                                            FeatureCode = feat,
                                            EntityName = feat == "DU_AN" ? "DuAn" : feat,
                                            EntityId = projId.ToString(),
                                            DuAnId = projId,
                                            GrantedAt = DateTime.UtcNow,
                                            GrantedByUsername = "System (Auto/Stakeholder)"
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return resultList;
        }

        public async Task<IEnumerable<GroupedUserPermissionDto>> GetGroupedUserPermissionsAsync(Guid? userId, string? featureCode, bool includeChildren = true)
        {
            var flatPermissions = await GetUserPermissionsAsync(userId, featureCode, includeChildren);
            var featureCatalog = (await GetFeatureCatalogAsync()).ToList();
            var featureMap = featureCatalog.ToDictionary(f => f.Code, StringComparer.OrdinalIgnoreCase);

            var groupedDict = new Dictionary<string, GroupedUserPermissionDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var perm in flatPermissions)
            {
                var normCode = NormalizeFeatureCode(perm.FeatureCode);
                if (string.IsNullOrEmpty(normCode)) normCode = perm.FeatureCode;

                if (!groupedDict.TryGetValue(normCode, out var group))
                {
                    var featInfo = featureMap.TryGetValue(normCode, out var info) ? info : null;
                    group = new GroupedUserPermissionDto
                    {
                        FeatureCode = normCode,
                        FeatureName = featInfo?.Name ?? normCode,
                        IsParent = (normCode == "DU_AN"),
                        ParentFeatureCode = (normCode == "GOI_THAU" || normCode == "QUAN_LY_HOP_DONG" || normCode == "CONG_VIEC") ? "DU_AN" : null,
                        Permissions = new List<UserPermissionDto>()
                    };
                    groupedDict[normCode] = group;
                }

                group.Permissions.Add(perm);
            }

            var sortedGroups = groupedDict.Values
                .OrderByDescending(g => g.IsParent)
                .ThenBy(g => g.FeatureCode)
                .ToList();

            return sortedGroups;
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

            var isProjectOwner = await _context.DuAns.AsNoTracking().AnyAsync(da => da.Id == duAnId && da.CreatedByUserId == userId);
            var isRelatedUser = await _context.CongViecNguoiLienQuans.AsNoTracking()
                .AnyAsync(n => n.UserId == userId && n.CongViecGoiThau != null && n.CongViecGoiThau.GoiThau != null && n.CongViecGoiThau.GoiThau.DuAnId == duAnId);

            if (isProjectOwner || isRelatedUser)
            {
                var allPerms = await _context.Permissions.AsNoTracking().ToListAsync();
                return new DuAnPermissionCheckDto
                {
                    DuAnId = duAnId,
                    UserId = userId,
                    IsAdmin = false,
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

        public async Task<IEnumerable<FeatureCatalogDto>> GetFeatureCatalogAsync()
        {
            var features = new List<FeatureCatalogDto>
            {
                new FeatureCatalogDto
                {
                    Code = "DU_AN",
                    Name = "Quản lý Dự án",
                    Description = "Tính năng quản lý thông tin các dự án công nghệ thông tin và đầu tư",
                    Aliases = new List<string> { "PROJECT", "PROJECTS", "DUAN" }
                },
                new FeatureCatalogDto
                {
                    Code = "GOI_THAU",
                    Name = "Quản lý Gói thầu",
                    Description = "Tính năng quản lý các gói thầu và công việc liên quan trong dự án",
                    Aliases = new List<string> { "PACKAGE", "PACKAGES", "GOITHAU" }
                },
                new FeatureCatalogDto
                {
                    Code = "QUAN_LY_HOP_DONG",
                    Name = "Quản lý Hợp đồng",
                    Description = "Tính năng quản lý hợp đồng, giá trị hợp đồng, phụ lục và đợt thanh toán",
                    Aliases = new List<string> { "CONTRACT", "CONTRACTS", "HOPDONG", "HOP_DONG", "QUANLYHOPDONG" }
                },
                new FeatureCatalogDto
                {
                    Code = "CONG_VIEC",
                    Name = "Quản lý Công việc",
                    Description = "Tính năng quản lý chi tiết các hạng mục công việc gói thầu",
                    Aliases = new List<string> { "TASK", "TASKS", "CONGVIEC" }
                },
                new FeatureCatalogDto
                {
                    Code = "DOI_TAC",
                    Name = "Quản lý Đối tác / Nhà thầu",
                    Description = "Tính năng quản lý thông tin nhà thầu, đối tác cung cấp dịch vụ",
                    Aliases = new List<string> { "PARTNER", "PARTNERS", "DOITAC" }
                },
                new FeatureCatalogDto
                {
                    Code = "BAO_CAO",
                    Name = "Báo cáo & Thống kê",
                    Description = "Tính năng tổng hợp báo cáo tình hình dự án, hợp đồng và tiến độ",
                    Aliases = new List<string> { "REPORT", "REPORTS", "BAOCAO" }
                },
                new FeatureCatalogDto
                {
                    Code = "LICENSE",
                    Name = "Quản lý Bản quyền / License",
                    Description = "Tính năng quản lý thông tin bản quyền phần mềm, hạn sử dụng license",
                    Aliases = new List<string> { "LICENSES", "BANQUYEN", "BAN_QUYEN" }
                }
            };

            return await Task.FromResult(features);
        }

        public static string NormalizeFeatureCode(string? featureCode)
        {
            if (string.IsNullOrWhiteSpace(featureCode)) return string.Empty;
            var code = featureCode.Trim().ToUpper();
            return code switch
            {
                "PROJECT" or "PROJECTS" or "DUAN" => "DU_AN",
                "GOITHAU" or "PACKAGE" or "PACKAGES" => "GOI_THAU",
                "CONTRACT" or "CONTRACTS" or "HOPDONG" or "HOP_DONG" or "QUANLYHOPDONG" => "QUAN_LY_HOP_DONG",
                "TASK" or "TASKS" or "CONGVIEC" => "CONG_VIEC",
                "PARTNER" or "PARTNERS" or "DOITAC" => "DOI_TAC",
                "REPORT" or "REPORTS" or "BAOCAO" => "BAO_CAO",
                "LICENSE" or "LICENSES" or "BANQUYEN" => "LICENSE",
                _ => code
            };
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

        private async Task CascadeProjectPermissionsAsync(Guid userId, Guid permissionId, Guid duAnId, Guid grantedByUserId)
        {
            var childFeatures = new List<(string FeatureCode, string EntityName)>
            {
                ("GOI_THAU", "GoiThau"),
                ("QUAN_LY_HOP_DONG", "HopDong")
            };

            foreach (var child in childFeatures)
            {
                var exists = await _context.UserPermissions.AnyAsync(up =>
                    up.UserId == userId &&
                    up.PermissionId == permissionId &&
                    up.FeatureCode == child.FeatureCode &&
                    up.DuAnId == duAnId);

                if (!exists)
                {
                    var childPerm = new UserPermission
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        PermissionId = permissionId,
                        FeatureCode = child.FeatureCode,
                        EntityName = child.EntityName,
                        EntityId = string.Empty,
                        DuAnId = duAnId,
                        GrantedAt = DateTime.UtcNow,
                        GrantedByUserId = grantedByUserId
                    };
                    _context.UserPermissions.Add(childPerm);
                }
            }
        }
    }
}
