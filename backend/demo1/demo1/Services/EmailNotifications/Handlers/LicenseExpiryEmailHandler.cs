using System;
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
/// Gửi email cảnh báo cho các License sắp hết hạn / đã quá hạn.
/// Ngưỡng cảnh báo lấy từ field CanhBaoTruocNgay của từng License
/// (mỗi license có thể tự cấu hình, mặc định từ SystemConfig key "Email:WarnDaysLicense").
/// </summary>
public class LicenseExpiryEmailHandler : IEmailNotificationHandler
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ISystemConfigService _systemConfig;
    private readonly IConfiguration _config;
    private readonly ILogger<LicenseExpiryEmailHandler> _logger;

    public string HandlerName => "LicenseExpiry";

    public LicenseExpiryEmailHandler(
        AppDbContext db,
        IEmailService emailService,
        ISystemConfigService systemConfig,
        IConfiguration config,
        ILogger<LicenseExpiryEmailHandler> logger)
    {
        _db = db;
        _emailService = emailService;
        _systemConfig = systemConfig;
        _config = config;
        _logger = logger;
    }

    public async Task HandleAsync()
    {
        var emailEnabled = await _systemConfig.GetBoolAsync("Email:Enabled", true);
        if (!emailEnabled)
        {
            _logger.LogInformation("[{Handler}] Email notification đang tắt. Bỏ qua.", HandlerName);
            return;
        }

        var today = DateTime.Today;
        var baseUrl = _config.GetValue<string>("App:BaseUrl", "").TrimEnd('/');

        _logger.LogInformation("[{Handler}] Bắt đầu quét gửi email License sắp/đã hết hạn.", HandlerName);

        // LoaiLicense == 2 là vĩnh viễn → bỏ qua
        var licenses = await _db.Licenses
            .Include(l => l.DuAn)
            .Where(l => l.IsActive && l.LoaiLicense != 2 && l.NgayKetThuc.HasValue)
            .AsNoTracking()
            .ToListAsync();

        var toWarn = licenses
            .Where(l => (l.NgayKetThuc!.Value.Date - today).Days <= l.CanhBaoTruocNgay)
            .ToList();

        _logger.LogInformation("[{Handler}] Tìm thấy {Count} license cần cảnh báo.", HandlerName, toWarn.Count);

        foreach (var license in toWarn)
        {
            var daysRemaining = (license.NgayKetThuc!.Value.Date - today).Days;
            var displayName = license.Name ?? license.Code;
            var subject = BuildSubject(daysRemaining, displayName);
            var link = string.IsNullOrEmpty(baseUrl) ? null : $"{baseUrl}/licenses/{license.Id}";

            var targetUsers = await ContractScanWorker.GetTargetUsersForLicenseAsync(_db, license, _systemConfig);
            var emailUsers = targetUsers.Where(u => !string.IsNullOrWhiteSpace(u.Email)).ToList();
            if (emailUsers.Count == 0)
            {
                _logger.LogWarning("[{Handler}] Không tìm thấy người nhận có email hợp lệ cho license mã {Code} (ID: {Id}). Bỏ qua gửi email.", HandlerName, license.Code, license.Id);
                continue;
            }

            foreach (var user in emailUsers)
            {
                bool alreadySent;
                if (daysRemaining < 0)
                {
                    alreadySent = await _db.EmailNotificationLogs
                        .AnyAsync(e => e.UserId == user.Id
                                       && e.EntityType == "License"
                                       && e.EntityId == license.Id.ToString()
                                       && e.Subject.Contains("đã hết hạn"));
                }
                else
                {
                    alreadySent = await _db.EmailNotificationLogs
                        .AnyAsync(e => e.UserId == user.Id
                                       && e.EntityType == "License"
                                       && e.EntityId == license.Id.ToString()
                                       && e.Subject == subject
                                       && e.SentAt.Date == DateTime.UtcNow.Date);
                }
                if (alreadySent) continue;

                var body = EmailTemplateBuilder.BuildExpiryEmail(
                    recipientName: user.FullName ?? user.Username,
                    title: subject,
                    entityType: "License",
                    entityName: displayName,
                    daysRemaining: daysRemaining,
                    deadlineDate: license.NgayKetThuc!.Value,
                    detailUrl: link);

                await _emailService.SendEmailAsync(user.Email!, subject, body);

                _db.EmailNotificationLogs.Add(new EmailNotificationLog
                {
                    UserId = user.Id,
                    EntityType = "License",
                    EntityId = license.Id.ToString(),
                    Subject = subject,
                    SentAt = DateTime.UtcNow
                });

                _logger.LogInformation("[{Handler}] Đã gửi email cho {Email} về license {Code}.", HandlerName, user.Email, license.Code);
            }
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("[{Handler}] Hoàn thành.", HandlerName);
    }

    private static string BuildSubject(int daysRemaining, string name) => daysRemaining < 0
        ? $"[Quản lý Hợp đồng] License đã hết hạn: {name}"
        : daysRemaining == 0
            ? $"[Quản lý Hợp đồng] License hết hạn HÔM NAY: {name}"
            : $"[Quản lý Hợp đồng] License sắp hết hạn ({daysRemaining} ngày): {name}";
}
