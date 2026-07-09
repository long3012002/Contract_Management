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

namespace demo1.Services.Implements
{
    public class AuditLogRetentionWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuditLogRetentionWorker> _logger;
        private readonly IConfiguration _configuration;

        public AuditLogRetentionWorker(
            IServiceProvider serviceProvider,
            ILogger<AuditLogRetentionWorker> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AuditLogRetentionWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanExpiredLogsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning expired audit logs.");
                }

                // Interval to run the cleanup job (default to 24 hours)
                var intervalHours = _configuration.GetValue<int>("AuditLogs:CleanupIntervalHours", 24);
                await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
            }
        }

        private async Task CleanExpiredLogsAsync()
        {
            var retentionDays = _configuration.GetValue<int>("AuditLogs:RetentionDays", 1);
            _logger.LogInformation("Cleaning audit logs older than {RetentionDays} days.", retentionDays);

            var cutoffTime = DateTime.UtcNow.AddDays(-retentionDays);

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Deletes records older than retention time directly in the DB
                var deletedCount = await dbContext.AuditLogs
                    .Where(log => log.Timestamp < cutoffTime)
                    .ExecuteDeleteAsync();

                _logger.LogInformation("Cleaned {DeletedCount} expired audit log entries.", deletedCount);
            }
        }
    }
}
