using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace demo1.Providers
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.Identity?.Name ?? connection.User?.FindFirst(ClaimTypes.Name)?.Value;
        }
    }
}
