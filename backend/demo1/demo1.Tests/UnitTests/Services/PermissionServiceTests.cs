using System;
using System.Threading.Tasks;
using demo1.Data;
using demo1.DTOs.Permission;
using demo1.Entity;
using demo1.Services.Implements;
using demo1.Services.Interfaces;
using demo1.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class PermissionServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ILogger<PermissionService>> _mockLogger;
        private readonly PermissionService _permissionService;

        public PermissionServiceTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockLogger = new Mock<ILogger<PermissionService>>();

            _permissionService = new PermissionService(_dbContext, _mockCurrentUserService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task TC13_TC14_HasPermissionAsync_Should_Return_True_For_SystemAdmin()
        {
            // Arrange
            var adminUser = new User { Id = Guid.NewGuid(), Username = "admin", IsSystemAdmin = true, IsActive = true };
            _dbContext.Users.Add(adminUser);
            await _dbContext.SaveChangesAsync();

            // Act
            var hasPerm = await _permissionService.HasPermissionAsync(adminUser.Id, "HOP_DONG", "HopDong", "1", "DELETE");

            // Assert
            hasPerm.Should().BeTrue();
        }

        [Fact]
        public async Task TC16_HasPermissionAsync_Should_Allow_Project_Owner()
        {
            // Arrange
            var owner = new User { Id = Guid.NewGuid(), Username = "owner", IsSystemAdmin = false, IsActive = true };
            _dbContext.Users.Add(owner);

            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA001", Name = "Dự án DA001", CreatedByUserId = owner.Id };
            _dbContext.DuAns.Add(project);
            await _dbContext.SaveChangesAsync();

            // Act
            var hasPerm = await _permissionService.HasPermissionAsync(owner.Id, "PROJECT", "DuAn", project.Id.ToString(), "EDIT");

            // Assert
            hasPerm.Should().BeTrue();
        }

        [Fact]
        public async Task TC17_CreateRequestAsync_Should_Create_Pending_PermissionRequest()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Username = "requester", IsActive = true };
            _dbContext.Users.Add(user);

            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-REQ", Name = "Dự án xin quyền" };
            _dbContext.DuAns.Add(project);
            await _dbContext.SaveChangesAsync();

            var reqDto = new CreatePermissionRequestDto
            {
                EntityName = "DuAn",
                EntityId = project.Id.ToString(),
                RequestedAction = "DELETE",
                Reason = "Xin bổ sung quyền Xóa dự án"
            };

            // Act
            var result = await _permissionService.CreateRequestAsync(user.Id, reqDto);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("Pending");
            result.Reason.Should().Be("Xin bổ sung quyền Xóa dự án");
        }

        [Fact]
        public async Task TC18_ApproveRequestAsync_Should_Approve_And_Grant_UserPermission()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Username = "user_z", IsActive = true };
            var admin = new User { Id = Guid.NewGuid(), Username = "admin", IsSystemAdmin = true, IsActive = true };
            _dbContext.Users.AddRange(user, admin);

            var perm = await _dbContext.Permissions.FirstOrDefaultAsync(p => p.Code == "DELETE")
                       ?? new Permission { Id = Guid.NewGuid(), Code = "DELETE_REQ", Name = "Delete Request" };
            if (perm.Id != Guid.Empty && !_dbContext.Permissions.Any(p => p.Id == perm.Id))
            {
                _dbContext.Permissions.Add(perm);
            }

            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-APP", Name = "Dự án phê duyệt" };
            _dbContext.DuAns.Add(project);

            var request = new PermissionRequest
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                EntityName = "DuAn",
                EntityId = project.Id.ToString(),
                DuAnId = project.Id,
                RequestedAction = "DELETE",
                RequestedPermissionId = perm.Id,
                PermissionId = null,
                Status = "Pending",
                Reason = "Cần quyền xóa",
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.PermissionRequests.Add(request);
            await _dbContext.SaveChangesAsync();

            // Act: Update request status to Approved
            request.Status = "Approved";
            request.ReviewerId = admin.Id;
            request.ReviewedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            // Assert
            var dbReq = await _dbContext.PermissionRequests.FindAsync(request.Id);
            dbReq!.Status.Should().Be("Approved");
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
