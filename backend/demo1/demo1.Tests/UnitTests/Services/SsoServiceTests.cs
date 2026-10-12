using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Implements;
using demo1.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Moq.Protected;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class SsoServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<ILogger<SsoService>> _mockLogger;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly IMemoryCache _memoryCache;
        private readonly IConfiguration _configuration;

        public SsoServiceTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockLogger = new Mock<ILogger<SsoService>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            var configDict = new Dictionary<string, string?>
            {
                {"SSO:IssuerUrl", "http://103.143.207.168:32081/api/v1"},
                {"SSO:AuthorizationUrl", "http://103.143.207.168:32081/api/v1/oauth2/authorize"},
                {"SSO:TokenUrl", "http://103.143.207.168:32081/api/v1/oauth2/token"},
                {"SSO:UserInfoUrl", "http://103.143.207.168:32081/api/v1/oauth2/userinfo"},
                {"SSO:JwksUrl", "http://103.143.207.168:32081/api/v1/oauth2/jwks"},
                {"SSO:ClientId", "qlcv"},
                {"SSO:ClientSecret", "FLaMbsZ2r0qdcSWytHcgHqfR44vgcXs8"},
                {"SSO:ValidateTokenSignature", "false"}, // Tắt verify chữ ký khi unit test JWT dummy
                {"JwtSettings:SecretKey", "Iip7U9SQ3R8wZdAaicLRbrJKBeG8zgEYeX6wlfw8p7k="},
                {"JwtSettings:Issuer", "ContractManagementBackend"},
                {"JwtSettings:Audience", "ContractManagementFrontend"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var httpContext = new DefaultHttpContext();
            _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            _memoryCache.Dispose();
        }

        private static string GenerateTestIdToken(string sub, string username, string email)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretDummyKeyForUnitTest1234567890!"));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, sub),
                    new Claim("preferred_username", username),
                    new Claim(JwtRegisteredClaimNames.Email, email)
                }),
                Issuer = "http://103.143.207.168:32081/api/v1",
                Audience = "qlcv",
                Expires = DateTime.UtcNow.AddMinutes(60),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [Theory]
        [InlineData("", "verifier", "state", "https://redirect")]
        [InlineData("code", "", "state", "https://redirect")]
        [InlineData("code", "verifier", "", "https://redirect")]
        public async Task HandleCallbackAsync_MissingParameters_ShouldReturnBadRequest(
            string code, string verifier, string state, string redirectUri)
        {
            // Arrange
            var service = new SsoService(
                _configuration,
                _dbContext,
                _mockHttpContextAccessor.Object,
                _mockLogger.Object,
                _mockHttpClientFactory.Object,
                _memoryCache
            );

            var request = new SsoCallbackRequest
            {
                Code = code,
                CodeVerifier = verifier,
                State = state,
                RedirectUri = redirectUri
            };

            // Act
            var result = await service.HandleCallbackAsync(request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.ErrorMessage.Should().Contain("không được để trống");
        }

        [Fact]
        public async Task HandleCallbackAsync_TokenEndpointFails_ShouldReturnBadRequest()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("{\"error\": \"invalid_grant\"}")
                });

            var service = new SsoService(
                _configuration,
                _dbContext,
                _mockHttpContextAccessor.Object,
                _mockLogger.Object,
                _mockHttpClientFactory.Object,
                _memoryCache
            );

            var request = new SsoCallbackRequest
            {
                Code = "valid_code",
                CodeVerifier = "valid_verifier",
                State = "valid_state",
                RedirectUri = "https://app.coopbank.vn/callback"
            };

            // Act
            var result = await service.HandleCallbackAsync(request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.ErrorMessage.Should().Contain("Xác thực mã đăng nhập SSO không thành công");
        }

        [Fact]
        public async Task HandleCallbackAsync_UserNotFoundInDb_ShouldReturnForbidden403()
        {
            // Arrange
            var testSub = "sso-sub-user-not-in-db";
            var idToken = GenerateTestIdToken(testSub, "unknown_user", "unknown@coopbank.vn");

            var tokenResponse = new SsoTokenResponse
            {
                AccessToken = "dummy_access_token",
                IdToken = idToken,
                ExpiresIn = 300,
                TokenType = "Bearer"
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
                });

            var service = new SsoService(
                _configuration,
                _dbContext,
                _mockHttpContextAccessor.Object,
                _mockLogger.Object,
                _mockHttpClientFactory.Object,
                _memoryCache
            );

            var request = new SsoCallbackRequest
            {
                Code = "valid_code",
                CodeVerifier = "valid_verifier",
                State = "valid_state",
                RedirectUri = "https://app.coopbank.vn/callback"
            };

            // Act
            var result = await service.HandleCallbackAsync(request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.ErrorMessage.Should().Contain("chưa được cấp quyền sử dụng hệ thống");
        }

        [Fact]
        public async Task HandleCallbackAsync_UserInactive_ShouldReturnForbidden403()
        {
            // Arrange
            var testSub = "sso-sub-inactive-user";
            var idToken = GenerateTestIdToken(testSub, "inactive_user", "inactive@coopbank.vn");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "inactive_user",
                FullName = "Tài khoản bị khóa",
                Email = "inactive@coopbank.vn",
                SsoSub = testSub,
                IsActive = false
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var tokenResponse = new SsoTokenResponse
            {
                AccessToken = "dummy_access_token",
                IdToken = idToken,
                ExpiresIn = 300,
                TokenType = "Bearer"
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
                });

            var service = new SsoService(
                _configuration,
                _dbContext,
                _mockHttpContextAccessor.Object,
                _mockLogger.Object,
                _mockHttpClientFactory.Object,
                _memoryCache
            );

            var request = new SsoCallbackRequest
            {
                Code = "valid_code",
                CodeVerifier = "valid_verifier",
                State = "valid_state",
                RedirectUri = "https://app.coopbank.vn/callback"
            };

            // Act
            var result = await service.HandleCallbackAsync(request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.ErrorMessage.Should().Contain("đang bị khóa hoặc ngưng hoạt động");
        }

        [Fact]
        public async Task HandleCallbackAsync_ValidUserLinkedBySub_ShouldReturnSuccess200()
        {
            // Arrange
            var testSub = "sso-sub-active-user-123";
            var idToken = GenerateTestIdToken(testSub, "active_user", "active@coopbank.vn");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "active_user",
                FullName = "Nguyễn Văn Hoạt Động",
                Email = "active@coopbank.vn",
                SsoSub = testSub,
                IsActive = true
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var tokenResponse = new SsoTokenResponse
            {
                AccessToken = "dummy_access_token",
                IdToken = idToken,
                ExpiresIn = 300,
                TokenType = "Bearer"
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
                });

            var service = new SsoService(
                _configuration,
                _dbContext,
                _mockHttpContextAccessor.Object,
                _mockLogger.Object,
                _mockHttpClientFactory.Object,
                _memoryCache
            );

            var request = new SsoCallbackRequest
            {
                Code = "valid_code",
                CodeVerifier = "valid_verifier",
                State = "valid_state",
                RedirectUri = "https://app.coopbank.vn/callback"
            };

            // Act
            var result = await service.HandleCallbackAsync(request);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Response.Should().NotBeNull();
            result.Response!.Username.Should().Be("active_user");
            result.Response.AccessToken.Should().NotBeNullOrWhiteSpace();
            result.Response.RefreshToken.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task HandleCallbackAsync_FirstLogin_ShouldLinkSubAndReturnSuccess200()
        {
            // Arrange
            var newSub = "sso-sub-first-time-login-456";
            var idToken = GenerateTestIdToken(newSub, "existing_username", "existing@coopbank.vn");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "existing_username",
                FullName = "Người dùng chưa gắn Sub",
                Email = "existing@coopbank.vn",
                SsoSub = null, // Chưa gắn sub trước đó
                IsActive = true
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var tokenResponse = new SsoTokenResponse
            {
                AccessToken = "dummy_access_token",
                IdToken = idToken,
                ExpiresIn = 300,
                TokenType = "Bearer"
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
                });

            var service = new SsoService(
                _configuration,
                _dbContext,
                _mockHttpContextAccessor.Object,
                _mockLogger.Object,
                _mockHttpClientFactory.Object,
                _memoryCache
            );

            var request = new SsoCallbackRequest
            {
                Code = "valid_code",
                CodeVerifier = "valid_verifier",
                State = "valid_state",
                RedirectUri = "https://app.coopbank.vn/callback"
            };

            // Act
            var result = await service.HandleCallbackAsync(request);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Response!.Username.Should().Be("existing_username");

            // Kiểm tra DB đã được cập nhật SsoSub
            var updatedUser = await _dbContext.Users.FindAsync(user.Id);
            updatedUser.Should().NotBeNull();
            updatedUser!.SsoSub.Should().Be(newSub);
        }
    }
}
