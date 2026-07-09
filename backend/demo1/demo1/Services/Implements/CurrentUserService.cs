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

            // Check if behind proxy
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                return forwardedFor.FirstOrDefault();
            }

            return context.Connection?.RemoteIpAddress?.ToString();
        }
    }
}
