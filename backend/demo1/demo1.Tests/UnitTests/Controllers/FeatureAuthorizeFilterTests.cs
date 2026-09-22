using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using demo1.Controllers;
using demo1.Data;
using demo1.Entity;
using demo1.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace demo1.Tests.UnitTests.Controllers
{
    public class FeatureAuthorizeFilterTests : IDisposable
    {
        private readonly AppDbContext _dbContext;

        public FeatureAuthorizeFilterTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();
        }

        private AuthorizationFilterContext CreateFilterContext(string username, string httpMethod, string routeKey, string routeValue)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = httpMethod;

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);

            var routeData = new RouteData();
            if (!string.IsNullOrEmpty(routeKey) && !string.IsNullOrEmpty(routeValue))
            {
                routeData.Values[routeKey] = routeValue;
            }

            var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }

        [Fact]
        public async Task Project_GET_Should_PassThrough_When_ProjectId_Does_Not_Exist_In_DB()
        {
            // Arrange: User exists and is NOT admin or project owner
            var user = new User { Id = Guid.NewGuid(), Username = "regular_user", FullName = "Regular", IsActive = true, IsSystemAdmin = false };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var nonExistentProjectId = Guid.NewGuid();
            var filter = new FeatureAuthorizeFilter("DU_AN", _dbContext);
            var context = CreateFilterContext("regular_user", "GET", "id", nonExistentProjectId.ToString());

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert: Filter must NOT return 403 Forbidden. It must let request pass to Controller (Result is null) so Controller returns 404 Not Found.
            context.Result.Should().BeNull();
        }

        [Fact]
        public async Task Project_GET_Should_Return_403_When_ProjectId_Exists_In_DB_But_User_Has_No_Permission()
        {
            // Arrange
            var otherUser = new User { Id = Guid.NewGuid(), Username = "creator", FullName = "Creator", IsActive = true };
            var user = new User { Id = Guid.NewGuid(), Username = "no_perm_user", FullName = "No Perm", IsActive = true, IsSystemAdmin = false };
            var project = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-403",
                Name = "Dự án cấm truy cập",
                CreatedByUserId = otherUser.Id,
                ChuDuAnId = otherUser.Id
            };
            _dbContext.Users.AddRange(otherUser, user);
            _dbContext.DuAns.Add(project);
            await _dbContext.SaveChangesAsync();

            var filter = new FeatureAuthorizeFilter("DU_AN", _dbContext);
            var context = CreateFilterContext("no_perm_user", "GET", "id", project.Id.ToString());

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert: Should return 403 Forbidden
            context.Result.Should().NotBeNull();
            context.Result.Should().BeOfType<JsonResult>();
            var jsonResult = (JsonResult)context.Result!;
            jsonResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task Project_GET_Should_PassThrough_When_User_Has_View_Permission()
        {
            // Arrange
            var otherUser = new User { Id = Guid.NewGuid(), Username = "creator_view", FullName = "Creator", IsActive = true };
            var user = new User { Id = Guid.NewGuid(), Username = "viewer_user", FullName = "Viewer", IsActive = true, IsSystemAdmin = false };
            var project = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-VIEW-01",
                Name = "Dự án được cấp quyền xem",
                CreatedByUserId = otherUser.Id,
                ChuDuAnId = otherUser.Id
            };

            var viewPerm = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstAsync(_dbContext.Permissions, p => p.Code == "VIEW");

            var userPerm = new UserPermission
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DuAnId = project.Id,
                EntityId = project.Id.ToString(),
                FeatureCode = "DU_AN",
                PermissionId = viewPerm.Id,
                Permission = viewPerm
            };

            _dbContext.Users.AddRange(otherUser, user);
            _dbContext.DuAns.Add(project);
            _dbContext.UserPermissions.Add(userPerm);
            await _dbContext.SaveChangesAsync();

            var filter = new FeatureAuthorizeFilter("DU_AN", _dbContext);
            var context = CreateFilterContext("viewer_user", "GET", "id", project.Id.ToString());

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert: Should pass through (Result is null, no 403 and NO LINQ translation exception)
            context.Result.Should().BeNull();
        }

        [Fact]
        public async Task Project_GET_Should_PassThrough_When_User_Has_RolePermission_Access()
        {
            // Arrange
            var otherUser = new User { Id = Guid.NewGuid(), Username = "owner", FullName = "Owner", IsActive = true };
            var user = new User { Id = Guid.NewGuid(), Username = "role_viewer", FullName = "Role Viewer", IsActive = true, IsSystemAdmin = false };
            var role = new Role { Id = Guid.NewGuid(), Name = "Manager", IsActive = true };
            var feature = new Feature { Id = Guid.NewGuid(), Code = "DU_AN", Name = "Quản lý Dự án", IsActive = true };

            var project = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-ROLE-01",
                Name = "Dự án xem theo Role Matrix",
                CreatedByUserId = otherUser.Id,
                ChuDuAnId = otherUser.Id
            };

            var userRole = new UserRole { UserId = user.Id, RoleId = role.Id };
            var rolePermission = new RolePermission
            {
                RoleId = role.Id,
                FeatureId = feature.Id,
                CanAccess = true,
                Permissions = "view"
            };

            _dbContext.Users.AddRange(otherUser, user);
            _dbContext.Roles.Add(role);
            _dbContext.Features.Add(feature);
            _dbContext.DuAns.Add(project);
            _dbContext.UserRoles.Add(userRole);
            _dbContext.RolePermissions.Add(rolePermission);
            await _dbContext.SaveChangesAsync();

            var filter = new FeatureAuthorizeFilter("DU_AN", _dbContext);
            var context = CreateFilterContext("role_viewer", "GET", "id", project.Id.ToString());

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert: Should pass through because RolePermission grants CanAccess = true
            context.Result.Should().BeNull();
        }

        [Fact]
        public async Task Project_POST_Should_PassThrough_When_User_Has_RolePermission_Create()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Username = "role_creator", FullName = "Role Creator", IsActive = true, IsSystemAdmin = false };
            var role = new Role { Id = Guid.NewGuid(), Name = "CreatorRole", IsActive = true };
            var feature = new Feature { Id = Guid.NewGuid(), Code = "DU_AN", Name = "Quản lý Dự án", IsActive = true };

            var userRole = new UserRole { UserId = user.Id, RoleId = role.Id };
            var rolePermission = new RolePermission
            {
                RoleId = role.Id,
                FeatureId = feature.Id,
                CanAccess = true,
                Permissions = "create,update"
            };

            _dbContext.Users.Add(user);
            _dbContext.Roles.Add(role);
            _dbContext.Features.Add(feature);
            _dbContext.UserRoles.Add(userRole);
            _dbContext.RolePermissions.Add(rolePermission);
            await _dbContext.SaveChangesAsync();

            var filter = new FeatureAuthorizeFilter("DU_AN", _dbContext);
            var context = CreateFilterContext("role_creator", "POST", "", "");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert: Should pass through because RolePermission grants "create"
            context.Result.Should().BeNull();
        }

        [Fact]
        public async Task Project_DELETE_Should_Return_403_When_User_RolePermission_Lacks_Delete()
        {
            // Arrange
            var otherUser = new User { Id = Guid.NewGuid(), Username = "owner2", FullName = "Owner2", IsActive = true };
            var user = new User { Id = Guid.NewGuid(), Username = "role_no_delete", FullName = "No Delete", IsActive = true, IsSystemAdmin = false };
            var role = new Role { Id = Guid.NewGuid(), Name = "EditorRole", IsActive = true };
            var feature = new Feature { Id = Guid.NewGuid(), Code = "DU_AN", Name = "Quản lý Dự án", IsActive = true };

            var project = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-NO-DEL",
                Name = "Dự án không được xóa",
                CreatedByUserId = otherUser.Id,
                ChuDuAnId = otherUser.Id
            };

            var userRole = new UserRole { UserId = user.Id, RoleId = role.Id };
            var rolePermission = new RolePermission
            {
                RoleId = role.Id,
                FeatureId = feature.Id,
                CanAccess = true,
                Permissions = "create,update" // NO delete permission
            };

            _dbContext.Users.AddRange(otherUser, user);
            _dbContext.Roles.Add(role);
            _dbContext.Features.Add(feature);
            _dbContext.DuAns.Add(project);
            _dbContext.UserRoles.Add(userRole);
            _dbContext.RolePermissions.Add(rolePermission);
            await _dbContext.SaveChangesAsync();

            var filter = new FeatureAuthorizeFilter("DU_AN", _dbContext);
            var context = CreateFilterContext("role_no_delete", "DELETE", "id", project.Id.ToString());

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert: Should return 403 Forbidden because RolePermission string does NOT contain "delete"
            context.Result.Should().NotBeNull();
            context.Result.Should().BeOfType<JsonResult>();
            ((JsonResult)context.Result!).StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task Project_POST_Should_Return_403_When_User_Is_Owner_Of_Old_Project_But_Role_Lacks_Create()
        {
            // Arrange: User owns an old project, but current Role does NOT have CREATE permission
            var ownerUser = new User { Id = Guid.NewGuid(), Username = "old_owner", FullName = "Old Owner", IsActive = true, IsSystemAdmin = false };
            var role = new Role { Id = Guid.NewGuid(), Name = "ViewerRole", IsActive = true };
            var feature = new Feature { Id = Guid.NewGuid(), Code = "DU_AN", Name = "Quản lý Dự án", IsActive = true };

            var oldProject = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-OLD",
                Name = "Dự án cũ do user sở hữu",
                CreatedByUserId = ownerUser.Id,
                ChuDuAnId = ownerUser.Id
            };

            var userRole = new UserRole { UserId = ownerUser.Id, RoleId = role.Id };
            var rolePermission = new RolePermission
            {
                RoleId = role.Id,
                FeatureId = feature.Id,
                CanAccess = true,
                Permissions = "view" // LACKS create permission
            };

            _dbContext.Users.Add(ownerUser);
            _dbContext.Roles.Add(role);
            _dbContext.Features.Add(feature);
            _dbContext.DuAns.Add(oldProject);
            _dbContext.UserRoles.Add(userRole);
            _dbContext.RolePermissions.Add(rolePermission);
            await _dbContext.SaveChangesAsync();

            var filter = new FeatureAuthorizeFilter("DU_AN", _dbContext);
            var context = CreateFilterContext("old_owner", "POST", "", "");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert: Must return 403 Forbidden because POST creates a NEW project and role lacks CREATE permission, even though user owns an old project!
            context.Result.Should().NotBeNull();
            context.Result.Should().BeOfType<JsonResult>();
            ((JsonResult)context.Result!).StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task Project_PUT_Should_Return_403_When_UserPermission_Is_View_Only_Even_If_Role_Has_Edit()
        {
            // Arrange: Role has EDIT permission, BUT user has explicit entity-level UserPermission with VIEW only
            var creator = new User { Id = Guid.NewGuid(), Username = "creator_user", FullName = "Creator", IsActive = true };
            var userX = new User { Id = Guid.NewGuid(), Username = "user_x", FullName = "User X", IsActive = true, IsSystemAdmin = false };
            var role = new Role { Id = Guid.NewGuid(), Name = "GlobalEditorRole", IsActive = true };
            var feature = new Feature { Id = Guid.NewGuid(), Code = "DU_AN", Name = "Quản lý Dự án", IsActive = true };

            var projectA = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-SCOPED-01",
                Name = "Dự án A bị giới hạn VIEW",
                CreatedByUserId = creator.Id,
                ChuDuAnId = creator.Id
            };

            var userRole = new UserRole { UserId = userX.Id, RoleId = role.Id };
            var rolePermission = new RolePermission
            {
                RoleId = role.Id,
                FeatureId = feature.Id,
                CanAccess = true,
                Permissions = "view,update" // Global role has EDIT
            };

            var viewPermCatalog = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstAsync(_dbContext.Permissions, p => p.Code == "VIEW");

            // Explicit scoped restriction on Project A: VIEW only
            var scopedUserPerm = new UserPermission
            {
                Id = Guid.NewGuid(),
                UserId = userX.Id,
                DuAnId = projectA.Id,
                EntityId = projectA.Id.ToString(),
                FeatureCode = "DU_AN",
                PermissionId = viewPermCatalog.Id,
                Permission = viewPermCatalog
            };

            _dbContext.Users.AddRange(creator, userX);
            _dbContext.Roles.Add(role);
            _dbContext.Features.Add(feature);
            _dbContext.DuAns.Add(projectA);
            _dbContext.UserRoles.Add(userRole);
            _dbContext.RolePermissions.Add(rolePermission);
            _dbContext.UserPermissions.Add(scopedUserPerm);
            await _dbContext.SaveChangesAsync();

            var filter = new FeatureAuthorizeFilter("DU_AN", _dbContext);
            var context = CreateFilterContext("user_x", "PUT", "id", projectA.Id.ToString());

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert: Must return 403 Forbidden because Scoped UserPermission (VIEW only) overrides Global Role EDIT permission!
            context.Result.Should().NotBeNull();
            context.Result.Should().BeOfType<JsonResult>();
            ((JsonResult)context.Result!).StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task Project_PUT_Should_PassThrough_When_User_Has_No_UserPermission_And_Role_Has_Edit()
        {
            // Arrange: User has NO specific UserPermission on Project B, so global Role EDIT permission applies
            var creator = new User { Id = Guid.NewGuid(), Username = "creator_b", FullName = "Creator B", IsActive = true };
            var userY = new User { Id = Guid.NewGuid(), Username = "user_y", FullName = "User Y", IsActive = true, IsSystemAdmin = false };
            var role = new Role { Id = Guid.NewGuid(), Name = "GlobalEditorRole2", IsActive = true };
            var feature = new Feature { Id = Guid.NewGuid(), Code = "DU_AN", Name = "Quản lý Dự án", IsActive = true };

            var projectB = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-UNSCOPED-02",
                Name = "Dự án B không bị giới hạn riêng",
                CreatedByUserId = creator.Id,
                ChuDuAnId = creator.Id
            };

            var userRole = new UserRole { UserId = userY.Id, RoleId = role.Id };
            var rolePermission = new RolePermission
            {
                RoleId = role.Id,
                FeatureId = feature.Id,
                CanAccess = true,
                Permissions = "view,update"
            };

            _dbContext.Users.AddRange(creator, userY);
            _dbContext.Roles.Add(role);
            _dbContext.Features.Add(feature);
            _dbContext.DuAns.Add(projectB);
            _dbContext.UserRoles.Add(userRole);
            _dbContext.RolePermissions.Add(rolePermission);
            await _dbContext.SaveChangesAsync();

            var filter = new FeatureAuthorizeFilter("DU_AN", _dbContext);
            var context = CreateFilterContext("user_y", "PUT", "id", projectB.Id.ToString());

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert: Should pass through because user has no specific UserPermission override, so Role EDIT permission is used
            context.Result.Should().BeNull();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
