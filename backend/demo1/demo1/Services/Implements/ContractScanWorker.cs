using System;
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
using demo1.Services.Interfaces;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using demo1.Hubs;

namespace demo1.Services.Implements
{
    public class ContractScanWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ContractScanWorker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ContractScanWorker(
            IServiceProvider serviceProvider,
            ILogger<ContractScanWorker> logger,
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
            _logger.LogInformation("ContractScanWorker started.");

            // Run initial scan immediately upon startup
            try
            {
                await ScanAndNotifyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during initial contract expiration scan on startup.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1); // Next midnight
                var delay = nextRun - now;
                if (delay.TotalMilliseconds <= 0)
                {
                    delay = TimeSpan.FromHours(24);
                }

                _logger.LogInformation("Next contract scan scheduled at {NextRun} (in {DelayHours:F2} hours).", nextRun, delay.TotalHours);

                try
                {
                    var testIntervalMinutes = _configuration.GetValue<int?>("ContractScan:TestIntervalMinutes");
                    if (testIntervalMinutes.HasValue && testIntervalMinutes.Value > 0)
                    {
                        _logger.LogInformation("Testing mode enabled: running contract scan every {Minutes} minutes.", testIntervalMinutes.Value);
                        await Task.Delay(TimeSpan.FromMinutes(testIntervalMinutes.Value), stoppingToken);
                        await ScanAndNotifyAsync();
                        continue;
                    }
                    
                    await Task.Delay(delay, stoppingToken);
                    await ScanAndNotifyAsync();
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("ContractScanWorker is stopping.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during contract expiration scan.");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task ScanAndNotifyAsync()
        {
            _logger.LogInformation("Starting contract expiration scan...");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var today = DateTime.Today;

            // Fetch active contracts
            var expiringContracts = await dbContext.HopDongs
                .Where(h => h.IsActive && h.ExpiredDate.HasValue)
                .ToListAsync();

            // Filter contracts expiring in <= 30 days OR already expired (< 0)
            var contractsToWarn = expiringContracts
                .Where(h =>
                {
                    var daysRemaining = (h.ExpiredDate!.Value.Date - today).Days;
                    return daysRemaining <= 30;
                })
                .ToList();

            var activeUsers = await dbContext.Users
                .Where(u => u.IsActive)
                .ToListAsync();

            if (!activeUsers.Any())
            {
                _logger.LogWarning("No active users found to receive notifications.");
                return;
            }

            var notificationsToPush = new List<(string Username, Notification Notification)>();

            if (contractsToWarn.Any())
            {
                _logger.LogInformation("Found {Count} contracts expiring soon or already expired.", contractsToWarn.Count);

                foreach (var contract in contractsToWarn)
                {
                    var daysRemaining = (contract.ExpiredDate!.Value.Date - today).Days;
                    _logger.LogInformation("[ContractScan] Phát hiện hợp đồng: Mã={Code}, Tên={Name}, Hạn dùng={ExpiredDate:dd/MM/yyyy}, Số ngày còn lại={DaysRemaining}", contract.Code, contract.Name, contract.ExpiredDate.Value, daysRemaining);
                    
                    string title;
                    string content;

                    if (daysRemaining < 0)
                    {
                        var daysOverdue = Math.Abs(daysRemaining);
                        title = "Hợp đồng: Đã hết hạn";
                        content = $"Hợp đồng '{contract.Name}' (Mã: {contract.Code}) đã hết hạn {daysOverdue} ngày (ngày hết hạn: {contract.ExpiredDate.Value:dd/MM/yyyy}).";
                    }
                    else if (daysRemaining == 0)
                    {
                        title = "Hợp đồng: Hết hạn hôm nay";
                        content = $"Hợp đồng '{contract.Name}' (Mã: {contract.Code}) hết hạn hôm nay ({contract.ExpiredDate.Value:dd/MM/yyyy}).";
                    }
                    else
                    {
                        title = "Hợp đồng: Sắp hết hạn";
                        content = $"Hợp đồng '{contract.Name}' (Mã: {contract.Code}) sắp hết hạn (còn {daysRemaining} ngày, hạn: {contract.ExpiredDate.Value:dd/MM/yyyy}).";
                    }

                    var link = $"/contracts/{contract.Id}";

                    foreach (var user in activeUsers)
                    {
                        var alreadyNotified = await dbContext.Notifications
                            .AnyAsync(n => n.UserId == user.Id && n.Link == link && n.Title == title);

                        if (alreadyNotified)
                        {
                            continue;
                        }

                        _logger.LogInformation("[ContractScan] Đang tạo thông báo hệ thống cho user {Username} về hợp đồng {Code}", user.Username, contract.Code);

                        var notification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            Title = title,
                            Content = content,
                            Link = link,
                            UserId = user.Id,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.Notifications.Add(notification);
                        notificationsToPush.Add((user.Username, notification));
                    }
                }
            }

            // Scan Licenses expiring soon or already expired based on custom threshold (CanhBaoTruocNgay)
            var activeLicenses = await dbContext.Licenses
                .Where(l => l.IsActive && l.LoaiLicense != 2 && l.NgayKetThuc.HasValue)
                .ToListAsync();

            var licensesToWarn = activeLicenses
                .Where(l =>
                {
                    var daysRemaining = (l.NgayKetThuc!.Value.Date - today).Days;
                    return daysRemaining <= l.CanhBaoTruocNgay;
                })
                .ToList();

            if (licensesToWarn.Any())
            {
                _logger.LogInformation("Found {Count} licenses expiring or already expired.", licensesToWarn.Count);
                foreach (var license in licensesToWarn)
                {
                    var daysRemaining = (license.NgayKetThuc!.Value.Date - today).Days;
                    _logger.LogInformation("[LicenseScan] Phát hiện License: Mã={Code}, Tên={Name}, Hạn dùng={ExpiredDate:dd/MM/yyyy}, Số ngày còn lại={DaysRemaining}", license.Code, license.Name, license.NgayKetThuc.Value, daysRemaining);

                    string title;
                    string content;

                    if (daysRemaining < 0)
                    {
                        var daysOverdue = Math.Abs(daysRemaining);
                        title = "License: Đã hết hạn";
                        content = $"License '{license.Name}' (Mã: {license.Code}) đã hết hạn {daysOverdue} ngày (ngày hết hạn: {license.NgayKetThuc.Value:dd/MM/yyyy}).";
                    }
                    else if (daysRemaining == 0)
                    {
                        title = "License: Hết hạn hôm nay";
                        content = $"License '{license.Name}' (Mã: {license.Code}) hết hạn hôm nay ({license.NgayKetThuc.Value:dd/MM/yyyy}).";
                    }
                    else
                    {
                        title = "License: Sắp hết hạn";
                        content = $"License '{license.Name}' (Mã: {license.Code}) sắp hết hạn (còn {daysRemaining} ngày).";
                    }

                    var link = $"/licenses/{license.Id}";

                    foreach (var user in activeUsers)
                    {
                        var alreadyNotified = await dbContext.Notifications
                            .AnyAsync(n => n.UserId == user.Id && n.Link == link && n.Title == title);

                        if (alreadyNotified) continue;

                        var notification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            Title = title,
                            Content = content,
                            Link = link,
                            UserId = user.Id,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.Notifications.Add(notification);
                        notificationsToPush.Add((user.Username, notification));
                    }
                }
            }

            if (notificationsToPush.Any())
            {
                await dbContext.SaveChangesAsync();

                foreach (var item in notificationsToPush)
                {
                    _logger.LogInformation("[ContractScan] Đang push realtime thông báo cho user {Username} qua SignalR", item.Username);
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

            _logger.LogInformation("Finished contract & license expiration scan. Generated {Count} notifications.", notificationsToPush.Count);
        }
    }
}
