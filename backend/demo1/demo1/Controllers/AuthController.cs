using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using demo1.DTOs;
using demo1.Services.Interfaces;

namespace demo1.Controllers
{
    /// <summary>
    /// API Quản lý Xác thực &amp; Phân quyền người dùng (Đăng nhập, Refresh Token, Xác thực 2FA, Đăng xuất).
    /// </summary>
    [ApiController]
    [Route("api/HeThong/auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        /// <summary>
        /// Đăng nhập hệ thống bằng Tên đăng nhập và Mật khẩu.
        /// </summary>
        /// <param name="request">Thông tin tài khoản (Username, Password)</param>
        /// <returns>Access Token, Refresh Token, Thông tin User và cờ 2FA</returns>
        /// <response code="200">Đăng nhập thành công</response>
        /// <response code="400">Tên đăng nhập hoặc mật khẩu không chính xác</response>
        /// <response code="429">Quá số lần đăng nhập cho phép (Rate Limit)</response>
        [EnableRateLimiting("LoginPolicy")]
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await authService.LoginAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Làm mới Access Token bằng Refresh Token.
        /// </summary>
        /// <param name="request">Refresh Token hợp lệ</param>
        /// <returns>Access Token và Refresh Token mới</returns>
        /// <response code="200">Cấp lại token thành công</response>
        /// <response code="401">Refresh Token không hợp lệ hoặc đã hết hạn</response>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var token = request?.RefreshToken;
            var result = await authService.RefreshAsync(new RefreshRequest { RefreshToken = token ?? "" });
            return HandleResult(result);
        }

        /// <summary>
        /// Kích hoạt xác thực 2 yếu tố (2FA) cho tài khoản.
        /// </summary>
        /// <param name="request">Mã xác thực 2FA OTP</param>
        /// <returns>Trạng thái kích hoạt 2FA thành công</returns>
        /// <response code="200">Kích hoạt thành công</response>
        /// <response code="400">Mã OTP không hợp lệ</response>
        [HttpPost("enable-2fa")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Enable2Fa([FromBody] Verify2FARequest request)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var result = await authService.Enable2FaAsync(request, authHeader);
            return HandleResult(result);
        }

        /// <summary>
        /// Xác thực mã OTP 2FA để hoàn tất đăng nhập.
        /// </summary>
        /// <param name="request">Mã OTP 2FA từ ứng dụng Authenticator</param>
        /// <returns>Access Token hoàn chỉnh</returns>
        /// <response code="200">Xác thực thành công</response>
        /// <response code="400">Mã OTP không hợp lệ hoặc đã hết hạn</response>
        [HttpPost("verify-2fa")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Verify2Fa([FromBody] Verify2FARequest request)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var result = await authService.Verify2FaAsync(request, authHeader);
            return HandleResult(result);
        }

        /// <summary>
        /// Đăng xuất khỏi hệ thống và hủy Refresh Token.
        /// </summary>
        /// <response code="200">Đăng xuất thành công</response>
        /// <response code="401">Chưa xác thực</response>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
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
