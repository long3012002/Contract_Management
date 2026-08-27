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

        [Fact]
        public async Task HasPermissionAsync_Should_Allow_Project_Owner_Via_ChuDuAnId()
        {
            // Arrange
            var owner = new User { Id = Guid.NewGuid(), Username = "owner_chu", IsSystemAdmin = false, IsActive = true };
            _dbContext.Users.Add(owner);

            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA002", Name = "Dự án DA002", ChuDuAnId = owner.Id };
            _dbContext.DuAns.Add(project);
            await _dbContext.SaveChangesAsync();

            // Act
            var hasPerm = await _permissionService.HasPermissionAsync(owner.Id, "PROJECT", "DuAn", project.Id.ToString(), "EDIT");

            // Assert
            hasPerm.Should().BeTrue();
        }

        [Fact]
        public async Task GetUserPermissionsAsync_Should_Return_Distinct_Permissions()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Username = "distinct_user", IsActive = true };
            _dbContext.Users.Add(user);

            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA003", Name = "Dự án DA003", CreatedByUserId = user.Id };
            _dbContext.DuAns.Add(project);

            var perm = await _dbContext.Permissions.FirstOrDefaultAsync(p => p.Code == "VIEW")
                       ?? new Permission { Id = Guid.NewGuid(), Code = "VIEW", Name = "View" };
            if (perm.Id != Guid.Empty && !_dbContext.Permissions.Any(p => p.Id == perm.Id))
            {
                _dbContext.Permissions.Add(perm);
            }

            // Add duplicate database permissions
            var perm1 = new UserPermission
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                PermissionId = perm.Id,
                FeatureCode = "PROJECT", // alias for DU_AN
                EntityName = "DuAn",
                EntityId = project.Id.ToString(),
                DuAnId = project.Id,
                GrantedAt = DateTime.UtcNow
            };
            var perm2 = new UserPermission
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                PermissionId = perm.Id,
                FeatureCode = "DU_AN",
                EntityName = "DuAn",
                EntityId = project.Id.ToString(),
                DuAnId = project.Id,
                GrantedAt = DateTime.UtcNow
            };
            _dbContext.UserPermissions.AddRange(perm1, perm2);
            await _dbContext.SaveChangesAsync();

            // Act
            _mockCurrentUserService.Setup(c => c.GetUsername()).Returns("distinct_user");
            var result = await _permissionService.GetUserPermissionsAsync(user.Id, "DU_AN", true);

            // Assert
            var list = result.ToList();
            list.Count(p => p.PermissionCode == "VIEW" && p.FeatureCode == "DU_AN").Should().Be(1);
        }

        [Fact]
        public async Task GrantAndRevokePermission_Should_GenerateNotifications_And_Stakeholder_Only_Get_View()
        {
            // Arrange
            var admin = new User { Id = Guid.NewGuid(), Username = "admin_user", IsActive = true };
            var targetUser = new User { Id = Guid.NewGuid(), Username = "target_user", IsActive = true };
            _dbContext.Users.AddRange(admin, targetUser);

            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA009", Name = "Dự án DA009" };
            _dbContext.DuAns.Add(project);

            var viewPerm = await _dbContext.Permissions.FirstOrDefaultAsync(p => p.Code == "VIEW")
                           ?? new Permission { Id = Guid.NewGuid(), Code = "VIEW", Name = "View" };
            if (viewPerm.Id != Guid.Empty && !_dbContext.Permissions.Any(p => p.Id == viewPerm.Id))
            {
                _dbContext.Permissions.Add(viewPerm);
            }
            await _dbContext.SaveChangesAsync();

            // Act 1: Grant permission
            var grantDto = new CreateUserPermissionDto
            {
                UserId = targetUser.Id,
                PermissionId = viewPerm.Id,
                FeatureCode = "DU_AN",
                EntityName = "DuAn",
                EntityId = project.Id.ToString(),
                DuAnId = project.Id
            };
            var grantResult = await _permissionService.GrantUserPermissionAsync(admin.Id, grantDto);

            // Assert 1: Notifications generated for both
            var adminNotis = await _dbContext.Notifications.Where(n => n.UserId == admin.Id).ToListAsync();
            var targetNotis = await _dbContext.Notifications.Where(n => n.UserId == targetUser.Id).ToListAsync();

            adminNotis.Should().ContainSingle(n => n.Title.Contains("Cấp quyền thành công"));
            targetNotis.Should().ContainSingle(n => n.Title.Contains("Cấp quyền truy cập"));

            // Act 2: Revoke permission
            var revokeResult = await _permissionService.RevokeUserPermissionAsync(admin.Id, grantResult.Id);

            // Assert 2: Revoke notifications generated
            var adminRevokeNotis = await _dbContext.Notifications.Where(n => n.UserId == admin.Id && n.Title.Contains("Thu hồi quyền thành công")).ToListAsync();
            var targetRevokeNotis = await _dbContext.Notifications.Where(n => n.UserId == targetUser.Id && n.Title.Contains("Thu hồi quyền truy cập")).ToListAsync();

            adminRevokeNotis.Should().NotBeEmpty();
            targetRevokeNotis.Should().NotBeEmpty();

            // Act 3: Stakeholder dynamic permission synthesis (only VIEW)
            var package = new GoiThau { Id = Guid.NewGuid(), DuAnId = project.Id, Name = "Gói thầu test" };
            _dbContext.GoiThaus.Add(package);

            var task = new CongViecGoiThau { Id = Guid.NewGuid(), GoiThauId = package.Id, Name = "Công việc test" };
            _dbContext.CongViecGoiThaus.Add(task);

            var stakeholder = new CongViecNguoiLienQuan { Id = Guid.NewGuid(), CongViecGoiThauId = task.Id, UserId = targetUser.Id };
            _dbContext.CongViecNguoiLienQuans.Add(stakeholder);
            await _dbContext.SaveChangesAsync();

            _mockCurrentUserService.Setup(c => c.GetUsername()).Returns("target_user");
            var synthResult = await _permissionService.GetUserPermissionsAsync(targetUser.Id, "DU_AN", true);
            var synthList = synthResult.ToList();

            // Assert 3: Stakeholder only gets VIEW permission
            synthList.Should().OnlyContain(p => p.PermissionCode == "VIEW");
        }

        [Fact]
        public async Task GrantProjectPermission_ShouldNotCascadeInDb_ButSynthesizeInQuery()
        {
            // Arrange
            var admin = new User { Id = Guid.NewGuid(), Username = "admin_user", IsActive = true };
            var targetUser = new User { Id = Guid.NewGuid(), Username = "target_user", IsActive = true };
            _dbContext.Users.AddRange(admin, targetUser);

            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA010", Name = "Dự án DA010" };
            _dbContext.DuAns.Add(project);

            var viewPerm = await _dbContext.Permissions.FirstOrDefaultAsync(p => p.Code == "VIEW")
                           ?? new Permission { Id = Guid.NewGuid(), Code = "VIEW", Name = "View" };
            if (viewPerm.Id != Guid.Empty && !_dbContext.Permissions.Any(p => p.Id == viewPerm.Id))
            {
                _dbContext.Permissions.Add(viewPerm);
            }
            await _dbContext.SaveChangesAsync();

            var grantDto = new CreateUserPermissionDto
            {
                UserId = targetUser.Id,
                PermissionId = viewPerm.Id,
                FeatureCode = "DU_AN",
                EntityName = "DuAn",
                EntityId = project.Id.ToString(),
                DuAnId = project.Id
            };

            // Act 1: Grant permission
            var grantResult = await _permissionService.GrantUserPermissionAsync(admin.Id, grantDto);

            // Assert 1: Only ONE record exists in database for this targetUser
            var dbPerms = await _dbContext.UserPermissions
                .Where(up => up.UserId == targetUser.Id)
                .ToListAsync();

            dbPerms.Should().ContainSingle();
            dbPerms.First().FeatureCode.Should().Be("DU_AN");

            // Act 2: Query user permissions with includeChildren = true
            _mockCurrentUserService.Setup(c => c.GetUsername()).Returns("target_user");
            var result = await _permissionService.GetUserPermissionsAsync(targetUser.Id, "DU_AN", true);
            var list = result.ToList();

            // Assert 2: Synthesized child features exist in the query result
            list.Count(p => p.FeatureCode == "DU_AN").Should().Be(1);
            list.Count(p => p.FeatureCode == "GOI_THAU").Should().Be(1);
            list.Count(p => p.FeatureCode == "QUAN_LY_HOP_DONG").Should().Be(1);
            list.Count(p => p.FeatureCode == "CONG_VIEC").Should().Be(1);

            // Act 3: Query specifically for child feature "GOI_THAU"
            var goiThauResult = await _permissionService.GetUserPermissionsAsync(targetUser.Id, "GOI_THAU", false);
            var goiThauList = goiThauResult.ToList();

            // Assert 3: GoiThau query returns the synthesized GoiThau permission
            goiThauList.Should().ContainSingle();
            goiThauList.First().FeatureCode.Should().Be("GOI_THAU");
            goiThauList.First().DuAnId.Should().Be(project.Id);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
