using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using demo1.DTOs;
using demo1.DTOs.Permission;
using demo1.Entity;
using demo1.Services.Interfaces;

namespace demo1.Controllers
{
    /// <summary>
    /// API Quản lý Tính năng (Features) ứng dụng.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/HeThong/admin/features")]
    public class FeaturesController(IAdminService adminService) : ControllerBase
    {
        private async Task<bool> IsAdminAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            return await adminService.IsSystemAdminAsync(username);
        }

        private async Task<bool> CanViewUserPermissionsAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            return await adminService.CanViewUserPermissionsAsync(username);
        }

        /// <summary>
        /// Lấy danh mục các mã tính năng (Feature Codes) đang hoạt động phục vụ phân quyền frontend.
        /// Hỗ trợ cả định dạng mảng phẳng (flat array) và cây phân cấp (tree) bằng tham số ?tree=true.
        /// </summary>
        /// <param name="tree">Bật true nếu muốn nhận trực tiếp danh sách dạng Cây phân cấp (nested children)</param>
        /// <returns>Danh sách tính năng chuẩn hóa</returns>
        /// <response code="200">Lấy danh sách mã tính năng thành công</response>
        [HttpGet("catalog")]
        [ProducesResponseType(typeof(IEnumerable<FeatureCatalogDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFeatureCatalog([FromQuery] bool tree = false)
        {
            var features = await adminService.GetFeaturesAsync();
            var activeFeatures = features
                .Where(f => f.IsActive)
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Name)
                .Select(f => new FeatureCatalogDto
                { 
                    FeatureId = f.Id,
                    FeatureCode = f.Code, 
                    FeatureName = f.Name,
                    Description = f.Description,
                    ParentCode = f.ParentCode,
                    SortOrder = f.SortOrder
                })
                .ToList();

            if (!tree)
            {
                return Ok(activeFeatures);
            }

            return Ok(BuildFeatureTree(activeFeatures));
        }

        /// <summary>
        /// API trả về trực tiếp danh mục tính năng dạng Cây (Hierarchical Tree Data) với các nút con `children: []`.
        /// </summary>
        [HttpGet("tree")]
        [ProducesResponseType(typeof(IEnumerable<FeatureCatalogDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFeatureTree()
        {
            var features = await adminService.GetFeaturesAsync();
            var activeFeatures = features
                .Where(f => f.IsActive)
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Name)
                .Select(f => new FeatureCatalogDto
                { 
                    FeatureId = f.Id,
                    FeatureCode = f.Code, 
                    FeatureName = f.Name,
                    Description = f.Description,
                    ParentCode = f.ParentCode,
                    SortOrder = f.SortOrder
                })
                .ToList();

            return Ok(BuildFeatureTree(activeFeatures));
        }

        private static List<FeatureCatalogDto> BuildFeatureTree(List<FeatureCatalogDto> flatFeatures)
        {
            var featureMap = flatFeatures.ToDictionary(f => f.FeatureCode, StringComparer.OrdinalIgnoreCase);
            var rootNodes = new List<FeatureCatalogDto>();

            foreach (var item in flatFeatures)
            {
                if (!string.IsNullOrEmpty(item.ParentCode) && featureMap.TryGetValue(item.ParentCode, out var parent))
                {
                    parent.Children.Add(item);
                }
                else
                {
                    rootNodes.Add(item);
                }
            }

            return rootNodes;
        }

        /// <summary>
        /// Lấy danh sách các Tính năng (Features) của ứng dụng.
        /// </summary>
        /// <returns>Danh sách tính năng</returns>
        /// <response code="200">Lấy danh sách tính năng thành công</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Feature>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFeatures()
        {
            if (!await CanViewUserPermissionsAsync()) return Forbid();
            var features = await adminService.GetFeaturesAsync();
            return Ok(features);
        }

        /// <summary>
        /// Tạo mới một Tính năng (Feature).
        /// </summary>
        /// <param name="dto">Thông tin tính năng mới</param>
        /// <returns>Tính năng vừa tạo</returns>
        /// <response code="200">Tạo tính năng thành công</response>
        [HttpPost]
        [ProducesResponseType(typeof(Feature), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateFeature([FromBody] CreateFeatureDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            var feature = await adminService.CreateFeatureAsync(dto);
            return Ok(feature);
        }

        /// <summary>
        /// Cập nhật thông tin Tính năng theo ID.
        /// </summary>
        /// <param name="featureId">Mã định danh Tính năng (GUID)</param>
        /// <param name="dto">Dữ liệu cập nhật</param>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="404">Không tìm thấy tính năng</response>
        [HttpPut("{featureId:guid}")]
        [ProducesResponseType(typeof(Feature), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateFeature(Guid featureId, [FromBody] UpdateFeatureDto dto)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                var feature = await adminService.UpdateFeatureAsync(featureId, dto);
                return Ok(feature);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa một Tính năng theo ID.
        /// </summary>
        /// <param name="featureId">Mã định danh Tính năng (GUID)</param>
        /// <response code="200">Xóa thành công</response>
        /// <response code="404">Không tìm thấy tính năng</response>
        [HttpDelete("{featureId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteFeature(Guid featureId)
        {
            if (!await IsAdminAsync()) return Forbid();
            try
            {
                await adminService.DeleteFeatureAsync(featureId);
                return Ok(new { Message = "Feature deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }
    }
}
