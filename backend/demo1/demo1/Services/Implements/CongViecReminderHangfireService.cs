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

namespace demo1.Services.Implements
{
    public class CongViecReminderHangfireService
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<CongViecReminderHangfireService> _logger;

        public CongViecReminderHangfireService(
            AppDbContext db,
            IHubContext<NotificationHub> hubContext,
            ILogger<CongViecReminderHangfireService> logger)
        {
            _db = db;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Tạo 4 Scheduled Jobs (6h, 12h, 18h nhắc nhở, 24h báo quá hạn) cho người liên quan công việc
        /// </summary>
        public async Task ScheduleRemindersForStakeholderAsync(CongViecNguoiLienQuan record, CongViecGoiThau task, User targetUser)
        {
            var jobIds = new List<string>();

            // Lần 1: Nhắc nhở sau 6 giờ
            string id1 = BackgroundJob.Schedule<CongViecReminderHangfireService>(
                x => x.SendReminderAsync(record.Id, "Bạn có công việc cần xác nhận (Đã qua 6h)"),
                TimeSpan.FromHours(6)
            );
            jobIds.Add(id1);

            // Lần 2: Nhắc nhở sau 12 giờ
            string id2 = BackgroundJob.Schedule<CongViecReminderHangfireService>(
                x => x.SendReminderAsync(record.Id, "Cảnh báo: Công việc chờ xác nhận đã qua 12h"),
                TimeSpan.FromHours(12)
            );
            jobIds.Add(id2);

            // Lần 3: Nhắc nhở gấp sau 18 giờ
            string id3 = BackgroundJob.Schedule<CongViecReminderHangfireService>(
                x => x.SendReminderAsync(record.Id, "GẤP: Chỉ còn 6h để xác nhận công việc này!"),
                TimeSpan.FromHours(18)
            );
            jobIds.Add(id3);

            // Lần 4: Xử lý QUÁ HẠN sau 24 giờ
            string id4 = BackgroundJob.Schedule<CongViecReminderHangfireService>(
                x => x.HandleTimeoutAsync(record.Id),
                TimeSpan.FromHours(24)
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

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Cảnh báo: Quá hạn xác nhận công việc",
                    Content = $"Công việc '{taskTitle}' đã quá 24h và chưa được xác nhận/bình luận. Trạng thái đã chuyển sang 'Quá hạn'.",
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

                _logger.LogInformation("Processed timeout (24h) for record {RecordId} of user {Username}", recordId, record.User.Username);
            }
        }
    }
}
