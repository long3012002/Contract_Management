using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using demo1.DTOs;
using demo1.Services;

namespace demo1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly RadiusClient _radiusClient;
        private readonly IConfiguration _configuration;

        public AuthController(RadiusClient radiusClient, IConfiguration configuration)
        {
            _radiusClient = radiusClient;
            _configuration = configuration;
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { Message = "Username and password are required." });
            }

            bool isAuthenticated = await _radiusClient.AuthenticateAsync(request.Username, request.Password);

            if (isAuthenticated)
            {
                var accessToken = GenerateJwtToken(request.Username, 180);
                var refreshToken = GenerateJwtToken(request.Username, 10080);
                return Ok(new LoginResponse 
                { 
                    Message = "Đăng nhập thành công", 
                    AccessToken = accessToken, 
                    RefreshToken = refreshToken, 
                    Username = request.Username 
                });
            }
            else
            {
                return Unauthorized(new { Message = "Sai tài khoản hoặc mật khẩu" });
            }
        }

        //[HttpPost("test")]
        //[ProducesResponseType(typeof(LoginResponse), 200)]
        //public async Task<IActionResult> TestLogin()
        //{
        //    bool result = await _radiusClient.AuthenticateAsync("quangmd", "XianWang072026");

        //    if (result)
        //    {
        //        var accessToken = GenerateJwtToken("quangmd", 180);
        //        var refreshToken = GenerateJwtToken("quangmd", 10080);
        //        return Ok(new LoginResponse 
        //        { 
        //            Message = "Đăng nhập thành công", 
        //            AccessToken = accessToken, 
        //            RefreshToken = refreshToken, 
        //            Username = "quangmd" 
        //        });
        //    }
        //    else
        //    {
        //        return Unauthorized(new { Message = "Sai tài khoản hoặc mật khẩu" });
        //    }
        //}

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        public IActionResult Refresh([FromBody] RefreshRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new { Message = "Refresh token is required." });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "Iip7U9SQ3R8wZdAaicLRbrJKBeG8zgEYeX6wlfw8p7k=";
            var key = Encoding.UTF8.GetBytes(secretKey);

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"] ?? "ContractManagementBackend",
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"] ?? "ContractManagementFrontend",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(request.RefreshToken, validationParameters, out var validatedToken);
                var username = principal.Identity?.Name ?? principal.FindFirst(ClaimTypes.Name)?.Value ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrWhiteSpace(username))
                {
                    return Unauthorized(new { Message = "Invalid token payload." });
                }

                var newAccessToken = GenerateJwtToken(username, 180);
                var newRefreshToken = GenerateJwtToken(username, 10080);

                return Ok(new LoginResponse
                {
                    Message = "Làm mới token thành công",
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    Username = username
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Message = "Invalid or expired refresh token.", Error = ex.Message });
            }
        }

        private string GenerateJwtToken(string username, double expiryInMinutes)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "Iip7U9SQ3R8wZdAaicLRbrJKBeG8zgEYeX6wlfw8p7k=";
            var issuer = jwtSettings["Issuer"] ?? "ContractManagementBackend";
            var audience = jwtSettings["Audience"] ?? "ContractManagementFrontend";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, username)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiryInMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
