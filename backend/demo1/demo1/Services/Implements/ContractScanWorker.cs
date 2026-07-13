using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using demo1.Data;
using demo1.Entity;
using demo1.Services.Interfaces;

namespace demo1.Services.Implements
{
    public class ContractScanWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ContractScanWorker> _logger;
        private readonly IConfiguration _configuration;

        public ContractScanWorker(
            IServiceProvider serviceProvider,
            ILogger<ContractScanWorker> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ContractScanWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Calculate time to next 0h (midnight)
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1); // Next midnight
                var delay = nextRun - now;

                _logger.LogInformation("Next contract scan scheduled at {NextRun} (in {DelayHours:F2} hours).", nextRun, delay.TotalHours);

                try
                {
                    // For testing, check if there is a dev setting to run immediately or run frequently
                    var testIntervalMinutes = _configuration.GetValue<int?>("ContractScan:TestIntervalMinutes");
                    if (testIntervalMinutes.HasValue && testIntervalMinutes.Value > 0)
                    {
                        _logger.LogInformation("Testing mode enabled: running contract scan every {Minutes} minutes.", testIntervalMinutes.Value);
                        await ScanAndNotifyAsync();
                        await Task.Delay(TimeSpan.FromMinutes(testIntervalMinutes.Value), stoppingToken);
                        continue;
                    }
                    
                    await Task.Delay(delay, stoppingToken);

                    // Run the scan
                    await ScanAndNotifyAsync();
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("ContractScanWorker is stopping.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during contract expiration scan.");
                    // In case of error, wait 1 hour before retrying to prevent rapid loops
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task ScanAndNotifyAsync()
        {
            _logger.LogInformation("Starting contract expiration scan...");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var today = DateTime.Today;

            // Fetch active contracts
            var expiringContracts = await dbContext.HopDongs
                .Where(h => h.IsActive && h.ExpiredDate.HasValue)
                .ToListAsync();

            // Filter contracts with 3 days or less remaining, but not already expired
            var contractsToWarn = expiringContracts
                .Where(h =>
                {
                    var daysRemaining = (h.ExpiredDate!.Value.Date - today).Days;
                    return daysRemaining <= 3 && daysRemaining >= 0;
                })
                .ToList();

            if (!contractsToWarn.Any())
            {
                _logger.LogInformation("No contracts found expiring in 3 days or less.");
                return;
            }

            _logger.LogInformation("Found {Count} contracts expiring in 3 days or less.", contractsToWarn.Count);

            // Fetch all active users to notify
            var activeUsers = await dbContext.Users
                .Where(u => u.IsActive)
                .ToListAsync();

            if (!activeUsers.Any())
            {
                _logger.LogWarning("No active users found to receive notifications.");
                return;
            }

            foreach (var contract in contractsToWarn)
            {
                var daysRemaining = (contract.ExpiredDate!.Value.Date - today).Days;
                _logger.LogInformation("[ContractScan] Phát hiện hợp đồng sắp hết hạn: Mã={Code}, Tên={Name}, Hạn dùng={ExpiredDate:dd/MM/yyyy}, Số ngày còn lại={DaysRemaining}", contract.Code, contract.Name, contract.ExpiredDate.Value, daysRemaining);
                
                var title = "Cảnh báo: Hợp đồng sắp hết hạn";
                var content = $"Hợp đồng '{contract.Name}' (Mã: {contract.Code}) sẽ hết hạn vào ngày {contract.ExpiredDate!.Value:dd/MM/yyyy} (còn lại {daysRemaining} ngày).";
                var link = $"/contracts/{contract.Id}";

                foreach (var user in activeUsers)
                {
                    // Check if this user has already been notified about this contract
                    var alreadyNotified = await dbContext.Notifications
                        .AnyAsync(n => n.UserId == user.Id && n.Link == link);

                    if (alreadyNotified)
                    {
                        _logger.LogInformation("[ContractScan] User {Username} đã nhận được cảnh báo cho hợp đồng {Code} trước đó. Bỏ qua.", user.Username, contract.Code);
                        continue;
                    }

                    _logger.LogInformation("[ContractScan] Đang tạo thông báo hệ thống và gửi email cho user {Username} ({Email}) về hợp đồng {Code}", user.Username, user.Email ?? "Không có email", contract.Code);

                    // 1. Create System Notification
                    var notification = new Notification
                    {
                        Title = title,
                        Content = content,
                        Link = link,
                        UserId = user.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.Notifications.Add(notification);

                    // 2. Send Email (Commented out as requested)
                    /*
                    if (!string.IsNullOrWhiteSpace(user.Email))
                    {
                        var emailBody = $@"
                            <h3>Thông báo hết hạn hợp đồng</h3>
                            <p>Xin chào <strong>{user.FullName}</strong>,</p>
                            <p>Hệ thống quản lý hợp đồng Coopbank xin thông báo:</p>
                            <ul>
                                <li><strong>Tên hợp đồng:</strong> {contract.Name}</li>
                                <li><strong>Mã hợp đồng:</strong> {contract.Code}</li>
                                <li><strong>Ngày hết hạn:</strong> {contract.ExpiredDate!.Value:dd/MM/yyyy}</li>
                                <li><strong>Thời gian còn lại:</strong> {daysRemaining} ngày</li>
                            </ul>
                            <p>Vui lòng truy cập hệ thống để kiểm tra và thực hiện gia hạn nếu cần thiết.</p>
                            <hr/>
                            <p>Đây là email tự động từ hệ thống, vui lòng không trả lời email này.</p>";

                        // Await sending email in background task to avoid blocking the loop
                        _ = emailService.SendEmailAsync(user.Email, $"[Cảnh báo Hợp đồng] {contract.Name} sắp hết hạn", emailBody);
                    }
                    */
                }
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Finished contract expiration scan. Notifications generated and emails triggered.");
        }
    }
}
