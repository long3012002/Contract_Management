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
        private readonly HttpClient _client;

        public DuAnControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
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
            // Arrange
            var username = "integration_test_admin";
            
            // Seed the test admin in the database
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Add user if not already present
                var existingUser = await db.Users.FindAsync(Guid.Empty); // Check if dummy exists or query by username
                if (!await db.Users.AnyAsync(u => u.Username == username))
                {
                    db.Users.Add(new User
                    {
                        Id = Guid.NewGuid(),
                        Username = username,
                        FullName = "Integration Admin",
                        IsActive = true,
                        IsSystemAdmin = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                }
            }

            // Generate JWT and configure HTTP headers
            var token = GenerateTestToken(username);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/NghiepVu/du-an?page=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<PagedResult<DuAnDto>>();
            result.Should().NotBeNull();
            result!.Items.Should().NotBeNull();
        }
    }
}
