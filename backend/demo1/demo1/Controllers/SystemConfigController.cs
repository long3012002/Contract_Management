using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.DTOs.SystemConfig;
using demo1.Services.EmailNotifications.Templates;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
    IEmailService emailService,
    AppDbContext dbContext,
    IConfiguration config) : ControllerBase
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

    // ─── POST /api/HeThong/system-config/preview-contract-expiry-email ───────

    /// <summary>
    /// Xem trước (preview) tiêu đề và nội dung HTML email cảnh báo hết hạn của hợp đồng.
    /// </summary>
    [HttpPost("preview-contract-expiry-email")]
    public async Task<IActionResult> PreviewContractExpiryEmail([FromBody] TestContractExpiryEmailDto dto)
    {
        if (!await IsAdminAsync()) return Forbid();

        var targetContractId = dto?.ContractId;
        demo1.Models.HopDong? contract = null;

        if (targetContractId.HasValue && targetContractId.Value != Guid.Empty)
        {
            contract = await dbContext.HopDongs
                .Include(h => h.DuAn)
                .Include(h => h.GoiThau).ThenInclude(g => g!.DuAn)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == targetContractId.Value);

            if (contract == null)
            {
                return NotFound(new { Message = $"Không tìm thấy hợp đồng với ID: {targetContractId.Value}" });
            }
        }
        else
        {
            // Nếu người dùng không truyền ID cụ thể, lấy hợp đồng mới nhất trong hệ thống để preview
            contract = await dbContext.HopDongs
                .Include(h => h.DuAn)
                .Include(h => h.GoiThau).ThenInclude(g => g!.DuAn)
                .AsNoTracking()
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync();

            if (contract == null)
            {
                return NotFound(new { Message = "Chưa có hợp đồng nào trong hệ thống để tạo email xem trước." });
            }
        }

        var today = DateTime.UtcNow.Date;
        var deadline = contract.ExpiredDate?.Date ?? today;
        var daysRemaining = (deadline - today).Days;

        var contractName = contract.Name ?? contract.Code ?? "Hợp đồng";
        var contractId = contract.Id;
        var subject = daysRemaining < 0
            ? $"[Quản lý Hợp đồng] Hợp đồng đã quá hạn: {contractName}"
            : daysRemaining == 0
                ? $"[Quản lý Hợp đồng] Hợp đồng hết hạn HÔM NAY: {contractName}"
                : $"[Quản lý Hợp đồng] Hợp đồng sắp hết hạn ({daysRemaining} ngày): {contractName}";

        var baseUrl = config.GetValue<string>("App:BaseUrl", "https://hopdong.co-opbank.vn").TrimEnd('/');
        var link = $"{baseUrl}/contracts/{contractId}";

        var htmlBody = EmailTemplateBuilder.BuildExpiryEmail(
            recipientName: "Người nhận thử nghiệm",
            title: subject,
            entityType: "Hợp đồng",
            entityName: contractName,
            daysRemaining: daysRemaining,
            deadlineDate: deadline,
            detailUrl: link);

        return Ok(new
        {
            ContractId = contractId,
            ContractCode = contract.Code,
            ContractName = contractName,
            ExpiredDate = deadline,
            DaysRemaining = daysRemaining,
            Subject = subject,
            HtmlBody = htmlBody,
            DetailUrl = link
        });
    }

    // ─── POST /api/HeThong/system-config/test-contract-expiry-email ──────────

    /// <summary>
    /// Gửi email test cảnh báo hợp đồng sắp/đã hết hạn tới danh sách người dùng được chọn hoặc email chỉ định.
    /// </summary>
    [HttpPost("test-contract-expiry-email")]
    public async Task<IActionResult> TestContractExpiryEmail([FromBody] TestContractExpiryEmailDto dto)
    {
        if (!await IsAdminAsync()) return Forbid();

        var targetContractId = dto?.ContractId;
        demo1.Models.HopDong? contract = null;

        if (targetContractId.HasValue && targetContractId.Value != Guid.Empty)
        {
            contract = await dbContext.HopDongs
                .Include(h => h.DuAn)
                .Include(h => h.GoiThau).ThenInclude(g => g!.DuAn)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == targetContractId.Value);

            if (contract == null)
            {
                return NotFound(new { Message = $"Không tìm thấy hợp đồng với ID: {targetContractId.Value}" });
            }
        }
        else
        {
            contract = await dbContext.HopDongs
                .Include(h => h.DuAn)
                .Include(h => h.GoiThau).ThenInclude(g => g!.DuAn)
                .AsNoTracking()
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync();

            if (contract == null)
            {
                return NotFound(new { Message = "Chưa có hợp đồng nào trong hệ thống để thực hiện gửi test." });
            }
        }

        var targetEmails = new List<(string Email, string Name)>();

        // 1. Lấy từ UserIds
        if (dto?.UserIds != null && dto.UserIds.Count > 0)
        {
            var users = await dbContext.Users
                .Where(u => dto.UserIds.Contains(u.Id) && !string.IsNullOrWhiteSpace(u.Email))
                .Select(u => new { u.Email, Name = u.FullName ?? u.Username })
                .ToListAsync();

            foreach (var u in users)
            {
                if (!string.IsNullOrWhiteSpace(u.Email))
                    targetEmails.Add((u.Email, u.Name));
            }
        }

        // 2. Lấy từ ManualEmail (nếu có)
        if (!string.IsNullOrWhiteSpace(dto?.ManualEmail))
        {
            var parts = dto.ManualEmail.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (p.Contains('@'))
                    targetEmails.Add((p.Trim(), p.Trim()));
            }
        }

        // Loại bỏ trùng email
        targetEmails = targetEmails
            .GroupBy(t => t.Email.ToLower().Trim())
            .Select(g => g.First())
            .ToList();

        if (targetEmails.Count == 0)
            return BadRequest(new { Message = "Vui lòng chọn ít nhất một người dùng có email hợp lệ hoặc nhập địa chỉ email nhận." });

        var today = DateTime.UtcNow.Date;
        var deadline = contract.ExpiredDate?.Date ?? today;
        var daysRemaining = (deadline - today).Days;

        var contractName = contract.Name ?? contract.Code ?? "Hợp đồng";
        var subject = daysRemaining < 0
            ? $"[Quản lý Hợp đồng] Hợp đồng đã quá hạn: {contractName}"
            : daysRemaining == 0
                ? $"[Quản lý Hợp đồng] Hợp đồng hết hạn HÔM NAY: {contractName}"
                : $"[Quản lý Hợp đồng] Hợp đồng sắp hết hạn ({daysRemaining} ngày): {contractName}";

        var baseUrl = config.GetValue<string>("App:BaseUrl", "https://hopdong.co-opbank.vn").TrimEnd('/');
        var link = $"{baseUrl}/contracts/{contract.Id}";

        var sentList = new List<string>();
        foreach (var recipient in targetEmails)
        {
            var htmlBody = EmailTemplateBuilder.BuildExpiryEmail(
                recipientName: recipient.Name,
                title: subject,
                entityType: "Hợp đồng",
                entityName: contractName,
                daysRemaining: daysRemaining,
                deadlineDate: deadline,
                detailUrl: link);

            await emailService.SendEmailAsync(recipient.Email, subject, htmlBody);
            sentList.Add(recipient.Email);
        }

        return Ok(new
        {
            Message = $"Đã gửi thử nghiệm email cảnh báo hợp đồng tới {sentList.Count} người nhận: {string.Join(", ", sentList)}",
            SentCount = sentList.Count,
            Recipients = sentList,
            Subject = subject
        });
    }
}
