using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using demo1.Data;
using demo1.Entity;
using Microsoft.AspNetCore.SignalR;
using demo1.Hubs;

namespace demo1.Services.Workers
{
    public class DotThanhToanScanWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DotThanhToanScanWorker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<NotificationHub> _hubContext;

        public DotThanhToanScanWorker(
            IServiceProvider serviceProvider,
            ILogger<DotThanhToanScanWorker> logger,
            IConfiguration configuration,
            IHubContext<NotificationHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DotThanhToanScanWorker started.");

            // Run initial scan immediately upon startup
            try
            {
                await ScanAndNotifyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during initial payment tranche scan on startup.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1); // Next midnight (0h)
                var delay = nextRun - now;
                if (delay.TotalMilliseconds <= 0)
                {
                    delay = TimeSpan.FromHours(24);
                }

                _logger.LogInformation("Next payment phase scan scheduled at {NextRun} (in {DelayHours:F2} hours).", nextRun, delay.TotalHours);

                try
                {
                    var testIntervalMinutes = _configuration.GetValue<int?>("DotThanhToanScan:TestIntervalMinutes");
                    if (testIntervalMinutes.HasValue && testIntervalMinutes.Value > 0)
                    {
                        _logger.LogInformation("Testing mode enabled for DotThanhToanScanWorker: running every {Minutes} minutes.", testIntervalMinutes.Value);
                        await Task.Delay(TimeSpan.FromMinutes(testIntervalMinutes.Value), stoppingToken);
                        await ScanAndNotifyAsync();
                        continue;
                    }

                    await Task.Delay(delay, stoppingToken);
                    await ScanAndNotifyAsync();
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("DotThanhToanScanWorker is stopping.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during payment phase scan.");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task ScanAndNotifyAsync()
        {
            _logger.LogInformation("Starting payment tranche expiration scan...");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var today = DateTime.Today;

            var warnDaysBefore = _configuration.GetValue<int>("DotThanhToanScan:WarnDaysBefore", 30);
            var intervalDays = _configuration.GetValue<int>("DotThanhToanScan:NotificationIntervalDays", 1);

            // Fetch active payment phases where contract is active and is not paid yet
            var pendingPaymentPhases = await dbContext.DotThanhToans
                .Include(d => d.HopDong)
                    .ThenInclude(h => h.DuAn)
                .Include(d => d.HopDong)
                    .ThenInclude(h => h.GoiThau)
                        .ThenInclude(g => g!.DuAn)
                .Where(d => !d.IsPaid && d.NgayThanhToan.HasValue && d.HopDong != null && d.HopDong.IsActive)
                .ToListAsync();

            if (!pendingPaymentPhases.Any())
            {
                _logger.LogInformation("No pending payment tranches found.");
                return;
            }

            var notificationsToPush = new List<(string Username, Notification Notification)>();

            foreach (var phase in pendingPaymentPhases)
            {
                var daysRemaining = (phase.NgayThanhToan!.Value.Date - today).Days;

                // Thresholds:
                // 1. Overdue: daysRemaining < 0
                // 2. Upcoming due: 0 <= daysRemaining <= warnDaysBefore
                if (daysRemaining > warnDaysBefore)
                {
                    continue; // Not yet due for warning
                }

                string title;
                string content;
                var formattedDate = phase.NgayThanhToan.Value.ToString("dd/MM/yyyy");
                var formattedAmount = phase.GiaTriThanhToan.ToString("N0") + " VNĐ";
                var link = $"/contracts/{phase.HopDongId}";

                if (daysRemaining < 0)
                {
                    var daysOverdue = Math.Abs(daysRemaining);
                    title = "Đợt thanh toán: Quá hạn";
                    content = $"Đợt thanh toán '{phase.TenDot}' của Hợp đồng '{phase.HopDong.Name}' (Số tiền: {formattedAmount}) đã quá hạn {daysOverdue} ngày (Hạn thanh toán: {formattedDate}).";
                }
                else if (daysRemaining == 0)
                {
                    title = "Đợt thanh toán: Đến hạn hôm nay";
                    content = $"Đợt thanh toán '{phase.TenDot}' của Hợp đồng '{phase.HopDong.Name}' (Số tiền: {formattedAmount}) đến hạn thanh toán hôm nay ({formattedDate}).";
                }
                else
                {
                    title = "Đợt thanh toán: Sắp đến hạn";
                    content = $"Đợt thanh toán '{phase.TenDot}' của Hợp đồng '{phase.HopDong.Name}' (Số tiền: {formattedAmount}) sắp đến hạn thanh toán (còn {daysRemaining} ngày, hạn: {formattedDate}).";
                }

                // Determine target users (Creators, Modifiers, Viewers/Editors, Project Owner, System Admins)
                var targetUsers = await ContractScanWorker.GetTargetUsersForContractAsync(dbContext, phase.HopDong);

                foreach (var user in targetUsers)
                {
                    bool alreadyNotified;
                    if (intervalDays <= 0)
                    {
                        alreadyNotified = await dbContext.Notifications
                            .AnyAsync(n => n.UserId == user.Id && n.Link == link && n.Title == title);
                    }
                    else
                    {
                        var cutoffDate = DateTime.UtcNow.Date.AddDays(-(intervalDays - 1));
                        alreadyNotified = await dbContext.Notifications
                            .AnyAsync(n => n.UserId == user.Id && n.Link == link && n.Title == title && n.CreatedAt >= cutoffDate);
                    }

                    if (alreadyNotified)
                    {
                        continue;
                    }

                    _logger.LogInformation("[DotThanhToanScan] Tạo thông báo cho user {Username} về đợt thanh toán {TenDot} - Hợp đồng {ContractCode}", user.Username, phase.TenDot, phase.HopDong.Code);

                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        Title = title,
                        Content = content,
                        Link = link,
                        FeatureCode = "QUAN_LY_HOP_DONG",
                        EntityName = "DotThanhToan",
                        EntityId = phase.Id.ToString(),
                        UserId = user.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    dbContext.Notifications.Add(notification);
                    notificationsToPush.Add((user.Username, notification));
                }
            }

            if (notificationsToPush.Any())
            {
                await dbContext.SaveChangesAsync();

                foreach (var item in notificationsToPush)
                {
                    await _hubContext.Clients.User(item.Username).SendAsync("ReceiveNotification", new
                    {
                        id = item.Notification.Id,
                        title = item.Notification.Title,
                        content = item.Notification.Content,
                        link = item.Notification.Link,
                        isRead = item.Notification.IsRead,
                        createdAt = item.Notification.CreatedAt
                    });
                }
            }

            _logger.LogInformation("Finished payment tranche scan. Created {Count} notifications.", notificationsToPush.Count);
        }
    }
}
