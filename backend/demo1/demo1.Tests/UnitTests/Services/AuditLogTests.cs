using System;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class AuditLogTests
    {
        private (AppDbContext context, Mock<ICurrentUserService> mockCurrentUserService) CreateDbContextWithUser(string username)
        {
            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(u => u.GetUsername()).Returns(username);

            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new AppDbContext(options, mockUserService.Object);
            context.Database.EnsureCreated();

            return (context, mockUserService);
        }

        [Fact]
        public async Task SaveChangesAsync_CreateEntity_GeneratesAuditLogWithFormattedDescription()
        {
            // Arrange
            var (context, _) = CreateDbContextWithUser("admin");
            var duAn = new DuAn
            {
                Code = "DA001",
                Name = "Dự án Nâng cấp Core Banking"
            };

            // Act
            context.DuAns.Add(duAn);
            await context.SaveChangesAsync();

            // Assert
            var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.TableName == "DuAns" && a.Action == "CREATE");
            Assert.NotNull(auditLog);
            Assert.Equal("admin", auditLog.Username);
            Assert.Equal("admin tạo mới Dự án Nâng cấp Core Banking", auditLog.Description);
        }

        [Fact]
        public async Task SaveChangesAsync_UpdateEntity_GeneratesAuditLogWithFormattedDescription()
        {
            // Arrange
            var (context, _) = CreateDbContextWithUser("nguyenvana");
            var duAn = new DuAn
            {
                Code = "DA002",
                Name = "Hệ thống Quản lý Hợp đồng"
            };
            context.DuAns.Add(duAn);
            await context.SaveChangesAsync();

            // Act
            duAn.Name = "Hệ thống Quản lý Hợp đồng v2";
            context.DuAns.Update(duAn);
            await context.SaveChangesAsync();

            // Assert
            var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.TableName == "DuAns" && a.Action == "UPDATE");
            Assert.NotNull(auditLog);
            Assert.Equal("nguyenvana", auditLog.Username);
            Assert.Equal("nguyenvana cập nhật Hệ thống Quản lý Hợp đồng v2", auditLog.Description);
        }

        [Fact]
        public async Task SaveChangesAsync_DeleteEntity_GeneratesAuditLogWithFormattedDescription()
        {
            // Arrange
            var (context, _) = CreateDbContextWithUser("admin");
            var duAn = new DuAn
            {
                Code = "DA003",
                Name = "Dự án Thử nghiệm"
            };
            context.DuAns.Add(duAn);
            await context.SaveChangesAsync();

            // Act
            context.DuAns.Remove(duAn);
            await context.SaveChangesAsync();

            // Assert
            var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.TableName == "DuAns" && a.Action == "DELETE");
            Assert.NotNull(auditLog);
            Assert.Equal("admin", auditLog.Username);
            Assert.Equal("admin xóa Dự án Thử nghiệm", auditLog.Description);
        }
    }
}
