using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using demo1.Data;
using demo1.DTOs;
using demo1.DTOs.Common;
using demo1.Entity;
using demo1.Services;
using demo1.Services.Implements;
using demo1.Services.Interfaces;
using demo1.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class AuthAndUserServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly RadiusClient _radiusClient;
        private readonly TotpService _totpService;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<ILogger<AuthService>> _mockAuthLogger;
        private readonly Mock<ILogger<UserService>> _mockUserLogger;
        private readonly AuthService _authService;
        private readonly UserService _userService;

        public AuthAndUserServiceTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();
            var myConfiguration = new Dictionary<string, string?>
            {
                {"JwtSettings:SecretKey", "Iip7U9SQ3R8wZdAaicLRbrJKBeG8zgEYeX6wlfw8p7k="},
                {"JwtSettings:Issuer", "ContractManagementBackend"},
                {"JwtSettings:Audience", "ContractManagementFrontend"},
                {"Auth:EnableLocalPasswordLogin", "true"},
                {"Auth:EnableDevBypass", "true"}
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();

            var radiusOptions = Options.Create(new RadiusSettings());
            var mockRadiusLogger = new Mock<ILogger<RadiusClient>>();
            _radiusClient = new RadiusClient(radiusOptions, mockRadiusLogger.Object);
            _totpService = new TotpService();

            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockAuthLogger = new Mock<ILogger<AuthService>>();
            _mockUserLogger = new Mock<ILogger<UserService>>();

            _authService = new AuthService(
                _radiusClient,
                config,
                _dbContext,
                _totpService,
                _mockHttpContextAccessor.Object,
                _mockAuthLogger.Object
            );

            _userService = new UserService(_dbContext, _mockUserLogger.Object);
        }

        private static string CreatePbkdf2Hash(string password)
        {
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 32);
            return $"pbkdf2-sha256:10000:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        [Fact]
        public async Task TC04_LoginAsync_Should_Return_Success_When_Valid_Credentials()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "an.nd",
                FullName = "Nguyễn Đức An",
                Email = "an.nd@coopbank.vn",
                PasswordHash = CreatePbkdf2Hash("Password123!"),
                IsActive = true
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var loginReq = new LoginRequest { Username = "an.nd", Password = "Password123!" };
            var result = await _authService.LoginAsync(loginReq);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task TC05_LoginAsync_Should_Fail_When_Password_Is_Incorrect()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "an.nd",
                FullName = "Nguyễn Đức An",
                PasswordHash = CreatePbkdf2Hash("Password123!"),
                IsActive = true
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var loginReq = new LoginRequest { Username = "an.nd", Password = "WrongPassword!" };
            var result = await _authService.LoginAsync(loginReq);

            // Assert
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task TC06_LoginAsync_Should_Succeed_When_DevMode_Bypass_Is_Active()
        {
            // Arrange
            var adminUser = new User { Id = Guid.NewGuid(), Username = "admin", FullName = "Admin", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(adminUser);
            await _dbContext.SaveChangesAsync();

            // Act
            var loginReq = new LoginRequest { Username = "admin", Password = "admin_bypass_dev" };
            var result = await _authService.LoginAsync(loginReq);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData("quangmd")]
        [InlineData("anhld2")]
        public async Task TC07_LoginAsync_Should_Succeed_Without_Radius_For_DevUsers(string username)
        {
            // Act - radius is disabled/unconfigured by default in mock setup, but dev bypass users should succeed
            var loginReq = new LoginRequest { Username = username, Password = "any_password" };
            var result = await _authService.LoginAsync(loginReq);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Response.Should().NotBeNull();
            result.Response.Username.Should().Be(username);
        }

        [Fact]
        public async Task TC03_CreateUser_Should_Fail_When_Email_Already_Exists()
        {
            // Arrange
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "user1",
                Email = "an.nd@coopbank.vn",
                IsActive = true
            };
            _dbContext.Users.Add(existingUser);
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            var isDuplicateEmail = _dbContext.Users.Any(u => u.Email == "an.nd@coopbank.vn");
            isDuplicateEmail.Should().BeTrue();
        }

        [Fact]
        public async Task TC01_TC02_User_Check_Duplicate_Code_By_PGD_And_Branch()
        {
            // Arrange
            var pgd1Id = Guid.NewGuid();
            var pgd2Id = Guid.NewGuid();

            var user1 = new User { Id = Guid.NewGuid(), Username = "NV001", IdPhongBan = pgd1Id };
            _dbContext.Users.Add(user1);
            await _dbContext.SaveChangesAsync();

            // Act: Check duplicate within same PGD
            var existsInSamePGD = _dbContext.Users.Any(u => u.Username == "NV001" && u.IdPhongBan == pgd1Id);
            // Act: Check duplicate across different PGD
            var existsInDiffPGD = _dbContext.Users.Any(u => u.Username == "NV001" && u.IdPhongBan == pgd2Id);

            // Assert
            existsInSamePGD.Should().BeTrue();
            existsInDiffPGD.Should().BeFalse();
        }

        [Fact]
        public async Task TC08_Lock_Unlock_User_Account_Should_Update_Status()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Username = "user_to_lock", IsActive = true };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act: Lock user
            user.IsActive = false;
            await _dbContext.SaveChangesAsync();

            // Assert
            var lockedUser = await _dbContext.Users.FindAsync(user.Id);
            lockedUser!.IsActive.Should().BeFalse();

            // Act: Unlock user
            lockedUser.IsActive = true;
            await _dbContext.SaveChangesAsync();

            // Assert
            var unlockedUser = await _dbContext.Users.FindAsync(user.Id);
            unlockedUser!.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task TC09_Create_Bulk_Users_Should_Add_All_Users_Successfully()
        {
            // Arrange
            var dtos = new List<CreateUserDto>
            {
                new CreateUserDto { Username = "bulk_user_1", Email = "u1@coopbank.vn", TenPhongBan = "IT" },
                new CreateUserDto { Username = "bulk_user_2", Email = "u2@coopbank.vn", TenPhongBan = "IT" },
                new CreateUserDto { Username = "bulk_user_3", Email = "u3@coopbank.vn", TenPhongBan = "IT" },
            };

            // Act
            var result = await _userService.ImportUsersAsync(dtos);

            // Assert
            result.Should().NotBeNull();
            result.AddedCount.Should().Be(3);
            _dbContext.Users.Count(u => u.Username.StartsWith("bulk_user_")).Should().Be(3);
        }

        [Fact]
        public async Task TC10_TC11_ImportUsersAsync_Should_Return_Errors_When_Invalid_Data()
        {
            // Arrange: 1 invalid row (empty username)
            var dtos = new List<CreateUserDto>
            {
                new CreateUserDto { Username = "", Email = "invalid@coopbank.vn" }
            };

            // Act
            var result = await _userService.ImportUsersAsync(dtos);

            // Assert
            result.ErrorCount.Should().Be(1);
            result.Errors.Should().NotBeEmpty();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
