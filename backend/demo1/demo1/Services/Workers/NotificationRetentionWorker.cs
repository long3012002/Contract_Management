using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using demo1.Data;

namespace demo1.Services.Workers
{
    public class NotificationRetentionWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationRetentionWorker> _logger;
        private readonly IConfiguration _configuration;

        public NotificationRetentionWorker(
            IServiceProvider serviceProvider,
            ILogger<NotificationRetentionWorker> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationRetentionWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var isEnabled = _configuration.GetValue<bool>("NotificationRetention:Enabled", true);
                if (isEnabled)
                {
                    try
                    {
                        await CleanExpiredReadNotificationsAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred while cleaning expired read notifications.");
                    }
                }

                // Interval to run the cleanup job (default to 24 hours)
                var intervalHours = _configuration.GetValue<int>("NotificationRetention:CleanupIntervalHours", 24);
                await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
            }
        }

        public async Task<int> CleanExpiredReadNotificationsAsync(CancellationToken stoppingToken = default)
        {
            var retentionDays = _configuration.GetValue<int>("NotificationRetention:ReadRetentionDays", 14);
            var batchSize = _configuration.GetValue<int>("NotificationRetention:BatchSize", 2000);

            _logger.LogInformation("Cleaning read notifications older than {RetentionDays} days.", retentionDays);

            var cutoffTime = DateTime.UtcNow.AddDays(-retentionDays);
            var totalDeleted = 0;

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Process batch deletion to prevent long table locking
                while (!stoppingToken.IsCancellationRequested)
                {
                    var expiredIds = await dbContext.Notifications
                        .AsNoTracking()
                        .Where(n => n.IsRead && n.CreatedAt < cutoffTime)
                        .OrderBy(n => n.CreatedAt)
                        .Select(n => n.Id)
                        .Take(batchSize)
                        .ToListAsync(stoppingToken);

                    if (expiredIds.Count == 0)
                    {
                        break;
                    }

                    var deletedCount = await dbContext.Notifications
                        .Where(n => expiredIds.Contains(n.Id))
                        .ExecuteDeleteAsync(stoppingToken);

                    totalDeleted += deletedCount;

                    if (expiredIds.Count < batchSize)
                    {
                        break;
                    }
                }

                _logger.LogInformation("Cleaned a total of {TotalDeleted} expired read notifications.", totalDeleted);
            }

            return totalDeleted;
        }
    }
}
