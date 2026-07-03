using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Message = "API hoạt động bình thường!",
            Time = DateTime.UtcNow,
            Status = "Success"
        });
    }
}
