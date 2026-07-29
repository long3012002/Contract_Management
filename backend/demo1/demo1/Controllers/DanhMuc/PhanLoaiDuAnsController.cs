using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Danh mục Phân loại Dự án (vd: Dự án CNTT, Dự án Xây dựng, Mua sắm hàng hóa...).
/// </summary>
[Route("api/DanhMuc/phan-loai-du-an")]
[FeatureAuthorize("PROJECT")]
public class PhanLoaiDuAnsController : CrudControllerBase<PhanLoaiDuAnDto, CreatePhanLoaiDuAnDto, UpdatePhanLoaiDuAnDto>
{
    public PhanLoaiDuAnsController(IPhanLoaiDuAnService service) : base(service)
    {
    }
}
