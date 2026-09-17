using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Cung cấp các Giá trị mặc định Nghiệp vụ (Business Defaults Metadata) dành cho Frontend.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/HeThong/business-defaults")]
public class SystemDefaultsController : ControllerBase
{
    /// <summary>
    /// Lấy danh sách các Giá trị Mặc định theo Quy tắc Nghiệp vụ của Hệ thống.
    /// </summary>
    /// <returns>Metadata giá trị mặc định của Hợp đồng, License, v.v.</returns>
    /// <response code="200">Lấy dữ liệu mặc định thành công</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetBusinessDefaults()
    {
        return Ok(new
        {
            License = new
            {
                DefaultWarningDays = 30,
                DefaultType = 1,
                DefaultStatus = 1
            },
            Contract = new
            {
                DefaultType = 1,
                DefaultPaymentMethod = 1,
                DefaultRenewalRequired = true,
                DefaultStatus = "Đang hiệu lực"
            }
        });
    }
}
