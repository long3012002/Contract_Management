using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using demo1.Data;

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

            // Retrieve user with roles and permissions from database
            var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            if (dbUser == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            // IsSystemAdmin has full access to everything
            if (dbUser.IsSystemAdmin)
            {
                return;
            }

            // Check specific feature authorization
            var httpMethod = context.HttpContext.Request.Method;

            // 1. Check department permission (PhongBan)
            bool isDeptAuthorized = false;
            if (dbUser.IdPhongBan.HasValue)
            {
                var deptPerm = await _dbContext.PhongBanPermissions
                    .Include(pbp => pbp.Feature)
                    .FirstOrDefaultAsync(pbp => pbp.PhongBanId == dbUser.IdPhongBan.Value && pbp.Feature != null && pbp.Feature.Code == _featureCode && pbp.Feature.IsActive);
                
                if (deptPerm != null)
                {
                    isDeptAuthorized = EvaluatePermission(deptPerm.CanAccess, deptPerm.Permissions, httpMethod);
                }
            }
            else
            {
                // If no department is set, default to authorized to avoid locking out unassigned users (or set to false if strict)
                isDeptAuthorized = true;
            }

            // 2. Check position permission (ChucVu)
            bool isPosAuthorized = false;
            if (dbUser.IdChucVu.HasValue)
            {
                var cvPerm = await _dbContext.ChucVuPermissions
                    .Include(cvp => cvp.Feature)
                    .FirstOrDefaultAsync(cvp => cvp.ChucVuId == dbUser.IdChucVu.Value && cvp.Feature != null && cvp.Feature.Code == _featureCode && cvp.Feature.IsActive);
                
                if (cvPerm != null)
                {
                    isPosAuthorized = EvaluatePermission(cvPerm.CanAccess, cvPerm.Permissions, httpMethod);
                }
            }
            else
            {
                isPosAuthorized = true;
            }

            if (!isDeptAuthorized || !isPosAuthorized)
            {
                context.Result = new ForbidResult();
            }
        }

        private bool EvaluatePermission(bool canAccess, string permissionsString, string httpMethod)
        {
            if (httpMethod == "GET")
            {
                return canAccess;
            }

            if (string.IsNullOrEmpty(permissionsString))
            {
                return false;
            }

            var allowedActions = permissionsString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().ToLower())
                .ToList();

            if (httpMethod == "POST")
            {
                return allowedActions.Contains("create");
            }
            if (httpMethod == "PUT" || httpMethod == "PATCH")
            {
                return allowedActions.Contains("update");
            }
            if (httpMethod == "DELETE")
            {
                return allowedActions.Contains("delete");
            }

            return false;
        }
    }
}
