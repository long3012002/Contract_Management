using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[ApiController]
[Route("api/warnings")]
public class WarningsController : ControllerBase
{
    private readonly IWarningService _service;

    public WarningsController(IWarningService service)
    {
        _service = service;
    }

    [HttpGet("contracts-expiring-soon")]
    public async Task<IActionResult> GetContractsExpiringSoon()
    {
        var result = await _service.GetContractsExpiringSoonAsync();
        return Ok(result);
    }

    [HttpGet("expired-contracts")]
    public async Task<IActionResult> GetExpiredContracts()
    {
        var result = await _service.GetExpiredContractsAsync();
        return Ok(result);
    }

    [HttpGet("over-budget-contracts")]
    public async Task<IActionResult> GetOverBudgetContracts()
    {
        var result = await _service.GetOverBudgetContractsAsync();
        return Ok(result);
    }
}
