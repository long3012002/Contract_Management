using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Quyết định / Nghị quyết (Thông tin quyết định phê duyệt dự án, gói thầu, hợp đồng).
/// </summary>
[Route("api/NghiepVu/resolutions")]
[FeatureAuthorize("NGHI_QUYET")]
public class ResolutionsController : CrudControllerBase<ResolutionDto, CreateResolutionDto, UpdateResolutionDto>
{
    public ResolutionsController(IResolutionService service) : base(service)
    {
    }
}
