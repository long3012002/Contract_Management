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
            var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.TableName == "DuAns" && a.Action == "Tạo mới");
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
            var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.TableName == "DuAns" && a.Action == "Cập nhật");
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
            var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.TableName == "DuAns" && a.Action == "Xóa");
            Assert.NotNull(auditLog);
            Assert.Equal("admin", auditLog.Username);
            Assert.Equal("admin xóa Dự án Thử nghiệm", auditLog.Description);
        }

        [Fact]
        public async Task SaveChangesAsync_GrantUserPermission_GeneratesAuditLogWithFormattedDescription()
        {
            // Arrange
            var (context, mockUserService) = CreateDbContextWithUser("admin");
            var adminUser = new User { Id = Guid.NewGuid(), Username = "admin", FullName = "Admin User", IsActive = true, IsSystemAdmin = true };
            mockUserService.Setup(u => u.GetUserId()).Returns(adminUser.Id);
            
            var user = new User { Id = Guid.NewGuid(), Username = "user1", FullName = "User One", IsActive = true };
            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA100", Name = "Dự án ABC" };
            var perm = await context.Permissions.FirstAsync(p => p.Code == "VIEW");
            
            context.Users.Add(adminUser);
            context.Users.Add(user);
            context.DuAns.Add(project);
            await context.SaveChangesAsync();

            // Act
            var userPerm = new UserPermission
            {
                UserId = user.Id,
                PermissionId = perm.Id,
                FeatureCode = "DU_AN",
                EntityName = "DuAn",
                EntityId = project.Id.ToString(),
                DuAnId = project.Id,
                GrantedByUserId = adminUser.Id
            };
            context.UserPermissions.Add(userPerm);
            await context.SaveChangesAsync();

            // Assert
            var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.TableName == "DuAns" && a.Action == "Cấp quyền");
            Assert.NotNull(auditLog);
            Assert.Equal("admin", auditLog.Username);
            Assert.Contains("admin cấp quyền Xem cho người dùng user1 (User One) trên dự án 'Dự án ABC'", auditLog.Description);
        }

        [Fact]
        public async Task SaveChangesAsync_RevokeUserPermission_GeneratesAuditLogWithFormattedDescription()
        {
            // Arrange
            var (context, mockUserService) = CreateDbContextWithUser("admin");
            var adminUser = new User { Id = Guid.NewGuid(), Username = "admin", FullName = "Admin User", IsActive = true, IsSystemAdmin = true };
            mockUserService.Setup(u => u.GetUserId()).Returns(adminUser.Id);
            
            var user = new User { Id = Guid.NewGuid(), Username = "user1", FullName = "User One", IsActive = true };
            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA100", Name = "Dự án ABC" };
            var perm = await context.Permissions.FirstAsync(p => p.Code == "VIEW");
            
            context.Users.Add(adminUser);
            context.Users.Add(user);
            context.DuAns.Add(project);
            
            var userPerm = new UserPermission
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                PermissionId = perm.Id,
                FeatureCode = "DU_AN",
                EntityName = "DuAn",
                EntityId = project.Id.ToString(),
                DuAnId = project.Id,
                GrantedByUserId = adminUser.Id
            };
            context.UserPermissions.Add(userPerm);
            await context.SaveChangesAsync();

            // Act
            context.UserPermissions.Remove(userPerm);
            await context.SaveChangesAsync();

            // Assert
            var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.TableName == "DuAns" && a.Action == "Thu hồi quyền");
            Assert.NotNull(auditLog);
            Assert.Equal("admin", auditLog.Username);
            Assert.Contains("admin thu hồi quyền Xem của người dùng user1 (User One) trên dự án 'Dự án ABC'", auditLog.Description);
        }

        [Fact]
        public async Task SaveChangesAsync_AddCongViecNguoiLienQuan_GeneratesAuditLogWithFormattedDescription()
        {
            // Arrange
            var (context, mockUserService) = CreateDbContextWithUser("admin");
            var adminUser = new User { Id = Guid.NewGuid(), Username = "admin", FullName = "Admin User", IsActive = true, IsSystemAdmin = true };
            mockUserService.Setup(u => u.GetUserId()).Returns(adminUser.Id);
            
            var user = new User { Id = Guid.NewGuid(), Username = "user1", FullName = "User One", IsActive = true };
            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA100", Name = "Dự án ABC" };
            var goiThau = new GoiThau { Id = Guid.NewGuid(), Code = "GT100", Name = "Gói Thầu A", DuAnId = project.Id };
            var task = new CongViecGoiThau { Id = Guid.NewGuid(), Code = "TASK01", TenTaiLieu = "Soạn thảo hợp đồng", TinhTrang = "Pending", GoiThauId = goiThau.Id };
            
            context.Users.Add(adminUser);
            context.Users.Add(user);
            context.DuAns.Add(project);
            context.GoiThaus.Add(goiThau);
            context.CongViecGoiThaus.Add(task);
            await context.SaveChangesAsync();

            // Act
            var stakeholder = new CongViecNguoiLienQuan
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CongViecGoiThauId = task.Id,
                Code = "NLQ01",
                Name = "Nguoi Lien Quan 1"
            };
            context.CongViecNguoiLienQuans.Add(stakeholder);
            await context.SaveChangesAsync();

            // Assert
            var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.TableName == "DuAns" && a.Action == "Thêm người liên quan");
            Assert.NotNull(auditLog);
            Assert.Equal("admin", auditLog.Username);
            Assert.Equal("admin thêm người liên quan vào Dự án ABC", auditLog.Description);
        }
    }
}
