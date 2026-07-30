using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;

namespace demo1.Controllers
{
    /// <summary>
    /// API Tra cứu Nhật ký Hệ thống (Audit Logs).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/HeThong/admin/audit-logs")]
    public class AuditLogsController(IAdminService adminService) : ControllerBase
    {
        private async Task<bool> IsAdminAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            return await adminService.IsSystemAdminAsync(username);
        }

        /// <summary>
        /// Xem Nhật ký Hệ thống (Audit Logs) có phân trang và lọc theo Người dùng / Ngày / Tên bảng.
        /// </summary>
        /// <param name="userId">ID Người dùng thực hiện thao tác (tùy chọn)</param>
        /// <param name="date">Ngày thao tác (tùy chọn)</param>
        /// <param name="tableName">Tên bảng bị ảnh hưởng (tùy chọn)</param>
        /// <param name="page">Trang hiện tại (Mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (Mặc định: 20)</param>
        /// <returns>Danh sách Audit Logs phân trang</returns>
        /// <response code="200">Lấy nhật ký hệ thống thành công</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<AuditLog>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] string? userId,
            [FromQuery] DateTime? date,
            [FromQuery] string? tableName,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!await IsAdminAsync()) return Forbid();
            var result = await adminService.GetAuditLogsAsync(userId, date, tableName, page, pageSize);
            return Ok(result);
        }
    }
}
