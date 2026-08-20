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

using demo1.Services.Implements;

namespace demo1.Controllers
{
    /// <summary>
    /// API Thông báo Cá nhân (Xem danh sách thông báo, Đánh dấu đã đọc một hoặc tất cả thông báo, Thống kê theo chức năng).
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
        /// Lấy danh sách thông báo của người dùng đăng nhập hiện tại (Có bộ lọc phân trang, tìm kiếm, lọc theo mã tính năng, lọc chưa đọc/đã đọc).
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

            // Bước 1: Lấy danh sách thông báo theo UserId
            var query = _dbContext.Notifications.AsNoTracking()
                .Where(n => n.UserId == user.Id);

            // Bước 2: Lọc theo từ khóa tìm kiếm (Title hoặc Content)
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var keyword = filter.Search.Trim();
                query = query.Where(n => 
                    EF.Functions.ILike(n.Title, $"%{keyword}%") || 
                    EF.Functions.ILike(n.Content, $"%{keyword}%"));
            }

            // Bước 3: Lọc theo Mã chức năng (FeatureCode) dùng mảng đơn giản để EF Core chuyển thành mệnh đề SQL IN(...)
            if (!string.IsNullOrWhiteSpace(filter.FeatureCode))
            {
                var raw = filter.FeatureCode.Trim();
                var norm = PermissionService.NormalizeFeatureCode(filter.FeatureCode);
                
                var featureCodes = new List<string> { raw, norm };
                if (norm == "DU_AN")
                {
                    featureCodes.Add("PROJECT");
                }
                else if (norm == "QUAN_LY_HOP_DONG")
                {
                    featureCodes.Add("CONTRACT");
                    featureCodes.Add("HOP_DONG");
                }
                else if (norm == "CONG_VIEC")
                {
                    featureCodes.Add("TASK");
                }

                var distinctCodes = featureCodes
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                query = query.Where(n => distinctCodes.Contains(n.FeatureCode));
            }

            // Bước 4: Lọc theo trạng thái đã đọc / chưa đọc
            if (filter.IsRead.HasValue)
            {
                query = query.Where(n => n.IsRead == filter.IsRead.Value);
            }

            // Bước 5: Lọc theo khoảng thời gian tạo
            if (filter.FromDate.HasValue)
            {
                query = query.Where(n => n.CreatedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(n => n.CreatedAt <= filter.ToDate.Value);
            }

            // Bước 6: Đếm tổng số bản ghi phù hợp
            var totalItems = await query.CountAsync();

            // Bước 7: Phân trang và lấy danh sách kết quả DTO
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
                    FeatureCode = n.FeatureCode,
                    EntityName = n.EntityName,
                    EntityId = n.EntityId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            foreach (var item in items)
            {
                item.Content = CleanNotificationContent(item.Content);
            }

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
        /// Loại bỏ tên chức năng ở đầu trường Content nếu có (ví dụ: "[Quản lý Hợp đồng] ..."), chỉ giữ lại thông điệp cho người dùng.
        /// </summary>
        public static string CleanNotificationContent(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            var cleaned = System.Text.RegularExpressions.Regex.Replace(content, @"^\[[^\]]+\]:?\s*", string.Empty);
            return cleaned.Trim();
        }

        /// <summary>
        /// Thống kê tổng số thông báo chưa đọc của người dùng hiện tại phân loại theo từng Chức năng (FeatureCode).
        /// </summary>
        /// <returns>Danh sách thống kê thông báo chưa đọc theo chức năng</returns>
        /// <response code="200">Lấy thống kê thông báo thành công</response>
        [HttpGet("summary-by-feature")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotificationSummaryByFeature()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized(new { Message = "Người dùng không hợp lệ hoặc tài khoản đã bị khóa." });
            }

            // Bước 1: Truy vấn các trường dữ liệu cần thiết từ CSDL đơn giản nhất có thể
            var rawNotifications = await _dbContext.Notifications.AsNoTracking()
                .Where(n => n.UserId == user.Id)
                .Select(n => new { FeatureCode = n.FeatureCode, IsRead = n.IsRead })
                .ToListAsync();

            // Bước 2: Gom nhóm và tính toán số lượng trên bộ nhớ (C# Memory) để tránh lỗi SQL GroupBy
            var summary = rawNotifications
                .GroupBy(n => string.IsNullOrWhiteSpace(n.FeatureCode) ? "SYSTEM" : n.FeatureCode)
                .Select(g => new
                {
                    FeatureCode = g.Key,
                    TotalCount = g.Count(),
                    UnreadCount = g.Count(n => !n.IsRead)
                })
                .ToList();

            return Ok(summary);
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

            if (unreadNotifications.Count > 0)
            {
                foreach (var n in unreadNotifications)
                {
                    n.IsRead = true;
                }
                await _dbContext.SaveChangesAsync();
            }

            return Ok(new { Message = "Đã đánh dấu tất cả thông báo là đã đọc.", Count = unreadNotifications.Count });
        }
    }
}
