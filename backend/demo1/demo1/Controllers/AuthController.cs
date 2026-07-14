using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using demo1.DTOs;
using demo1.Services.Interfaces;

namespace demo1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [EnableRateLimiting("LoginPolicy")]
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await authService.LoginAsync(request);
            return HandleResult(result);
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var token = request?.RefreshToken;
            var result = await authService.RefreshAsync(new RefreshRequest { RefreshToken = token ?? "" });
            return HandleResult(result);
        }

        [HttpPost("enable-2fa")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        public async Task<IActionResult> Enable2Fa([FromBody] Verify2FARequest request)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var result = await authService.Enable2FaAsync(request, authHeader);
            return HandleResult(result);
        }

        [HttpPost("verify-2fa")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        public async Task<IActionResult> Verify2Fa([FromBody] Verify2FARequest request)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var result = await authService.Verify2FaAsync(request, authHeader);
            return HandleResult(result);
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var result = await authService.LogoutAsync(username);
            return HandleResult(result);
        }

        private IActionResult HandleResult(AuthResult result)
        {
            if (result.IsSuccess)
            {
                return Ok(result.Response);
            }

            return result.StatusCode switch
            {
                400 => BadRequest(new { Message = result.ErrorMessage }),
                401 => Unauthorized(new { Message = result.ErrorMessage }),
                403 => StatusCode(StatusCodes.Status403Forbidden, new { Message = result.ErrorMessage }),
                404 => NotFound(new { Message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { Message = result.ErrorMessage })
            };
        }
    }
}
