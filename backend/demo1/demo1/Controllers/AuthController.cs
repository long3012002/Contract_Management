using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using demo1.DTOs;
using demo1.Services;

namespace demo1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly RadiusClient _radiusClient;

        public AuthController(RadiusClient radiusClient)
        {
            _radiusClient = radiusClient;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { Message = "Username and password are required." });
            }

            bool isAuthenticated = await _radiusClient.AuthenticateAsync(request.Username, request.Password);

            if (isAuthenticated)
            {
                return Ok(new { Message = "Đăng nhập thành công" });
            }
            else
            {
                return Unauthorized(new { Message = "Sai tài khoản hoặc mật khẩu" });
            }
        }
        [HttpPost("test")]
        public async Task<IActionResult> TestLogin()
        {
            bool result = await _radiusClient.AuthenticateAsync("quangmd", "XianWang072026");

            if (result)
            {
                return Ok(new { Message = "Đăng nhập thành công" });
            }
            else
            {
                return Unauthorized(new { Message = "Sai tài khoản hoặc mật khẩu" });
            }
        }
    }
}
