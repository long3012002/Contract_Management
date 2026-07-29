using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Route("api/goi-thau")]
[FeatureAuthorize("BID_PACKAGE")] // Keep BID_PACKAGE feature code for authorization purposes
public class GoiThausController : CrudControllerBase<GoiThauDto, CreateGoiThauDto, UpdateGoiThauDto>
{
    private readonly IGoiThauService _goiThauService;

    public GoiThausController(IGoiThauService service) : base(service)
    {
        _goiThauService = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<GoiThauDto>>> GetAll([FromQuery] GoiThauFilterDto filter)
    {
        var result = await _goiThauService.GetAllAsync(filter);
        return Ok(result);
    }
}
