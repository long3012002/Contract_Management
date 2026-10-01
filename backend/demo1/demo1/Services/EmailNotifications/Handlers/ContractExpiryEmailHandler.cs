using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.Entity;
using demo1.Services.EmailNotifications.Templates;
using demo1.Services.Interfaces;
using demo1.Services.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace demo1.Services.EmailNotifications.Handlers;

/// <summary>
/// Gửi email cảnh báo cho các hợp đồng sắp hết hạn / đã quá hạn.
/// Ngưỡng cảnh báo đọc từ SystemConfig (DB) key "Email:WarnDaysContract", mặc định 30 ngày.
/// Chỉ gửi một lần mỗi ngày nhờ kiểm tra bảng EmailNotificationLogs.
/// </summary>
public class ContractExpiryEmailHandler : IEmailNotificationHandler
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ISystemConfigService _systemConfig;
    private readonly IConfiguration _config;
    private readonly ILogger<ContractExpiryEmailHandler> _logger;

    public string HandlerName => "ContractExpiry";

    public ContractExpiryEmailHandler(
        AppDbContext db,
        IEmailService emailService,
        ISystemConfigService systemConfig,
        IConfiguration config,
        ILogger<ContractExpiryEmailHandler> logger)
    {
        _db = db;
        _emailService = emailService;
        _systemConfig = systemConfig;
        _config = config;
        _logger = logger;
    }

    public async Task HandleAsync()
    {
        var today = DateTime.Today;
        // Đọc từ DB (SystemConfig) trước, fallback về appsettings
        var emailEnabled = await _systemConfig.GetBoolAsync("Email:Enabled", true);
        if (!emailEnabled)
        {
            _logger.LogInformation("[{Handler}] Email notification đang tắt (Email:Enabled=false). Bỏ qua.", HandlerName);
            return;
        }
        var warnDays = await _systemConfig.GetIntAsync("Email:WarnDaysContract",
            _config.GetValue<int>("ContractScan:WarnDaysBefore", 30));
        var baseUrl = _config.GetValue<string>("App:BaseUrl", "").TrimEnd('/');

        _logger.LogInformation("[{Handler}] Bắt đầu quét gửi email hợp đồng sắp/đã hết hạn (ngưỡng: {WarnDays} ngày).", HandlerName, warnDays);

        var contracts = await _db.HopDongs
            .Include(h => h.DuAn)
            .Include(h => h.GoiThau).ThenInclude(g => g!.DuAn)
            .Where(h => h.IsActive && h.ExpiredDate.HasValue)
            .AsNoTracking()
            .ToListAsync();

        var toWarn = contracts.Where(h => (h.ExpiredDate!.Value.Date - today).Days <= warnDays).ToList();

        _logger.LogInformation("[{Handler}] Tìm thấy {Count} hợp đồng cần cảnh báo.", HandlerName, toWarn.Count);

        foreach (var contract in toWarn)
        {
            var daysRemaining = (contract.ExpiredDate!.Value.Date - today).Days;
            var subject = BuildSubject(daysRemaining, contract.Name ?? contract.Code);
            var link = string.IsNullOrEmpty(baseUrl) ? null : $"{baseUrl}/contracts/{contract.Id}";

            var targetUsers = await ContractScanWorker.GetTargetUsersForContractAsync(_db, contract);
            var emailUsers = targetUsers.Where(u => !string.IsNullOrWhiteSpace(u.Email)).ToList();

            foreach (var user in emailUsers)
            {
                // Chống spam: kiểm tra đã gửi email với tiêu đề này trong ngày chưa
                var alreadySent = await _db.EmailNotificationLogs
                    .AnyAsync(e => e.UserId == user.Id
                                   && e.EntityType == "HopDong"
                                   && e.EntityId == contract.Id.ToString()
                                   && e.Subject == subject
                                   && e.SentAt.Date == DateTime.UtcNow.Date);
                if (alreadySent) continue;

                var body = EmailTemplateBuilder.BuildExpiryEmail(
                    recipientName: user.FullName ?? user.Username,
                    title: subject,
                    entityType: "Hợp đồng",
                    entityName: contract.Name ?? contract.Code,
                    daysRemaining: daysRemaining,
                    deadlineDate: contract.ExpiredDate!.Value,
                    detailUrl: link);

                await _emailService.SendEmailAsync(user.Email!, subject, body);

                _db.EmailNotificationLogs.Add(new EmailNotificationLog
                {
                    UserId = user.Id,
                    EntityType = "HopDong",
                    EntityId = contract.Id.ToString(),
                    Subject = subject,
                    SentAt = DateTime.UtcNow
                });

                _logger.LogInformation("[{Handler}] Đã gửi email cho {Email} về hợp đồng {Code}.", HandlerName, user.Email, contract.Code);
            }
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("[{Handler}] Hoàn thành.", HandlerName);
    }

    private static string BuildSubject(int daysRemaining, string name) => daysRemaining < 0
        ? $"[Co-opBank] Hợp đồng đã quá hạn: {name}"
        : daysRemaining == 0
            ? $"[Co-opBank] Hợp đồng hết hạn HÔM NAY: {name}"
            : $"[Co-opBank] Hợp đồng sắp hết hạn ({daysRemaining} ngày): {name}";
}
