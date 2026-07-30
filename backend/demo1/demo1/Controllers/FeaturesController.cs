using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using demo1.DTOs;
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
