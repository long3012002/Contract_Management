using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace demo1.Data;

public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Luôn tự động kiểm tra kết nối CSDL và áp dụng các EF Core Migrations còn thiếu khi ứng dụng khởi động.
    /// Hoạt động độc lập hoàn toàn, không phụ thuộc vào cấu hình hay quá trình Seeding dữ liệu.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("DatabaseMigration");

        try
        {
            logger?.LogInformation("Đang kiểm tra kết nối CSDL để thực hiện EF Core Migrations...");

            int maxRetries = 10;
            int retryDelayMs = 2000;
            bool connected = false;

            for (int i = 1; i <= maxRetries; i++)
            {
                try
                {
                    if (await context.Database.CanConnectAsync())
                    {
                        connected = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Lần {Attempt}/{MaxRetries}: Chưa thể kết nối Database, thử lại sau {Delay}ms...", i, maxRetries, retryDelayMs);
                }

                if (i < maxRetries)
                {
                    await Task.Delay(retryDelayMs);
                }
            }

            if (!connected)
            {
                logger?.LogError("Không thể kết nối Database sau {MaxRetries} lần thử. Bỏ qua ApplyDatabaseMigrationsAsync.", maxRetries);
                return;
            }

            try
            {
                // Dọn dẹp bảng Hangfire cũ trong schema public nếu có để tránh xung đột
                await context.Database.ExecuteSqlRawAsync(@"
                    DROP TABLE IF EXISTS public.""lock"" CASCADE;
                    DROP TABLE IF EXISTS public.""schema"" CASCADE;
                    DROP TABLE IF EXISTS public.""job"" CASCADE;
                    DROP TABLE IF EXISTS public.""state"" CASCADE;
                    DROP TABLE IF EXISTS public.""jobparameter"" CASCADE;
                    DROP TABLE IF EXISTS public.""jobqueue"" CASCADE;
                    DROP TABLE IF EXISTS public.""list"" CASCADE;
                    DROP TABLE IF EXISTS public.""set"" CASCADE;
                    DROP TABLE IF EXISTS public.""counter"" CASCADE;
                    DROP TABLE IF EXISTS public.""aggregatedcounter"" CASCADE;
                    DROP TABLE IF EXISTS public.""hash"" CASCADE;
                    DROP TABLE IF EXISTS public.""server"" CASCADE;
                ");
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Bỏ qua dọn dẹp bảng Hangfire cũ trong public schema.");
            }

            logger?.LogInformation("Bắt đầu áp dụng EF Core Migrations (Database.MigrateAsync)...");
            await context.Database.MigrateAsync();
            logger?.LogInformation("Hoàn tất áp dụng toàn bộ EF Core Migrations thành công.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Đã xảy ra lỗi khi áp dụng EF Core Migrations.");
            throw;
        }
    }
}
