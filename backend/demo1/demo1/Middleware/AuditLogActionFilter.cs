using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using demo1.Data;

namespace demo1.Middleware
{
    public class AuditLogActionFilter : IAsyncActionFilter
    {
        private readonly AppDbContext _dbContext;

        public AuditLogActionFilter(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Execute the action (controller endpoint method)
            var executedContext = await next();

            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var username = user.Identity.Name;
                if (!string.IsNullOrEmpty(username))
                {
                    try
                    {
                        var controllerName = context.RouteData.Values["controller"]?.ToString();
                        
                        // Skip auditing Auth and Health endpoints as they don't require access logs or are logged separately
                        if (controllerName == "Auth" || controllerName == "Health")
                        {
                            return;
                        }

                        string? ipAddress = GetClientIpAddress(context.HttpContext);

                        var httpMethod = context.HttpContext.Request.Method;
                        var path = context.HttpContext.Request.Path.Value;
                        var actionName = context.RouteData.Values["action"]?.ToString();

                        var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);

                        // Check if the result was a forbidden/unauthorized response
                        var isSuccess = executedContext.Exception == null && 
                                        (executedContext.Result == null || 
                                         !(executedContext.Result is ForbidResult || 
                                           executedContext.Result is ChallengeResult || 
                                           executedContext.Result is UnauthorizedResult || 
                                           (executedContext.Result is ObjectResult obj && obj.StatusCode == 403)));

                        var auditLog = new demo1.Entity.AuditLog
                        {
                            UserId = dbUser?.Id.ToString(),
                            Username = username,
                            Action = isSuccess ? "ACCESS_GRANTED" : "ACCESS_DENIED",
                            TableName = controllerName ?? "System",
                            EntityId = actionName ?? "Execute",
                            Timestamp = DateTime.UtcNow,
                            IpAddress = ipAddress,
                            NewValues = $"{httpMethod} {path}"
                        };

                        _dbContext.AuditLogs.Add(auditLog);
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AuditLogActionFilter Error]: {ex.Message}");
                    }
                }
            }
        }

        private string? GetClientIpAddress(Microsoft.AspNetCore.Http.HttpContext context)
        {
            // 1. Try X-Forwarded-For
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrEmpty(forwardedFor))
            {
                var ip = forwardedFor.ToString().Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(ip))
                {
                    return CleanIpAddress(ip);
                }
            }

            // 2. Try X-Real-IP
            if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp) && !string.IsNullOrEmpty(realIp))
            {
                return CleanIpAddress(realIp.ToString().Trim());
            }

            // 3. Fallback to Connection Remote IP
            var remoteIp = context.Connection?.RemoteIpAddress;
            if (remoteIp != null)
            {
                if (remoteIp.IsIPv4MappedToIPv6)
                {
                    return remoteIp.MapToIPv4().ToString();
                }
                return remoteIp.ToString();
            }

            return null;
        }

        private string CleanIpAddress(string ip)
        {
            if (ip.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
            {
                return ip.Substring(7);
            }
            return ip;
        }
    }
}
