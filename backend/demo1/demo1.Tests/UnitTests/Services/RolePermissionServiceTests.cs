using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Implements;
using demo1.Services.Interfaces;
using demo1.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class RolePermissionServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ILogger<AdminService>> _mockLogger;
        private readonly AdminService _adminService;

        public RolePermissionServiceTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(u => u.GetUsername()).Returns("admin_test");
            _mockLogger = new Mock<ILogger<AdminService>>();

            _adminService = new AdminService(_dbContext, _mockCurrentUserService.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Fact]
        public async Task UpdateRolePermissionsAsync_Should_Persist_RolePermissions_To_Database()
        {
            // Arrange
            var role = new Role { Id = Guid.NewGuid(), Name = "Quản lý Dự án", IsActive = true };
            var feature1 = new Feature { Id = Guid.NewGuid(), Code = "DU_AN", Name = "Quản lý Dự án", IsActive = true };
            var feature2 = new Feature { Id = Guid.NewGuid(), Code = "GOI_THAU", Name = "Quản lý Gói thầu", IsActive = true };

            _dbContext.Roles.Add(role);
            _dbContext.Features.AddRange(feature1, feature2);
            await _dbContext.SaveChangesAsync();

            var updateDtos = new List<UpdateRolePermissionDto>
            {
                new UpdateRolePermissionDto { FeatureId = feature1.Id, CanAccess = true, Permissions = "create" },
                new UpdateRolePermissionDto { FeatureId = feature2.Id, CanAccess = true, Permissions = "delete" }
            };

            // Act
            await _adminService.UpdateRolePermissionsAsync(role.Id, updateDtos);

            // Assert
            var savedPermissions = _dbContext.RolePermissions.Where(rp => rp.RoleId == role.Id).ToList();
            savedPermissions.Should().HaveCount(2);

            var duAnPerm = savedPermissions.FirstOrDefault(rp => rp.FeatureId == feature1.Id);
            duAnPerm.Should().NotBeNull();
            duAnPerm!.CanAccess.Should().BeTrue();
            duAnPerm.Permissions.Should().Be("create");

            var goiThauPerm = savedPermissions.FirstOrDefault(rp => rp.FeatureId == feature2.Id);
            goiThauPerm.Should().NotBeNull();
            goiThauPerm!.CanAccess.Should().BeTrue();
            goiThauPerm.Permissions.Should().Be("delete");

            // Verify GetRolePermissionsAsync returns the updated data
            var result = await _adminService.GetRolePermissionsAsync(role.Id);
            var resultList = result.ToList();
            resultList.Should().HaveCount(2);

            var res1 = resultList.First(r => r.FeatureId == feature1.Id);
            res1.CanAccess.Should().BeTrue();
            res1.Permissions.Should().Be("create");
        }
    }
}
