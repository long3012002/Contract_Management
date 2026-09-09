using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using demo1.Data;
using Microsoft.AspNetCore.Http;

using demo1.Services.Implements;

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
            _featureCode = PermissionService.NormalizeFeatureCode(featureCode);
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
            // Project Owner anywhere has full operational access to business features
            var isProjectOwnerAnywhere = await IsProjectOwnerAnywhereAsync(dbUser.Id);
            if (isProjectOwnerAnywhere && (_featureCode == "DU_AN" || _featureCode == "GOI_THAU" || _featureCode == "QUAN_LY_HOP_DONG" || _featureCode == "CONG_VIEC" || _featureCode == "DOI_TAC" || _featureCode == "BAO_CAO"))
            {
                return;
            }

            var httpMethod = context.HttpContext.Request.Method.ToUpper();

            var routeValues = context.RouteData.Values;
            string? entityId = null;
            foreach (var key in routeValues.Keys)
            {
                if (key.Equals("id", StringComparison.OrdinalIgnoreCase) || key.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                {
                    var val = routeValues[key]?.ToString();
                    if (!string.IsNullOrEmpty(val) && Guid.TryParse(val, out _))
                    {
                        entityId = val;
                        break;
                    }
                }
            }

            Guid? duAnId = null;
            if (!string.IsNullOrEmpty(entityId) && Guid.TryParse(entityId, out var parsedEntityId))
            {
                if (_featureCode == "DU_AN")
                {
                    var exists = await _dbContext.DuAns.AnyAsync(x => x.Id == parsedEntityId);
                    if (!exists)
                    {
                        // Quy tắc: Kiểm tra sự tồn tại của dữ liệu (404) trước khi kiểm tra quyền truy cập (403)
                        // Nếu ID không tồn tại trong DB -> Cho đi tiếp vào Controller để Controller trả về 404 Not Found
                        return;
                    }
                    duAnId = parsedEntityId;
                }
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
                    var hd = await _dbContext.HopDongs.AsNoTracking()
                        .Include(x => x.LoaiHopDongNavigation)
                        .Include(x => x.GoiThau)
                        .FirstOrDefaultAsync(x => x.Id == parsedEntityId);
                    duAnId = hd?.DuAnId ?? hd?.GoiThau?.DuAnId;
                    if (duAnId == null && hd?.GoiThauId.HasValue == true)
                    {
                        var gt = await _dbContext.GoiThaus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == hd.GoiThauId.Value);
                        duAnId = gt?.DuAnId;
                    }
                    if (hd?.LoaiHopDongNavigation?.Code == "01" && dbUser.CanViewHopDong && httpMethod == "GET")
                    {
                        return; // Bypass immediately if they have the specific role and it's contract 01
                    }
                }
            }

            // 1. GET requests: check if they are project owner/related user, or if they have VIEW permission
            if (httpMethod == "GET")
            {
                if (string.IsNullOrEmpty(entityId))
                {
                    return; // Listing endpoint, handled by service-level filtering
                }

                if (await IsProjectOwnerOrRelatedUserAsync(dbUser.Id, entityId))
                {
                    return;
                }

                var validCodes = GetEquivalentFeatureCodes(_featureCode);
                var validViewActions = new[] { "VIEW", "EDIT", "CREATE", "DELETE", "ADMIN" };

                var hasViewPermission = await _dbContext.UserPermissions
                    .AsNoTracking()
                    .Include(up => up.Permission)
                    .AnyAsync(up =>
                        up.UserId == dbUser.Id &&
                        (
                            validCodes.Contains(up.FeatureCode) ||
                            (duAnId.HasValue && (up.DuAnId == duAnId.Value || up.EntityId == duAnId.Value.ToString()))
                        ) &&
                        (
                            up.EntityId == entityId ||
                            (duAnId.HasValue && (up.DuAnId == duAnId.Value || up.EntityId == duAnId.Value.ToString()))
                        ) &&
                        up.Permission != null && validViewActions.Contains(up.Permission.Code));

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

            // Requirement: Creating new items requires explicit CREATE UserPermission for the feature OR being Project Owner
            if (httpMethod == "POST")
            {
                if (!string.IsNullOrEmpty(entityId) && await IsProjectOwnerOrRelatedUserAsync(dbUser.Id, entityId, isReadOnlyCheck: true))
                {
                    return;
                }
                if (duAnId.HasValue && await IsProjectOwnerOrRelatedUserAsync(dbUser.Id, duAnId.Value.ToString(), isReadOnlyCheck: true))
                {
                    return;
                }

                var requiredPermCode = "CREATE";
                var validCodes = GetEquivalentFeatureCodes(_featureCode);

                // Composite Index Lookup on UserPermissions + Permission Catalog Code + DuAnId check if available
                var hasPermission = await _dbContext.UserPermissions
                    .AsNoTracking()
                    .Include(up => up.Permission)
                    .AnyAsync(up =>
                        up.UserId == dbUser.Id &&
                        validCodes.Contains(up.FeatureCode) &&
                        (!duAnId.HasValue || up.DuAnId == duAnId.Value) &&
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

            // Requirement: Editing/Deleting specific record requires explicit UserPermission OR being Project Owner / Related User (for tasks/comments)
            if (httpMethod == "PUT" || httpMethod == "PATCH" || httpMethod == "DELETE")
            {
                if (string.IsNullOrEmpty(entityId))
                {
                    return;
                }

                if (await IsProjectOwnerOrRelatedUserAsync(dbUser.Id, entityId, isReadOnlyCheck: false))
                {
                    return; // Project Owner or Related User (for assigned task/comment) has operational access
                }

                var requiredPermCode = (httpMethod == "DELETE") ? "DELETE" : "EDIT";
                var validCodes = GetEquivalentFeatureCodes(_featureCode);

                // High-performance Composite Index Lookup on UserPermissions + Permission Catalog Code
                var hasPermission = await _dbContext.UserPermissions
                    .AsNoTracking()
                    .Include(up => up.Permission)
                    .AnyAsync(up =>
                        up.UserId == dbUser.Id &&
                        (
                            (validCodes.Contains(up.FeatureCode) && up.EntityId == entityId) ||
                            (up.FeatureCode == "DU_AN" && duAnId.HasValue && up.DuAnId == duAnId.Value)
                        ) &&
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

        private static List<string> GetEquivalentFeatureCodes(string featureCode)
        {
            var norm = PermissionService.NormalizeFeatureCode(featureCode);
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "", featureCode, norm };

            switch (norm)
            {
                case "DU_AN":
                    codes.Add("DU_AN");
                    codes.Add("DUAN");
                    codes.Add("PROJECT");
                    codes.Add("PROJECTS");
                    break;
                case "GOI_THAU":
                    codes.Add("GOI_THAU");
                    codes.Add("GOITHAU");
                    codes.Add("PACKAGE");
                    codes.Add("PACKAGES");
                    break;
                case "QUAN_LY_HOP_DONG":
                    codes.Add("QUAN_LY_HOP_DONG");
                    codes.Add("QUANLYHOPDONG");
                    codes.Add("HOP_DONG");
                    codes.Add("HOPDONG");
                    codes.Add("CONTRACT");
                    codes.Add("CONTRACTS");
                    break;
                case "CONG_VIEC":
                    codes.Add("CONG_VIEC");
                    codes.Add("CONGVIEC");
                    codes.Add("TASK");
                    codes.Add("TASKS");
                    break;
                case "DOI_TAC":
                    codes.Add("DOI_TAC");
                    codes.Add("DOITAC");
                    codes.Add("PARTNER");
                    codes.Add("PARTNERS");
                    break;
                case "BAO_CAO":
                    codes.Add("BAO_CAO");
                    codes.Add("BAOCAO");
                    codes.Add("REPORT");
                    codes.Add("REPORTS");
                    break;
                case "LICENSE":
                    codes.Add("LICENSE");
                    codes.Add("LICENSES");
                    codes.Add("BANQUYEN");
                    break;
            }

            // Always include project codes as project-level permissions can grant access
            codes.Add("DU_AN");
            codes.Add("DUAN");
            codes.Add("PROJECT");
            codes.Add("PROJECTS");

            return codes.ToList();
        }

        private async Task<bool> IsProjectOwnerAnywhereAsync(Guid userId)
        {
            return await _dbContext.DuAns.AsNoTracking().AnyAsync(da => (da.CreatedByUserId == userId || da.ChuDuAnId == userId));
        }

        private async Task<bool> IsProjectOwnerOrRelatedUserAsync(Guid userId, string entityIdStr, bool isReadOnlyCheck = true)
        {
            if (!Guid.TryParse(entityIdStr, out var entityId))
            {
                return false;
            }

            // 1. Check DuAn: Access granted if Project Owner. Tagged in a task DOES NOT grant project access.
            var duAn = await _dbContext.DuAns.AsNoTracking().FirstOrDefaultAsync(da => da.Id == entityId);
            if (duAn != null)
            {
                if (duAn.CreatedByUserId == userId || duAn.ChuDuAnId == userId) return true;
            }

            // 2. Check GoiThau: Access granted if Project Owner (Full) OR tagged in a task belonging to THIS GoiThau (Read-only)
            var goiThau = await _dbContext.GoiThaus.AsNoTracking().FirstOrDefaultAsync(gt => gt.Id == entityId);
            if (goiThau != null)
            {
                if (goiThau.DuAnId.HasValue)
                {
                    var isOwner = await _dbContext.DuAns.AsNoTracking().AnyAsync(da => da.Id == goiThau.DuAnId.Value && (da.CreatedByUserId == userId || da.ChuDuAnId == userId));
                    if (isOwner) return true;
                }

                if (isReadOnlyCheck)
                {
                    var isRelatedToGoiThau = await _dbContext.CongViecNguoiLienQuans.AsNoTracking()
                        .AnyAsync(n => n.UserId == userId && n.CongViecGoiThau != null && n.CongViecGoiThau.GoiThauId == entityId);
                    if (isRelatedToGoiThau) return true;
                }
            }

            // 3. Check HopDong: Access granted if Project Owner (Full) OR tagged in a task belonging to the contract's GoiThau (Read-only)
            var hopDong = await _dbContext.HopDongs.AsNoTracking().FirstOrDefaultAsync(hd => hd.Id == entityId);
            if (hopDong != null)
            {
                var targetDuAnId = hopDong.DuAnId;
                if (!targetDuAnId.HasValue && hopDong.GoiThauId.HasValue)
                {
                    var parentGoiThau = await _dbContext.GoiThaus.AsNoTracking().FirstOrDefaultAsync(gt => gt.Id == hopDong.GoiThauId.Value);
                    targetDuAnId = parentGoiThau?.DuAnId;
                }

                if (targetDuAnId.HasValue)
                {
                    var isOwner = await _dbContext.DuAns.AsNoTracking().AnyAsync(da => da.Id == targetDuAnId.Value && (da.CreatedByUserId == userId || da.ChuDuAnId == userId));
                    if (isOwner) return true;
                }

                if (isReadOnlyCheck && hopDong.GoiThauId.HasValue)
                {
                    var isRelatedToHopDong = await _dbContext.CongViecNguoiLienQuans.AsNoTracking()
                        .AnyAsync(n => n.UserId == userId && n.CongViecGoiThau != null && n.CongViecGoiThau.GoiThauId == hopDong.GoiThauId.Value);
                    if (isRelatedToHopDong) return true;
                }
            }

            // 4. Check CongViecGoiThau: Access granted if Project Owner OR directly tagged in THIS task (allows view, status update, confirm)
            var congViec = await _dbContext.CongViecGoiThaus.AsNoTracking().FirstOrDefaultAsync(cv => cv.Id == entityId);
            if (congViec != null)
            {
                var isDirectRelated = await _dbContext.CongViecNguoiLienQuans.AsNoTracking()
                    .AnyAsync(n => n.UserId == userId && n.CongViecGoiThauId == entityId);
                if (isDirectRelated) return true;

                var parentGoiThau = await _dbContext.GoiThaus.AsNoTracking().FirstOrDefaultAsync(gt => gt.Id == congViec.GoiThauId);
                if (parentGoiThau != null && parentGoiThau.DuAnId.HasValue)
                {
                    var isOwner = await _dbContext.DuAns.AsNoTracking().AnyAsync(da => da.Id == parentGoiThau.DuAnId.Value && (da.CreatedByUserId == userId || da.ChuDuAnId == userId));
                    if (isOwner) return true;
                }
            }

            // 5. Check CommentCongViecGoiThau: Access granted if Project Owner OR tagged in the parent task
            var comment = await _dbContext.CommentCongViecGoiThaus.AsNoTracking().FirstOrDefaultAsync(c => c.Id == entityId);
            if (comment != null)
            {
                var isRelatedToComment = await _dbContext.CongViecNguoiLienQuans.AsNoTracking()
                    .AnyAsync(n => n.UserId == userId && n.CongViecGoiThauId == comment.CongViecGoiThauId);
                if (isRelatedToComment) return true;

                var parentCongViec = await _dbContext.CongViecGoiThaus.AsNoTracking().FirstOrDefaultAsync(cv => cv.Id == comment.CongViecGoiThauId);
                if (parentCongViec != null)
                {
                    var parentGoiThau = await _dbContext.GoiThaus.AsNoTracking().FirstOrDefaultAsync(gt => gt.Id == parentCongViec.GoiThauId);
                    if (parentGoiThau != null && parentGoiThau.DuAnId.HasValue)
                    {
                        var isOwner = await _dbContext.DuAns.AsNoTracking().AnyAsync(da => da.Id == parentGoiThau.DuAnId.Value && (da.CreatedByUserId == userId || da.ChuDuAnId == userId));
                        if (isOwner) return true;
                    }
                }
            }

            // 6. Check License: Access granted if Project Owner
            var license = await _dbContext.Licenses.AsNoTracking().FirstOrDefaultAsync(l => l.Id == entityId);
            if (license != null)
            {
                var isOwner = await _dbContext.DuAns.AsNoTracking().AnyAsync(da => da.Id == license.DuAnId && (da.CreatedByUserId == userId || da.ChuDuAnId == userId));
                if (isOwner) return true;
            }

            return false;
        }
    }
}
