using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniExcelLibs;
using demo1.DTOs;
using demo1.Services.Interfaces;

namespace demo1.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController(IUserService userService) : ControllerBase
    {
        // 1. Hàm Thêm/Cập nhật nhiều (Bulk Upsert Users)
        [HttpPost("bulk-create")]
        public async Task<IActionResult> AddMultiple([FromBody] List<CreateUserDto> dtos)
        {
            if (dtos == null || !dtos.Any())
            {
                return BadRequest(new { Message = "Danh sách người dùng không được để trống." });
            }

            var result = await userService.ImportUsersAsync(dtos);
            if (result.ErrorCount > 0)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        // 1b. Hàm Import từ Excel
        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "File không hợp lệ hoặc trống." });
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
            {
                return BadRequest(new { Message = "Chỉ hỗ trợ file Excel (.xlsx, .xls)." });
            }

            try
            {
                List<CreateUserDto> dtos = new();
                using (var stream = file.OpenReadStream())
                {
                    var rows = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>();
                    foreach (var row in rows)
                    {
                        var rowDict = new Dictionary<string, object>(row, StringComparer.OrdinalIgnoreCase);

                        var dto = new CreateUserDto
                        {
                            Username = rowDict.ContainsKey("Username") ? rowDict["Username"]?.ToString() ?? string.Empty : string.Empty,
                            FullName = rowDict.ContainsKey("Họ tên") ? rowDict["Họ tên"]?.ToString() ?? string.Empty : (rowDict.ContainsKey("FullName") ? rowDict["FullName"]?.ToString() ?? string.Empty : string.Empty),
                            Email = rowDict.ContainsKey("Email") ? rowDict["Email"]?.ToString() : null,
                            Phone = rowDict.ContainsKey("Số điện thoại") ? rowDict["Số điện thoại"]?.ToString() : (rowDict.ContainsKey("Phone") ? rowDict["Phone"]?.ToString() : null),
                            TenPhongBan = rowDict.ContainsKey("Phòng ban") ? rowDict["Phòng ban"]?.ToString() : (rowDict.ContainsKey("TenPhongBan") ? rowDict["TenPhongBan"]?.ToString() : null),
                            TenChucVu = rowDict.ContainsKey("Chức vụ") ? rowDict["Chức vụ"]?.ToString() : (rowDict.ContainsKey("TenChucVu") ? rowDict["TenChucVu"]?.ToString() : null),
                            Role = rowDict.ContainsKey("Role") ? rowDict["Role"]?.ToString() : (rowDict.ContainsKey("Vai trò") ? rowDict["Vai trò"]?.ToString() : null),
                            IsActive = true,
                            IsSystemAdmin = false
                        };

                        if (rowDict.ContainsKey("Trạng thái"))
                        {
                            var activeStr = rowDict["Trạng thái"]?.ToString()?.Trim()?.ToLower();
                            if (activeStr == "khóa" || activeStr == "khoa" || activeStr == "0" || activeStr == "false")
                            {
                                dto.IsActive = false;
                            }
                        }

                        if (rowDict.ContainsKey("Admin"))
                        {
                            var adminStr = rowDict["Admin"]?.ToString()?.Trim()?.ToLower();
                            if (adminStr == "có" || adminStr == "co" || adminStr == "1" || adminStr == "true")
                            {
                                dto.IsSystemAdmin = true;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(dto.Username))
                        {
                            dtos.Add(dto);
                        }
                    }
                }

                if (!dtos.Any())
                {
                    return BadRequest(new { Message = "Không tìm thấy dữ liệu người dùng nào hợp lệ trong file Excel." });
                }

                var result = await userService.ImportUsersAsync(dtos);
                if (result.ErrorCount > 0)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Đã xảy ra lỗi khi đọc file Excel.", Detail = ex.Message });
            }
        }

        // 2. Hàm Xoá nhiều (Bulk Delete Users)
        [HttpDelete("bulk-delete")]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<Guid> ids)
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest(new { Message = "Danh sách ID cần xoá không được để trống." });
            }

            var result = await userService.DeleteUsersAsync(ids);
            if (result.DeletedCount == 0)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
    }
}
