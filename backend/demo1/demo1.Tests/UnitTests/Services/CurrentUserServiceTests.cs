using System;
using System.Security.Claims;
using demo1.Services.Implements;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class CurrentUserServiceTests
    {
        [Fact]
        public void GetUserId_Should_Return_Guid_When_Multiple_NameIdentifier_Claims_Exist_And_One_Is_Guid()
        {
            // Arrange
            var expectedGuid = Guid.NewGuid();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin"), // Invalid GUID claim (from sub mapping)
                new Claim(ClaimTypes.NameIdentifier, expectedGuid.ToString()) // Valid GUID claim
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.User).Returns(principal);

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

            var currentUserService = new CurrentUserService(mockHttpContextAccessor.Object);

            // Act
            var result = currentUserService.GetUserId();

            // Assert
            result.Should().Be(expectedGuid);
        }

        [Fact]
        public void GetUserId_Should_Return_Guid_When_Single_Valid_Guid_NameIdentifier_Claim_Exists()
        {
            // Arrange
            var expectedGuid = Guid.NewGuid();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, expectedGuid.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.User).Returns(principal);

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

            var currentUserService = new CurrentUserService(mockHttpContextAccessor.Object);

            // Act
            var result = currentUserService.GetUserId();

            // Assert
            result.Should().Be(expectedGuid);
        }

        [Fact]
        public void GetUserId_Should_Return_Null_When_No_Valid_Guid_NameIdentifier_Claim_Exists()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.User).Returns(principal);

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

            var currentUserService = new CurrentUserService(mockHttpContextAccessor.Object);

            // Act
            var result = currentUserService.GetUserId();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetUserId_Should_Return_Null_When_No_NameIdentifier_Claims_Exist()
        {
            // Arrange
            var claims = new Claim[] { };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.User).Returns(principal);

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

            var currentUserService = new CurrentUserService(mockHttpContextAccessor.Object);

            // Act
            var result = currentUserService.GetUserId();

            // Assert
            result.Should().BeNull();
        }
    }
}
