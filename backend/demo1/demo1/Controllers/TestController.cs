using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Message = "API is running.",
            Time = DateTime.UtcNow,
            Status = "Success"
        });
    }
}
