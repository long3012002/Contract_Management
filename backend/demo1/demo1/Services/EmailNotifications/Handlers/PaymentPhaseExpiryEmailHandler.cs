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
/// Gửi email cảnh báo cho các Đợt thanh toán sắp đến hạn / đã quá hạn.
/// Ngưỡng cảnh báo đọc từ SystemConfig key "Email:WarnDaysPayment", mặc định 30 ngày.
/// </summary>
public class PaymentPhaseExpiryEmailHandler : IEmailNotificationHandler
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ISystemConfigService _systemConfig;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentPhaseExpiryEmailHandler> _logger;

    public string HandlerName => "PaymentPhaseExpiry";

    public PaymentPhaseExpiryEmailHandler(
        AppDbContext db,
        IEmailService emailService,
        ISystemConfigService systemConfig,
        IConfiguration config,
        ILogger<PaymentPhaseExpiryEmailHandler> logger)
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
        var warnDays = await _systemConfig.GetIntAsync("Email:WarnDaysPayment",
            _config.GetValue<int>("DotThanhToanScan:WarnDaysBefore", 30));
        var baseUrl = _config.GetValue<string>("App:BaseUrl", "").TrimEnd('/');

        _logger.LogInformation("[{Handler}] Bắt đầu quét gửi email đợt thanh toán sắp/đã đến hạn (ngưỡng: {WarnDays} ngày).", HandlerName, warnDays);

        var phases = await _db.DotThanhToans
            .Include(d => d.HopDong).ThenInclude(h => h.DuAn)
            .Include(d => d.HopDong).ThenInclude(h => h.GoiThau).ThenInclude(g => g!.DuAn)
            .Where(d => !d.IsPaid && d.NgayThanhToan.HasValue && d.HopDong != null && d.HopDong.IsActive)
            .AsNoTracking()
            .ToListAsync();

        var toWarn = phases
            .Where(d => (d.NgayThanhToan!.Value.Date - today).Days <= warnDays)
            .ToList();

        _logger.LogInformation("[{Handler}] Tìm thấy {Count} đợt thanh toán cần cảnh báo.", HandlerName, toWarn.Count);

        foreach (var phase in toWarn)
        {
            var daysRemaining = (phase.NgayThanhToan!.Value.Date - today).Days;
            var contractName = phase.HopDong?.Name ?? phase.HopDong?.Code ?? "N/A";
            var subject = BuildSubject(daysRemaining, phase.TenDot, contractName);
            var link = string.IsNullOrEmpty(baseUrl) ? null : $"{baseUrl}/contracts/{phase.HopDongId}";
            var amountInfo = $"Số tiền: {phase.GiaTriThanhToan:N0} VNĐ";

            var targetUsers = await ContractScanWorker.GetTargetUsersForContractAsync(_db, phase.HopDong!, _systemConfig);
            var emailUsers = targetUsers.Where(u => !string.IsNullOrWhiteSpace(u.Email)).ToList();

            foreach (var user in emailUsers)
            {
                bool alreadySent;
                if (daysRemaining < 0)
                {
                    alreadySent = await _db.EmailNotificationLogs
                        .AnyAsync(e => e.UserId == user.Id
                                       && e.EntityType == "DotThanhToan"
                                       && e.EntityId == phase.Id.ToString()
                                       && e.Subject.Contains("quá hạn"));
                }
                else
                {
                    alreadySent = await _db.EmailNotificationLogs
                        .AnyAsync(e => e.UserId == user.Id
                                       && e.EntityType == "DotThanhToan"
                                       && e.EntityId == phase.Id.ToString()
                                       && e.Subject == subject
                                       && e.SentAt.Date == DateTime.UtcNow.Date);
                }
                if (alreadySent) continue;

                var displayName = $"{phase.TenDot} – {contractName}";
                var body = EmailTemplateBuilder.BuildExpiryEmail(
                    recipientName: user.FullName ?? user.Username,
                    title: subject,
                    entityType: "Đợt thanh toán",
                    entityName: displayName,
                    daysRemaining: daysRemaining,
                    deadlineDate: phase.NgayThanhToan!.Value,
                    detailUrl: link,
                    extraInfo: amountInfo);

                await _emailService.SendEmailAsync(user.Email!, subject, body);

                _db.EmailNotificationLogs.Add(new EmailNotificationLog
                {
                    UserId = user.Id,
                    EntityType = "DotThanhToan",
                    EntityId = phase.Id.ToString(),
                    Subject = subject,
                    SentAt = DateTime.UtcNow
                });

                _logger.LogInformation("[{Handler}] Đã gửi email cho {Email} về đợt thanh toán {TenDot}.", HandlerName, user.Email, phase.TenDot);
            }
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("[{Handler}] Hoàn thành.", HandlerName);
    }

    private static string BuildSubject(int daysRemaining, string tenDot, string contractName) => daysRemaining < 0
        ? $"[Quản lý Hợp đồng] Đợt thanh toán quá hạn: {tenDot} ({contractName})"
        : daysRemaining == 0
            ? $"[Quản lý Hợp đồng] Đợt thanh toán đến hạn HÔM NAY: {tenDot} ({contractName})"
            : $"[Quản lý Hợp đồng] Đợt thanh toán sắp đến hạn ({daysRemaining} ngày): {tenDot} ({contractName})";
}
