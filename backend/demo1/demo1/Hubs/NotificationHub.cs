using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace demo1.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var username = Context.User?.Identity?.Name;
            _logger.LogInformation("SignalR Client connected: Username={Username}, ConnectionId={ConnectionId}", 
                username, Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.User?.Identity?.Name;
            _logger.LogInformation("SignalR Client disconnected: Username={Username}, ConnectionId={ConnectionId}, Error={Error}", 
                username, Context.ConnectionId, exception?.Message ?? "None");

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinCongViecGroup(string idCongViec)
        {
            var groupName = $"CongViec_{idCongViec}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);
        }

        public async Task LeaveCongViecGroup(string idCongViec)
        {
            var groupName = $"CongViec_{idCongViec}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);
        }
    }
}

