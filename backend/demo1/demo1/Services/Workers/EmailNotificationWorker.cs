using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using demo1.Services.EmailNotifications;
using demo1.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace demo1.Services.Workers;

/// <summary>
/// Background worker điều phối toàn bộ email notification.
/// Chạy mỗi ngày một lần lúc 7h sáng (hoặc theo TestIntervalMinutes khi dev/test).
///
/// Để thêm một loại email mới:
///   1. Tạo class implement <see cref="IEmailNotificationHandler"/>
///   2. Đăng ký trong ServiceConfiguration (AddScoped)
///   3. Worker này tự inject và gọi, không cần sửa thêm gì.
/// </summary>
public class EmailNotificationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailNotificationWorker> _logger;
    private readonly IConfiguration _configuration;

    public EmailNotificationWorker(
        IServiceProvider serviceProvider,
        ILogger<EmailNotificationWorker> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailNotificationWorker started.");

        // Chạy ngay lần đầu khi khởi động để không bỏ sót hợp đồng quá hạn qua đêm
        await RunAllHandlersAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            // Hỗ trợ chế độ test: chạy mỗi N phút
            var testMinutes = _configuration.GetValue<int?>("EmailNotification:TestIntervalMinutes");
            if (testMinutes.HasValue && testMinutes.Value > 0)
            {
                _logger.LogInformation("EmailNotificationWorker test mode: chạy lại sau {Minutes} phút.", testMinutes.Value);
                await Task.Delay(TimeSpan.FromMinutes(testMinutes.Value), stoppingToken);
                await RunAllHandlersAsync();
                continue;
            }

            // Production: chạy mỗi ngày lúc 7h sáng (giờ máy chủ)
            var now = DateTime.Now;
            var next7am = now.Date.AddDays(now.Hour >= 7 ? 1 : 0).AddHours(7);
            var delay = next7am - now;

            _logger.LogInformation("EmailNotificationWorker: lần chạy tiếp theo lúc {NextRun} (sau {Hours:F1}h).", next7am, delay.TotalHours);

            try
            {
                await Task.Delay(delay, stoppingToken);
                await RunAllHandlersAsync();
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("EmailNotificationWorker đang dừng.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailNotificationWorker gặp lỗi, thử lại sau 1 giờ.");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task RunAllHandlersAsync()
    {
        // Kiểm tra flag bật/tắt toàn bộ email notification
        var enabled = _configuration.GetValue<bool>("EmailNotification:Enabled", true);
        if (!enabled)
        {
            _logger.LogInformation("EmailNotificationWorker: Email notification đang bị tắt (EmailNotification:Enabled=false). Bỏ qua.");
            return;
        }

        _logger.LogInformation("EmailNotificationWorker: Bắt đầu chạy tất cả handlers...");

        using var scope = _serviceProvider.CreateScope();

        var systemConfig = scope.ServiceProvider.GetService<ISystemConfigService>();
        if (systemConfig != null)
        {
            var emailEnabled = await systemConfig.GetBoolAsync("Email:Enabled", true);
            if (!emailEnabled)
            {
                _logger.LogInformation("EmailNotificationWorker: Cấu hình Email:Enabled = false trong SystemConfig. Bỏ qua chạy các handlers.");
                return;
            }
        }

        // Lấy tất cả handler đã đăng ký – thêm handler mới chỉ cần đăng ký DI, không sửa code ở đây
        var handlers = scope.ServiceProvider.GetServices<IEmailNotificationHandler>();

        foreach (var handler in handlers)
        {
            try
            {
                _logger.LogInformation("EmailNotificationWorker: Chạy handler [{Handler}]...", handler.HandlerName);
                await handler.HandleAsync();
            }
            catch (Exception ex)
            {
                // Một handler lỗi không dừng các handler còn lại
                _logger.LogError(ex, "EmailNotificationWorker: Handler [{Handler}] gặp lỗi.", handler.HandlerName);
            }
        }

        _logger.LogInformation("EmailNotificationWorker: Tất cả handlers đã chạy xong.");
    }
}
