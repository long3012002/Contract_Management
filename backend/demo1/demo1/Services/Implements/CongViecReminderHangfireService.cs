using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.Entity;
using demo1.Hubs;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Configuration;

namespace demo1.Services.Implements
{
    public class CongViecReminderHangfireService
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<CongViecReminderHangfireService> _logger;
        private readonly IConfiguration _configuration;

        public CongViecReminderHangfireService(
            AppDbContext db,
            IHubContext<NotificationHub> hubContext,
            ILogger<CongViecReminderHangfireService> logger,
            IConfiguration configuration)
        {
            _db = db;
            _hubContext = hubContext;
            _logger = logger;
            _configuration = configuration;
        }

        private (double total, bool isMinutes) GetDeadlineConfig()
        {
            var minutes = _configuration.GetValue<int?>("StakeholderCheck:HanXacNhanMinutes");
            if (minutes.HasValue && minutes.Value > 0)
            {
                return (minutes.Value, true);
            }
            var hours = _configuration.GetValue<int?>("StakeholderCheck:HanXacNhanHours") ?? 24;
            return (hours, false);
        }

        private string FormatDuration(double value, bool isMinutes)
        {
            return isMinutes ? $"{value} phút" : $"{value} giờ";
        }

        /// <summary>
        /// Tạo 4 Scheduled Jobs nhắc nhở và báo quá hạn cho người liên quan công việc dựa trên cấu hình
        /// </summary>
        public async Task ScheduleRemindersForStakeholderAsync(CongViecNguoiLienQuan record, CongViecGoiThau task, User targetUser)
        {
            var jobIds = new List<string>();
            var (deadline, isMinutes) = GetDeadlineConfig();

            var t1 = deadline * 0.25;
            var t2 = deadline * 0.50;
            var t3 = deadline * 0.75;
            var t4 = deadline;

            var time1 = isMinutes ? TimeSpan.FromMinutes(t1) : TimeSpan.FromHours(t1);
            var time2 = isMinutes ? TimeSpan.FromMinutes(t2) : TimeSpan.FromHours(t2);
            var time3 = isMinutes ? TimeSpan.FromMinutes(t3) : TimeSpan.FromHours(t3);
            var time4 = isMinutes ? TimeSpan.FromMinutes(t4) : TimeSpan.FromHours(t4);

            // Lần 1: Nhắc nhở sau 25% thời gian
            string id1 = BackgroundJob.Schedule<CongViecReminderHangfireService>(
                x => x.SendReminderAsync(record.Id, $"Bạn có công việc cần xác nhận (Đã qua {FormatDuration(t1, isMinutes)})"),
                time1
            );
            jobIds.Add(id1);

            // Lần 2: Nhắc nhở sau 50% thời gian
            string id2 = BackgroundJob.Schedule<CongViecReminderHangfireService>(
                x => x.SendReminderAsync(record.Id, $"Cảnh báo: Công việc chờ xác nhận đã qua {FormatDuration(t2, isMinutes)}"),
                time2
            );
            jobIds.Add(id2);

            // Lần 3: Nhắc nhở gấp sau 75% thời gian (còn lại 25%)
            var remaining = t4 - t3;
            string id3 = BackgroundJob.Schedule<CongViecReminderHangfireService>(
                x => x.SendReminderAsync(record.Id, $"GẤP: Chỉ còn {FormatDuration(remaining, isMinutes)} để xác nhận công việc này!"),
                time3
            );
            jobIds.Add(id3);

            // Lần 4: Xử lý QUÁ HẠN sau 100% thời gian
            string id4 = BackgroundJob.Schedule<CongViecReminderHangfireService>(
                x => x.HandleTimeoutAsync(record.Id),
                time4
            );
            jobIds.Add(id4);

            record.ReminderJobIds = string.Join(",", jobIds);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Hủy tất cả các Reminder Job chưa chạy của một CongViecNguoiLienQuan khi người dùng đã xác nhận / bình luận
        /// </summary>
        public void CancelReminders(CongViecNguoiLienQuan record)
        {
            if (string.IsNullOrWhiteSpace(record.ReminderJobIds)) return;

            var jobIds = record.ReminderJobIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var jobId in jobIds)
            {
                try
                {
                    BackgroundJob.Delete(jobId.Trim());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi xóa Hangfire Job {JobId}", jobId);
                }
            }
            record.ReminderJobIds = null;
        }

        /// <summary>
        /// Hàm gửi nhắc nhở (Mốc 6h, 12h, 18h) với Double Check chống Race-Condition
        /// </summary>
        public async Task SendReminderAsync(Guid recordId, string customMessage)
        {
            var record = await _db.CongViecNguoiLienQuans
                .Include(n => n.CongViecGoiThau)
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.Id == recordId);

            // Double Check: Chỉ gửi khi trạng thái vẫn đang là Pending
            if (record != null && record.TrangThaiXacNhan == "Pending" && record.User != null)
            {
                var taskTitle = record.CongViecGoiThau?.TenTaiLieu ?? "Công việc gói thầu";
                var link = $"/goi-thau/cong-viec/{record.CongViecGoiThauId}";

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Nhắc nhở xác nhận công việc",
                    Content = $"{customMessage} trong công việc '{taskTitle}'. Vui lòng xác nhận hoặc bình luận.",
                    Link = link,
                    UserId = record.UserId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Notifications.Add(notification);
                await _db.SaveChangesAsync();

                await _hubContext.Clients.User(record.User.Username).SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    title = notification.Title,
                    content = notification.Content,
                    link = notification.Link,
                    isRead = notification.IsRead,
                    createdAt = notification.CreatedAt
                });

                _logger.LogInformation("Sent reminder job for record {RecordId} to user {Username}", recordId, record.User.Username);
            }
        }

        /// <summary>
        /// Hàm xử lý QUÁ HẠN khi chạm mốc 24h với Double Check chống Race-Condition
        /// </summary>
        public async Task HandleTimeoutAsync(Guid recordId)
        {
            var record = await _db.CongViecNguoiLienQuans
                .Include(n => n.CongViecGoiThau)
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.Id == recordId);

            // Double Check: Chỉ chuyển trạng thái Overdue khi vẫn là Pending
            if (record != null && record.TrangThaiXacNhan == "Pending" && record.User != null)
            {
                record.TrangThaiXacNhan = "Overdue";
                record.ReminderJobIds = null;
                record.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                var taskTitle = record.CongViecGoiThau?.TenTaiLieu ?? "Công việc gói thầu";
                var link = $"/goi-thau/cong-viec/{record.CongViecGoiThauId}";

                var (deadline, isMinutes) = GetDeadlineConfig();
                var formattedDeadline = FormatDuration(deadline, isMinutes);

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Cảnh báo: Quá hạn xác nhận công việc",
                    Content = $"Công việc '{taskTitle}' đã quá {formattedDeadline} và chưa được xác nhận/bình luận. Trạng thái đã chuyển sang 'Quá hạn'.",
                    Link = link,
                    UserId = record.UserId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Notifications.Add(notification);
                await _hubContext.Clients.User(record.User.Username).SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    title = notification.Title,
                    content = notification.Content,
                    link = notification.Link,
                    isRead = notification.IsRead,
                    createdAt = notification.CreatedAt
                });

                // Notify CreateUser and ModifiedUser
                var task = await _db.CongViecGoiThaus
                    .Include(t => t.CreateUser)
                    .Include(t => t.ModifiedUser)
                    .FirstOrDefaultAsync(t => t.Id == record.CongViecGoiThauId);

                if (task != null)
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
                            Title = "Người liên quan quá hạn xác nhận công việc",
                            Content = $"Người liên quan {record.User.FullName ?? record.User.Username} đã quá hạn xác nhận công việc '{taskTitle}'.",
                            Link = link,
                            UserId = targetUser.Id,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.Notifications.Add(overdueNotification);
                        await _hubContext.Clients.User(targetUser.Username).SendAsync("ReceiveNotification", new
                        {
                            id = overdueNotification.Id,
                            title = overdueNotification.Title,
                            content = overdueNotification.Content,
                            link = overdueNotification.Link,
                            isRead = overdueNotification.IsRead,
                            createdAt = overdueNotification.CreatedAt
                        });
                    }
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Processed timeout ({Deadline}) for record {RecordId} of user {Username}", formattedDeadline, recordId, record.User.Username);
            }
        }
    }
}
