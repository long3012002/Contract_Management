using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        [Fact]
        public async Task GetAuditLogsByProjectIdAsync_CaseInsensitiveAndFKMappingAndChangeFiltering_WorksAsExpected()
        {
            // Arrange
            var (context, mockUserService) = CreateDbContextWithUser("admin");

            var nhomDuAn = new demo1.Entity.DanhMuc.NhomDuAn { Id = Guid.NewGuid(), Code = "NDA01", Name = "Nhóm CNTT" };
            var phanLoai = new demo1.Entity.DanhMuc.PhanLoaiDuAn { Id = Guid.NewGuid(), Code = "PL01", Name = "Dự án nhóm A" };
            var nguonVon = new demo1.Entity.DanhMuc.NguonVon { Id = Guid.NewGuid(), Code = "NV01", Name = "Ngân sách nhà nước" };
            var doiTac = new DoiTac { Id = Guid.NewGuid(), Code = "DT01", Name = "Tập đoàn Viettel" };
            var projectId = Guid.NewGuid();

            context.NhomDuAns.Add(nhomDuAn);
            context.PhanLoaiDuAns.Add(phanLoai);
            context.NguonVons.Add(nguonVon);
            context.DoiTacs.Add(doiTac);

            // Audit log 1: TableName is "DuAn" (variant), EntityId is UPPERCASE Guid string, Action is UPDATE
            var log1 = new AuditLog
            {
                Id = Guid.NewGuid(),
                TableName = "DuAn",
                EntityId = projectId.ToString().ToUpper(),
                Action = "UPDATE",
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                Username = "admin",
                OldValues = $"{{\"Name\":\"Core Banking\",\"NhomDuAnId\":\"{nhomDuAn.Id}\",\"PhanLoaiDuAnId\":\"{phanLoai.Id}\"}}",
                NewValues = $"{{\"Name\":\"Core Banking\",\"NhomDuAnId\":\"{nhomDuAn.Id}\",\"PhanLoaiDuAnId\":\"{Guid.NewGuid()}\"}}"
            };

            // Audit log 2: TableName is "Dự án" (variant), EntityId is LOWERCASE Guid string, Action is CREATE
            var log2 = new AuditLog
            {
                Id = Guid.NewGuid(),
                TableName = "Dự án",
                EntityId = projectId.ToString().ToLower(),
                Action = "CREATE",
                Timestamp = DateTime.UtcNow.AddMinutes(-10),
                Username = "admin",
                NewValues = $"{{\"ChuDauTuId\":\"{doiTac.Id}\",\"NguonVonId\":\"{nguonVon.Id}\"}}"
            };

            context.AuditLogs.AddRange(log1, log2);
            await context.SaveChangesAsync();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<demo1.Mapper.MappingProfile>());
            var serviceProvider = services.BuildServiceProvider();
            var mapper = serviceProvider.GetRequiredService<IMapper>();
            var mockHubContext = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<demo1.Hubs.NotificationHub>>();
            var duAnService = new demo1.Services.Implements.DuAnService(context, mapper, mockUserService.Object, mockHubContext.Object);

            // Act
            var logs = await duAnService.GetAuditLogsByProjectIdAsync(projectId);

            // Assert
            Assert.Equal(2, logs.Count);

            // Log 1 assertions (UPDATE)
            var updateLog = logs.First(l => l.Id == log1.Id);
            Assert.Equal("Dự án", updateLog.TableName);
            Assert.Equal("UPDATE", updateLog.Action);
            Assert.NotNull(updateLog.OldValues);
            Assert.NotNull(updateLog.NewValues);
            Assert.NotNull(updateLog.ChangedColumns);

            // Name field had no change ("Core Banking" vs "Core Banking"), so it should be filtered out!
            Assert.DoesNotContain("Tên", updateLog.ChangedColumns);
            Assert.DoesNotContain("Name", updateLog.ChangedColumns);
            Assert.Contains("Phân loại dự án", updateLog.ChangedColumns);

            // NhomDuAnId had same GUID, so filtered out!
            Assert.DoesNotContain("Nhóm dự án", updateLog.ChangedColumns);

            // Check translated GUID in OldValues for PhanLoaiDuAnId -> "Dự án nhóm A"
            Assert.Contains("Dự án nhóm A", updateLog.OldValues);

            // Log 2 assertions (CREATE)
            var createLog = logs.First(l => l.Id == log2.Id);
            Assert.Equal("Dự án", createLog.TableName);
            Assert.Equal("CREATE", createLog.Action);
            Assert.Null(createLog.OldValues);
            Assert.Contains("Tập đoàn Viettel", createLog.NewValues);
            Assert.Contains("Ngân sách nhà nước", createLog.NewValues);
        }
    }
}
