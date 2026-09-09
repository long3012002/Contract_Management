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
                LoaiDuAn = 1,
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
                LoaiDuAn = 1,
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

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
