using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Danh mục Nhóm Dự án (vd: Nhóm A, Nhóm B, Nhóm C theo quy định đầu tư công).
/// </summary>
[Route("api/DanhMuc/nhom-du-an")]
[FeatureAuthorize("PROJECT")]
public class NhomDuAnsController : CrudControllerBase<NhomDuAnDto, CreateNhomDuAnDto, UpdateNhomDuAnDto>
{
    public NhomDuAnsController(INhomDuAnService service) : base(service)
    {
    }
}
