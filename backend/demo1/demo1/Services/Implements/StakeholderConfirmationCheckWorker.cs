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

namespace demo1.Services.Implements
{
    public class StakeholderConfirmationCheckWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StakeholderConfirmationCheckWorker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<NotificationHub> _hubContext;

        public StakeholderConfirmationCheckWorker(
            IServiceProvider serviceProvider,
            ILogger<StakeholderConfirmationCheckWorker> logger,
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
            _logger.LogInformation("StakeholderConfirmationCheckWorker started.");

            // Run initial check immediately on startup
            try
            {
                await CheckAndNotifyStakeholdersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during initial stakeholder check on startup.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1); // Next 0h midnight
                var delay = nextRun - now;
                if (delay.TotalMilliseconds <= 0)
                {
                    delay = TimeSpan.FromHours(24);
                }

                _logger.LogInformation("Next stakeholder confirmation check scheduled at {NextRun} (in {DelayHours:F2} hours).", nextRun, delay.TotalHours);

                try
                {
                    var testIntervalMinutes = _configuration.GetValue<int?>("StakeholderCheck:TestIntervalMinutes");
                    if (testIntervalMinutes.HasValue && testIntervalMinutes.Value > 0)
                    {
                        _logger.LogInformation("Testing mode enabled for StakeholderConfirmationCheckWorker: running every {Minutes} minutes.", testIntervalMinutes.Value);
                        await CheckAndNotifyStakeholdersAsync();
                        await Task.Delay(TimeSpan.FromMinutes(testIntervalMinutes.Value), stoppingToken);
                        continue;
                    }

                    await Task.Delay(delay, stoppingToken);
                    await CheckAndNotifyStakeholdersAsync();
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("StakeholderConfirmationCheckWorker is stopping.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during stakeholder confirmation check.");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task CheckAndNotifyStakeholdersAsync()
        {
            _logger.LogInformation("Starting stakeholder confirmation expiration & reminder check...");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;

            // 1. Fetch pending stakeholder records where 24h deadline (HanXacNhanAt) has passed
            var expiredRecords = await dbContext.CongViecNguoiLienQuans
                .Include(nlq => nlq.CongViecGoiThau)
                .Include(nlq => nlq.User)
                .Where(nlq => nlq.TrangThaiXacNhan == "Pending" && nlq.HanXacNhanAt <= now)
                .ToListAsync();

            var notificationsToPush = new List<(string TargetUsername, Notification NotificationPayload)>();

            if (expiredRecords.Any())
            {
                var expiredUserIds = expiredRecords.Select(r => r.UserId).Distinct().ToList();
                var expiredTaskLinks = expiredRecords.Select(r => $"/goi-thau/cong-viec/{r.CongViecGoiThauId}").Distinct().ToList();

                var existingExpiredNotifs = await dbContext.Notifications
                    .Where(n => n.UserId.HasValue && expiredUserIds.Contains(n.UserId.Value) && n.Link != null && expiredTaskLinks.Contains(n.Link) && n.Title.Contains("Quá hạn"))
                    .Select(n => new { n.UserId, n.Link })
                    .ToListAsync();

                var existingExpiredNotifsSet = new HashSet<(Guid UserId, string Link)>(
                    existingExpiredNotifs.Select(n => (n.UserId!.Value, n.Link ?? ""))
                );

                var expiredTaskIds = expiredRecords.Select(r => r.CongViecGoiThauId).Distinct().ToList();
                var tasksDict = new Dictionary<Guid, CongViecGoiThau>();
                if (expiredTaskIds.Any())
                {
                    var tasksList = await dbContext.CongViecGoiThaus
                        .Include(t => t.CreateUser)
                        .Include(t => t.ModifiedUser)
                        .Where(t => expiredTaskIds.Contains(t.Id))
                        .ToListAsync();
                    tasksDict = tasksList.ToDictionary(t => t.Id, t => t);
                }

                foreach (var record in expiredRecords)
                {
                    record.TrangThaiXacNhan = "Overdue";
                    record.UpdatedAt = now;

                    var taskTitle = record.CongViecGoiThau?.TenTaiLieu ?? "Công việc gói thầu";
                    var link = $"/goi-thau/cong-viec/{record.CongViecGoiThauId}";

                    var alreadyNotified = existingExpiredNotifsSet.Contains((record.UserId, link));

                    if (!alreadyNotified && record.User != null)
                    {
                        var notification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            Title = "Cảnh báo: Quá hạn công việc",
                            Content = $"Bạn đã quá hạn xác nhận công việc '{taskTitle}'.",
                            Link = link,
                            UserId = record.UserId,
                            IsRead = false,
                            CreatedAt = now
                        };

                        dbContext.Notifications.Add(notification);
                        notificationsToPush.Add((record.User.Username, notification));
                    }

                    // Notify CreateUser and ModifiedUser
                    if (tasksDict.TryGetValue(record.CongViecGoiThauId, out var task) && record.User != null)
                    {
                        var usersToNotify = new List<User>();
                        if (task.CreateUser != null)
                        {
                            usersToNotify.Add(task.CreateUser);
                        }
                        if (task.ModifiedUser != null && (task.CreateUserId == null || task.ModifiedUserId != task.CreateUserId))
                        {
                            usersToNotify.Add(task.ModifiedUser);
                        }

                        foreach (var targetUser in usersToNotify)
                        {
                            var overdueNotification = new Notification
                            {
                                Id = Guid.NewGuid(),
                                Title = "Quá hạn: Người liên quan",
                                Content = $"Thành viên {record.User.FullName ?? record.User.Username} đã quá hạn xác nhận '{taskTitle}'.",
                                Link = link,
                                UserId = targetUser.Id,
                                IsRead = false,
                                CreatedAt = now
                            };
                            dbContext.Notifications.Add(overdueNotification);
                            notificationsToPush.Add((targetUser.Username, overdueNotification));
                        }
                    }
                }
            }

            // 2. Fetch pending stakeholder records approaching deadline (<= 6 hours remaining)
            var warningThresholdTime = now.AddHours(6);
            var upcomingRecords = await dbContext.CongViecNguoiLienQuans
                .Include(nlq => nlq.CongViecGoiThau)
                .Include(nlq => nlq.User)
                .Where(nlq => nlq.TrangThaiXacNhan == "Pending" && nlq.HanXacNhanAt > now && nlq.HanXacNhanAt <= warningThresholdTime)
                .ToListAsync();

            if (upcomingRecords.Any())
            {
                var upcomingUserIds = upcomingRecords.Select(r => r.UserId).Distinct().ToList();
                var upcomingTaskLinks = upcomingRecords.Select(r => $"/goi-thau/cong-viec/{r.CongViecGoiThauId}").Distinct().ToList();

                var existingUpcomingNotifs = await dbContext.Notifications
                    .Where(n => n.UserId.HasValue && upcomingUserIds.Contains(n.UserId.Value) && n.Link != null && upcomingTaskLinks.Contains(n.Link) && n.Title.Contains("Sắp hết hạn"))
                    .Select(n => new { n.UserId, n.Link })
                    .ToListAsync();

                var existingUpcomingNotifsSet = new HashSet<(Guid UserId, string Link)>(
                    existingUpcomingNotifs.Select(n => (n.UserId!.Value, n.Link ?? ""))
                );

                foreach (var record in upcomingRecords)
                {
                    var taskTitle = record.CongViecGoiThau?.TenTaiLieu ?? "Công việc gói thầu";
                    var link = $"/goi-thau/cong-viec/{record.CongViecGoiThauId}";

                    var hoursLeft = Math.Max(1, (int)Math.Round((record.HanXacNhanAt - now).TotalHours));
                    var alreadyNotified = existingUpcomingNotifsSet.Contains((record.UserId, link));

                    if (!alreadyNotified && record.User != null)
                    {
                        var notification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            Title = "Nhắc nhở: Sắp hết hạn",
                            Content = $"Công việc '{taskTitle}' sắp hết hạn (còn {hoursLeft} giờ).",
                            Link = link,
                            UserId = record.UserId,
                            IsRead = false,
                            CreatedAt = now
                        };

                        dbContext.Notifications.Add(notification);
                        notificationsToPush.Add((record.User.Username, notification));
                    }
                }
            }

            if (expiredRecords.Any() || notificationsToPush.Any())
            {
                await dbContext.SaveChangesAsync();
            }

            // Push SignalR realtime notifications
            foreach (var item in notificationsToPush)
            {
                _logger.LogInformation("[StakeholderCheck] Pushing realtime notification to user {Username}", item.TargetUsername);
                await _hubContext.Clients.User(item.TargetUsername).SendAsync("ReceiveNotification", new
                {
                    id = item.NotificationPayload.Id,
                    title = item.NotificationPayload.Title,
                    content = item.NotificationPayload.Content,
                    link = item.NotificationPayload.Link,
                    isRead = item.NotificationPayload.IsRead,
                    createdAt = item.NotificationPayload.CreatedAt
                });
            }

            _logger.LogInformation("Finished stakeholder check. Processed {ExpiredCount} expired records, sent {NotifCount} notifications.",
                expiredRecords.Count, notificationsToPush.Count);
        }
    }
}
