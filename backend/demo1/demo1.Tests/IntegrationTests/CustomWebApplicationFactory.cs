using System;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace demo1.Tests.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private PostgreSqlContainer? _dbContainer;
        public bool IsDockerAvailable { get; private set; } = true;

        public async Task InitializeAsync()
        {
            try
            {
                _dbContainer = new PostgreSqlBuilder("postgres:16-alpine")
                    .WithDatabase("test_db")
                    .WithUsername("test_user")
                    .WithPassword("test_password")
                    .Build();
                await _dbContainer.StartAsync();
            }
            catch
            {
                IsDockerAvailable = false;
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            if (_dbContainer != null)
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
            }

            builder.ConfigureServices(services =>
            {
                if (_dbContainer != null)
                {
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<DbContextOptions>();
                    services.RemoveAll<AppDbContext>();

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseNpgsql(_dbContainer.GetConnectionString());
                        options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
                    });

                    using var scope = services.BuildServiceProvider().CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.EnsureCreated();
                }
            });
        }

        public new async Task DisposeAsync()
        {
            if (_dbContainer != null)
            {
                await _dbContainer.DisposeAsync();
            }
        }
    }
}
