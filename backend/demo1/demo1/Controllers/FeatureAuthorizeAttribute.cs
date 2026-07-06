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
            
            // Query to see if any active role assigned to the user has the required permission for the active feature
            var permissions = await _dbContext.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == dbUser.Id && ur.Role != null && ur.Role.IsActive)
                .Join(_dbContext.RolePermissions.Include(rp => rp.Feature),
                    ur => ur.RoleId,
                    rp => rp.RoleId,
                    (ur, rp) => rp)
                .Where(rp => rp.Feature != null && rp.Feature.Code == _featureCode && rp.Feature.IsActive)
                .ToListAsync();

            bool isAuthorized = false;

            if (httpMethod == "GET")
            {
                isAuthorized = permissions.Any(rp => rp.CanAccess);
            }
            else if (httpMethod == "POST")
            {
                isAuthorized = permissions.Any(rp => rp.CanCreate);
            }
            else if (httpMethod == "PUT" || httpMethod == "PATCH")
            {
                isAuthorized = permissions.Any(rp => rp.CanUpdate);
            }
            else if (httpMethod == "DELETE")
            {
                isAuthorized = permissions.Any(rp => rp.CanDelete);
            }

            if (!isAuthorized)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
