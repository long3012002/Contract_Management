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
using demo1.DTOs;

namespace demo1.Controllers
{
    /// <summary>
    /// API Thông báo Cá nhân (Xem danh sách thông báo, Đánh dấu đã đọc một hoặc tất cả thông báo).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/HeThong/notification")]
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

        /// <summary>
        /// Lấy danh sách thông báo của người dùng đăng nhập hiện tại (Có bộ lọc phân trang, tìm kiếm, lọc chưa đọc/đã đọc).
        /// </summary>
        /// <param name="filter">Bộ lọc danh sách thông báo</param>
        /// <returns>Danh sách thông báo phân trang</returns>
        /// <response code="200">Lấy thông báo thành công</response>
        /// <response code="401">Chưa xác thực hoặc tài khoản bị khóa</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PagedResult<NotificationDto>>> GetNotifications([FromQuery] NotificationFilterDto filter)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized(new { Message = "Người dùng không hợp lệ hoặc tài khoản đã bị khóa." });
            }

            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 100);

            var query = _dbContext.Notifications.AsNoTracking()
                .Where(n => n.UserId == user.Id);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var keyword = filter.Search.Trim();
                query = query.Where(n => 
                    EF.Functions.Like(n.Title, $"%{keyword}%") || 
                    EF.Functions.Like(n.Content, $"%{keyword}%"));
            }

            if (filter.IsRead.HasValue)
            {
                query = query.Where(n => n.IsRead == filter.IsRead.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(n => n.CreatedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(n => n.CreatedAt <= filter.ToDate.Value);
            }

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Content,
                    Link = n.Link,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            var result = new PagedResult<NotificationDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return Ok(result);
        }

        /// <summary>
        /// Đánh dấu một thông báo cụ thể theo ID là đã đọc.
        /// </summary>
        /// <param name="id">Mã định danh Thông báo (GUID)</param>
        /// <response code="200">Đánh dấu thành công</response>
        /// <response code="404">Không tìm thấy thông báo</response>
        [HttpPut("{id:guid}/read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Đánh dấu tất cả thông báo của người dùng hiện tại là đã đọc.
        /// </summary>
        /// <response code="200">Đánh dấu toàn bộ thông báo là đã đọc thành công</response>
        [HttpPut("read-all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
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
