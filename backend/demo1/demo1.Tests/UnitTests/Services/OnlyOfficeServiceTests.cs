using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using demo1.Data;
using demo1.Entity;
using demo1.Services.Implements;
using demo1.Services.Interfaces;
using demo1.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class OnlyOfficeServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<IPermissionService> _mockPermissionService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly Mock<ILogger<OnlyOfficeService>> _mockLogger;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly OnlyOfficeService _onlyOfficeService;

        public OnlyOfficeServiceTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();

            _mockPermissionService = new Mock<IPermissionService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockLogger = new Mock<ILogger<OnlyOfficeService>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            _mockEnv.Setup(e => e.ContentRootPath).Returns(AppContext.BaseDirectory);

            var uploadSettingsSection = new Mock<IConfigurationSection>();
            uploadSettingsSection.Setup(s => s["StoragePath"]).Returns("uploads");

            var ooSettingsSection = new Mock<IConfigurationSection>();
            ooSettingsSection.Setup(s => s["JwtSecret"]).Returns("TestSecretKeyForOnlyOffice1234567890");
            ooSettingsSection.Setup(s => s["PublicBaseUrl"]).Returns("http://127.0.0.1:5000");

            _mockConfiguration.Setup(c => c.GetSection("UploadSettings")).Returns(uploadSettingsSection.Object);
            _mockConfiguration.Setup(c => c.GetSection("OnlyOfficeSettings")).Returns(ooSettingsSection.Object);

            _onlyOfficeService = new OnlyOfficeService(
                _dbContext,
                _mockPermissionService.Object,
                _mockConfiguration.Object,
                _mockEnv.Object,
                _mockLogger.Object,
                _mockHttpClientFactory.Object,
                _mockHttpContextAccessor.Object
            );
        }

        [Fact]
        public async Task GenerateConfigAsync_Should_Succeed_For_SystemAdmin_In_EditMode()
        {
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin_user",
                IsActive = true,
                IsSystemAdmin = true
            };
            _dbContext.Users.Add(adminUser);

            var attachment = new FileAttachment
            {
                Id = Guid.NewGuid(),
                FileName = "document.docx",
                FilePath = "uploads/document.docx",
                ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                FileSize = 1024,
                EntityType = "DU_AN",
                EntityId = Guid.NewGuid(),
                Code = "DOC-01",
                Name = "Tài liệu dự án",
                IsActive = true
            };
            _dbContext.FileAttachments.Add(attachment);
            await _dbContext.SaveChangesAsync();

            var config = await _onlyOfficeService.GenerateConfigAsync(attachment, "edit", adminUser.Id, adminUser.Username);

            config.Should().NotBeNull();
            config.EditorConfig.Mode.Should().Be("edit");
        }

        [Fact]
        public async Task GenerateConfigAsync_Should_Succeed_For_ProjectOwner_In_EditMode()
        {
            var ownerId = Guid.NewGuid();
            var ownerUser = new User
            {
                Id = ownerId,
                Username = "owner_user",
                IsActive = true,
                IsSystemAdmin = false
            };
            _dbContext.Users.Add(ownerUser);

            var project = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-01",
                Name = "Dự án A",
                CreatedByUserId = ownerId,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.DuAns.Add(project);

            var attachment = new FileAttachment
            {
                Id = Guid.NewGuid(),
                FileName = "document.docx",
                FilePath = "uploads/document.docx",
                ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                FileSize = 1024,
                EntityType = "DU_AN",
                EntityId = project.Id,
                Code = "DOC-01",
                Name = "Tài liệu dự án",
                IsActive = true
            };
            _dbContext.FileAttachments.Add(attachment);
            await _dbContext.SaveChangesAsync();

            var config = await _onlyOfficeService.GenerateConfigAsync(attachment, "edit", ownerId, ownerUser.Username);

            config.Should().NotBeNull();
            config.EditorConfig.Mode.Should().Be("edit");
        }

        [Fact]
        public async Task GenerateConfigAsync_Should_Succeed_For_User_With_Direct_EditPermission_In_EditMode()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "user_edit",
                IsActive = true,
                IsSystemAdmin = false
            };
            _dbContext.Users.Add(user);

            var entityId = Guid.NewGuid();
            var permissionCatalog = await _dbContext.Permissions.FirstAsync(p => p.Code == "EDIT");

            var userPermission = new UserPermission
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FeatureCode = "GOI_THAU",
                EntityName = "GoiThau",
                EntityId = entityId.ToString(),
                Permission = permissionCatalog
            };
            _dbContext.UserPermissions.Add(userPermission);

            var attachment = new FileAttachment
            {
                Id = Guid.NewGuid(),
                FileName = "document.docx",
                FilePath = "uploads/document.docx",
                ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                FileSize = 1024,
                EntityType = "GOI_THAU",
                EntityId = entityId,
                Code = "DOC-01",
                Name = "Tài liệu dự án",
                IsActive = true
            };
            _dbContext.FileAttachments.Add(attachment);
            await _dbContext.SaveChangesAsync();

            var config = await _onlyOfficeService.GenerateConfigAsync(attachment, "edit", userId, user.Username);

            config.Should().NotBeNull();
            config.EditorConfig.Mode.Should().Be("edit");
        }

        [Fact]
        public async Task GenerateConfigAsync_Should_Succeed_For_User_With_ProjectWide_EditPermission_In_EditMode()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "user_proj_edit",
                IsActive = true,
                IsSystemAdmin = false
            };
            _dbContext.Users.Add(user);

            var projectId = Guid.NewGuid();
            var project = new DuAn
            {
                Id = projectId,
                Code = "DA-02",
                Name = "Dự án B",
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.DuAns.Add(project);

            var gt = new GoiThau
            {
                Id = Guid.NewGuid(),
                DuAnId = projectId,
                Code = "GT-01",
                Name = "Gói thầu A",
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.GoiThaus.Add(gt);

            var permissionCatalog = await _dbContext.Permissions.FirstAsync(p => p.Code == "EDIT");

            var projectPermission = new UserPermission
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FeatureCode = "DU_AN",
                EntityName = "DuAn",
                EntityId = projectId.ToString(),
                DuAnId = projectId,
                Permission = permissionCatalog
            };
            _dbContext.UserPermissions.Add(projectPermission);

            var attachment = new FileAttachment
            {
                Id = Guid.NewGuid(),
                FileName = "document.docx",
                FilePath = "uploads/document.docx",
                ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                FileSize = 1024,
                EntityType = "GOI_THAU",
                EntityId = gt.Id,
                Code = "DOC-01",
                Name = "Tài liệu dự án",
                IsActive = true
            };
            _dbContext.FileAttachments.Add(attachment);
            await _dbContext.SaveChangesAsync();

            var config = await _onlyOfficeService.GenerateConfigAsync(attachment, "edit", userId, user.Username);

            config.Should().NotBeNull();
            config.EditorConfig.Mode.Should().Be("edit");
        }

        [Fact]
        public async Task GenerateConfigAsync_Should_Throw_UnauthorizedAccessException_For_User_Without_Permission()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "normal_user",
                IsActive = true,
                IsSystemAdmin = false
            };
            _dbContext.Users.Add(user);

            var attachment = new FileAttachment
            {
                Id = Guid.NewGuid(),
                FileName = "document.docx",
                FilePath = "uploads/document.docx",
                ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                FileSize = 1024,
                EntityType = "DU_AN",
                EntityId = Guid.NewGuid(),
                Code = "DOC-01",
                Name = "Tài liệu dự án",
                IsActive = true
            };
            _dbContext.FileAttachments.Add(attachment);
            await _dbContext.SaveChangesAsync();

            Func<Task> action = async () => await _onlyOfficeService.GenerateConfigAsync(attachment, "edit", userId, user.Username);

            await action.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Bạn không có quyền chỉnh sửa tệp tin đính kèm này.");
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
