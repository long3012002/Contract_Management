using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.DTOs.SystemConfig;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Cấu hình Hệ thống.
/// Chỉ dành cho System Admin.
/// </summary>
[Authorize]
[ApiController]
[Route("api/HeThong/system-config")]
public class SystemConfigController(
    ISystemConfigService systemConfigService,
    IAdminService adminService,
    IEmailService emailService) : ControllerBase
{
    private async Task<bool> IsAdminAsync()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return false;
        return await adminService.IsSystemAdminAsync(username);
    }

    private string CurrentUsername => User.Identity?.Name ?? "unknown";

    // ─── GET /api/HeThong/system-config ──────────────────────────────────────

    /// <summary>
    /// Lấy toàn bộ cấu hình hệ thống (chỉ System Admin).
    /// </summary>
    /// <returns>Danh sách tất cả SystemConfig, sắp xếp theo Group → SortOrder.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<SystemConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll()
    {
        if (!await IsAdminAsync()) return Forbid();
        var result = await systemConfigService.GetAllAsync();
        return Ok(result);
    }

    // ─── PUT /api/HeThong/system-config ──────────────────────────────────────

    /// <summary>
    /// Cập nhật nhiều cấu hình cùng lúc (batch update).
    /// </summary>
    /// <param name="dto">Danh sách các cặp Key-Value cần cập nhật.</param>
    /// <returns>Số lượng key được cập nhật thành công.</returns>
    [HttpPut]
    [ProducesResponseType(typeof(UpdateSystemConfigResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateBatch([FromBody] UpdateSystemConfigBatchDto dto)
    {
        if (!await IsAdminAsync()) return Forbid();

        if (dto?.Items == null || dto.Items.Count == 0)
            return BadRequest(new { Message = "Danh sách items không được rỗng." });

        var updated = await systemConfigService.UpdateBatchAsync(dto.Items, CurrentUsername);
        return Ok(new UpdateSystemConfigResultDto
        {
            UpdatedCount = updated,
            Message = $"Đã cập nhật {updated} cấu hình thành công."
        });
    }

    // ─── POST /api/HeThong/system-config/{key}/reset ─────────────────────────

    /// <summary>
    /// Reset một key về giá trị mặc định.
    /// </summary>
    /// <param name="key">Key cần reset (URL-encoded nếu có ký tự đặc biệt).</param>
    [HttpPost("{key}/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetToDefault(string key)
    {
        if (!await IsAdminAsync()) return Forbid();
        try
        {
            await systemConfigService.ResetToDefaultAsync(key, CurrentUsername);
            return Ok(new { Message = $"Key '{key}' đã được reset về mặc định." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    // ─── POST /api/HeThong/system-config/test-email ──────────────────────────

    /// <summary>
    /// Gửi email test để kiểm tra cấu hình SMTP hiện tại.
    /// </summary>
    /// <param name="dto">Email nhận thử nghiệm.</param>
    [HttpPost("test-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TestEmail([FromBody] TestEmailDto dto)
    {
        if (!await IsAdminAsync()) return Forbid();

        if (string.IsNullOrWhiteSpace(dto?.ToEmail))
            return BadRequest(new { Message = "Email nhận không được rỗng." });

        await emailService.SendEmailAsync(
            dto.ToEmail,
            "[Quản lý Hợp đồng] Test email từ trang Cấu hình Hệ thống",
            $"<p>Đây là email kiểm tra gửi từ trang <strong>Cấu hình Hệ thống</strong>.</p>" +
            $"<p>Thời điểm gửi: {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>" +
            $"<p>Nếu bạn nhận được email này, cấu hình SMTP đang hoạt động bình thường.</p>");

        return Ok(new { Message = $"Đã kích hoạt gửi email test đến {dto.ToEmail}." });
    }
}
