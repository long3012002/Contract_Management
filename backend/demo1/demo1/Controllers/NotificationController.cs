using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using demo1.Data;
using demo1.Entity;
using demo1.DTOs.Common;

namespace demo1.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public NotificationController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Helper to get currently logged-in user
        private async Task<User?> GetCurrentUserAsync()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;

            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        }

        // 1. GET: api/notification
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized(new { Message = "Người dùng không hợp lệ hoặc tài khoản đã bị khóa." });
            }

            var notifications = await _dbContext.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Content,
                    n.Link,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // 2. PUT: api/notification/{id}/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized(new { Message = "Người dùng không hợp lệ hoặc tài khoản đã bị khóa." });
            }

            var notification = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

            if (notification == null)
            {
                return NotFound(new { Message = "Không tìm thấy thông báo." });
            }

            notification.IsRead = true;
            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Đã đánh dấu thông báo là đã đọc." });
        }

        // 3. PUT/POST: api/notification/read-all, api/notification/ConfirmAll, api/notification/confirm-all
        [HttpPut("read-all")]
        [HttpPost("read-all")]
        [HttpPut("confirm-all")]
        [HttpPost("confirm-all")]
        [HttpPut("ConfirmAll")]
        [HttpPost("ConfirmAll")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized(new { Message = "Người dùng không hợp lệ hoặc tài khoản đã bị khóa." });
            }

            var unreadNotifications = await _dbContext.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .ToListAsync();

            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Đã đánh dấu tất cả thông báo là đã đọc.", Count = unreadNotifications.Count });
        }
    }
}
