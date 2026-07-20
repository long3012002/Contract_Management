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

            // All active users can access/view all features (GET requests) and create new items (POST)
            if (httpMethod == "GET" || httpMethod == "POST")
            {
                return;
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
    }
}
