using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using demo1.Services.Interfaces;

namespace demo1.Services.Implements
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetUsername()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Name)?.Value ?? user?.Identity?.Name;
        }

        public string? GetIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

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
