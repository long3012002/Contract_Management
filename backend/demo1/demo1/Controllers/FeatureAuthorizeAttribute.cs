using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using demo1.Data;
using Microsoft.AspNetCore.Http;

namespace demo1.Controllers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class FeatureAuthorizeAttribute : TypeFilterAttribute
    {
        public FeatureAuthorizeAttribute(string featureCode) : base(typeof(FeatureAuthorizeFilter))
        {
            Arguments = new object[] { featureCode };
        }
    }

    public class FeatureAuthorizeFilter : IAsyncAuthorizationFilter
    {
        private readonly string _featureCode;
        private readonly AppDbContext _dbContext;

        public FeatureAuthorizeFilter(string featureCode, AppDbContext dbContext)
        {
            _featureCode = featureCode;
            _dbContext = dbContext;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var username = user.Identity.Name;
            if (string.IsNullOrEmpty(username))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            if (dbUser == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            // System Admin has full unrestricted access
            if (dbUser.IsSystemAdmin)
            {
                return;
            }

            var httpMethod = context.HttpContext.Request.Method.ToUpper();

            // 1. GET requests: check if they are project owner, or if they have VIEW permission
            if (httpMethod == "GET")
            {
                var routeValues = context.RouteData.Values;
                string? entityId = null;
                if (routeValues.ContainsKey("id") && routeValues["id"] != null)
                {
                    entityId = routeValues["id"]?.ToString();
                }

                if (string.IsNullOrEmpty(entityId))
                {
                    return; // Listing endpoint, handled by service-level filtering
                }

                if (await IsProjectOwnerAsync(dbUser.Id, entityId))
                {
                    return;
                }

                Guid? duAnId = null;
                if (Guid.TryParse(entityId, out var parsedEntityId))
                {
                    if (_featureCode == "DU_AN") duAnId = parsedEntityId;
                    else if (_featureCode == "GOI_THAU")
                    {
                        var gt = await _dbContext.GoiThaus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == parsedEntityId);
                        duAnId = gt?.DuAnId;
                        if (duAnId == null)
                        {
                            var cv = await _dbContext.CongViecGoiThaus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == parsedEntityId);
                            if (cv != null)
                            {
                                var pgt = await _dbContext.GoiThaus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cv.GoiThauId);
                                duAnId = pgt?.DuAnId;
                            }
                        }
                    }
                    else if (_featureCode == "QUAN_LY_HOP_DONG")
                    {
                        var hd = await _dbContext.HopDongs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == parsedEntityId);
                        duAnId = hd?.DuAnId;
                    }
                }

                var hasViewPermission = await _dbContext.UserPermissions
                    .AsNoTracking()
                    .AnyAsync(up =>
                        up.UserId == dbUser.Id &&
                        (up.FeatureCode == _featureCode || up.FeatureCode == string.Empty) &&
                        (up.EntityId == entityId || (duAnId.HasValue && up.DuAnId == duAnId.Value)));

                if (!hasViewPermission)
                {
                    context.Result = new JsonResult(new
                    {
                        Message = "Bạn không có quyền xem bản ghi này. Vui lòng liên hệ chủ dự án hoặc quản trị viên.",
                        RequiresPermissionRequest = true,
                        FeatureCode = _featureCode,
                        EntityId = entityId,
                        RequiredPermissionCode = "VIEW"
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }
                return;
            }

            // Requirement: Creating new items requires explicit CREATE UserPermission for the feature
            if (httpMethod == "POST")
            {
                var requiredPermCode = "CREATE";

                // High-performance Lookup on UserPermissions + Permission Catalog Code
                var hasPermission = await _dbContext.UserPermissions
                    .AsNoTracking()
                    .Include(up => up.Permission)
                    .AnyAsync(up =>
                        up.UserId == dbUser.Id &&
                        (up.FeatureCode == _featureCode || up.FeatureCode == string.Empty) &&
                        up.Permission != null && up.Permission.Code == requiredPermCode);

                if (!hasPermission)
                {
                    context.Result = new JsonResult(new
                    {
                        Message = "Bạn chưa có quyền tạo mới trên tính năng này. Vui lòng gửi yêu cầu cấp quyền.",
                        RequiresPermissionRequest = true,
                        FeatureCode = _featureCode,
                        EntityId = string.Empty,
                        RequiredPermissionCode = requiredPermCode
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }
            }

            // Requirement: Editing/Deleting specific record requires explicit UserPermission
            if (httpMethod == "PUT" || httpMethod == "PATCH" || httpMethod == "DELETE")
            {
                var routeValues = context.RouteData.Values;
                string? entityId = null;
                if (routeValues.ContainsKey("id") && routeValues["id"] != null)
                {
                    entityId = routeValues["id"]?.ToString();
                }

                if (string.IsNullOrEmpty(entityId))
                {
                    return;
                }

                if (await IsProjectOwnerAsync(dbUser.Id, entityId))
                {
                    return; // Project Owner has unrestricted edit/delete access
                }

                var requiredPermCode = (httpMethod == "DELETE") ? "DELETE" : "EDIT";

                // High-performance Composite Index Lookup on UserPermissions + Permission Catalog Code
                var hasPermission = await _dbContext.UserPermissions
                    .AsNoTracking()
                    .Include(up => up.Permission)
                    .AnyAsync(up =>
                        up.UserId == dbUser.Id &&
                        (up.FeatureCode == _featureCode || up.FeatureCode == string.Empty) &&
                        up.EntityId == entityId &&
                        up.Permission != null && up.Permission.Code == requiredPermCode);

                if (!hasPermission)
                {
                    context.Result = new JsonResult(new
                    {
                        Message = $"Bạn chưa có quyền { (requiredPermCode == "DELETE" ? "xóa" : "chỉnh sửa") } trên bản ghi này. Vui lòng gửi yêu cầu cấp quyền.",
                        RequiresPermissionRequest = true,
                        FeatureCode = _featureCode,
                        EntityId = entityId,
                        RequiredPermissionCode = requiredPermCode
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }
            }
        }

        private async Task<bool> IsProjectOwnerAsync(Guid userId, string entityIdStr)
        {
            if (!Guid.TryParse(entityIdStr, out var entityId))
            {
                return false;
            }

            // Check DuAn
            var isDuAnCreator = await _dbContext.DuAns.AnyAsync(da => da.Id == entityId && da.CreatedByUserId == userId);
            if (isDuAnCreator) return true;

            // Check GoiThau
            var goiThau = await _dbContext.GoiThaus.AsNoTracking().FirstOrDefaultAsync(gt => gt.Id == entityId);
            if (goiThau != null && goiThau.DuAnId.HasValue)
            {
                var isOwner = await _dbContext.DuAns.AnyAsync(da => da.Id == goiThau.DuAnId.Value && da.CreatedByUserId == userId);
                if (isOwner) return true;
            }

            // Check HopDong
            var hopDong = await _dbContext.HopDongs.AsNoTracking().FirstOrDefaultAsync(hd => hd.Id == entityId);
            if (hopDong != null && hopDong.DuAnId.HasValue)
            {
                var isOwner = await _dbContext.DuAns.AnyAsync(da => da.Id == hopDong.DuAnId.Value && da.CreatedByUserId == userId);
                if (isOwner) return true;
            }

            // Check CongViecGoiThau
            var congViec = await _dbContext.CongViecGoiThaus.AsNoTracking().FirstOrDefaultAsync(cv => cv.Id == entityId);
            if (congViec != null)
            {
                var parentGoiThau = await _dbContext.GoiThaus.AsNoTracking().FirstOrDefaultAsync(gt => gt.Id == congViec.GoiThauId);
                if (parentGoiThau != null && parentGoiThau.DuAnId.HasValue)
                {
                    var isOwner = await _dbContext.DuAns.AnyAsync(da => da.Id == parentGoiThau.DuAnId.Value && da.CreatedByUserId == userId);
                    if (isOwner) return true;
                }
            }

            // Check CommentCongViecGoiThau
            var comment = await _dbContext.CommentCongViecGoiThaus.AsNoTracking().FirstOrDefaultAsync(c => c.Id == entityId);
            if (comment != null)
            {
                var parentCongViec = await _dbContext.CongViecGoiThaus.AsNoTracking().FirstOrDefaultAsync(cv => cv.Id == comment.CongViecGoiThauId);
                if (parentCongViec != null)
                {
                    var parentGoiThau = await _dbContext.GoiThaus.AsNoTracking().FirstOrDefaultAsync(gt => gt.Id == parentCongViec.GoiThauId);
                    if (parentGoiThau != null && parentGoiThau.DuAnId.HasValue)
                    {
                        var isOwner = await _dbContext.DuAns.AnyAsync(da => da.Id == parentGoiThau.DuAnId.Value && da.CreatedByUserId == userId);
                        if (isOwner) return true;
                    }
                }
            }

            // Check License
            var license = await _dbContext.Licenses.AsNoTracking().FirstOrDefaultAsync(l => l.Id == entityId);
            if (license != null)
            {
                var isOwner = await _dbContext.DuAns.AnyAsync(da => da.Id == license.DuAnId && da.CreatedByUserId == userId);
                if (isOwner) return true;
            }

            return false;
        }
    }
}
