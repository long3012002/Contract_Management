using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using demo1.DTOs;
using demo1.Services.Interfaces;

namespace demo1.Controllers
{
    /// <summary>
    /// API riêng biệt tiếp nhận và xử lý xác thực SSO (Single Sign-On / OIDC OAuth2 PKCE).
    /// </summary>
    [ApiController]
    [Route("api/sso")]
    public class SsoController(ISsoService ssoService) : ControllerBase
    {
        /// <summary>
        /// Tiếp nhận Authorization Code và PKCE code_verifier từ Frontend sau khi đăng nhập SSO thành công.
        /// </summary>
        /// <param name="request">Thông tin Authorization Code, Code Verifier và Redirect URI</param>
        /// <returns>Thông tin người dùng, Access Token, Refresh Token và phân quyền</returns>
        /// <response code="200">Đăng nhập SSO thành công</response>
        /// <response code="400">Mã Authorization Code hoặc Code Verifier không hợp lệ</response>
        /// <response code="403">Tài khoản SSO chưa được cấp quyền sử dụng hệ thống hoặc đang bị khóa</response>
        [EnableRateLimiting("LoginPolicy")]
        [HttpPost("callback")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Callback([FromBody] SsoCallbackRequest request)
        {
            var result = await ssoService.HandleCallbackAsync(request);
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
                503 => StatusCode(StatusCodes.Status503ServiceUnavailable, new { Message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { Message = result.ErrorMessage })
            };
        }
    }
}
