using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace demo1.Tests.IntegrationTests.Controllers
{
    public class DuAnControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient? _client;

        public DuAnControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            if (_factory.IsDockerAvailable)
            {
                _client = factory.CreateClient();
            }
        }

        private string GenerateTestToken(string username)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = "Iip7U9SQ3R8wZdAaicLRbrJKBeG8zgEYeX6wlfw8p7k="; // From appsettings.json
            var key = Encoding.UTF8.GetBytes(secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(JwtRegisteredClaimNames.Sub, username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = "ContractManagementBackend",
                Audience = "ContractManagementFrontend",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [Fact]
        public async Task Get_Projects_Endpoint_Should_Return_Success_When_Authenticated_As_Admin()
        {
            if (!_factory.IsDockerAvailable || _client == null)
            {
                // Docker daemon is not running on host machine; skip integration test requiring Testcontainers PostgreSQL
                return;
            }

            // Arrange
            var username = "integration_test_admin";
            var token = GenerateTestToken(username);

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (!dbContext.Users.Any(u => u.Username == username))
                {
                    dbContext.Users.Add(new User
                    {
                        Id = Guid.NewGuid(),
                        Username = username,
                        FullName = "Integration Test Admin",
                        IsActive = true,
                        IsSystemAdmin = true
                    });
                    await dbContext.SaveChangesAsync();
                }
            }

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/NghiepVu/du-an");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
