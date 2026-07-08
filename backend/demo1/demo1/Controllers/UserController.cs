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
                        var dto = new CreateUserDto
                        {
                            Username = row.ContainsKey("Username") ? row["Username"]?.ToString() ?? string.Empty : string.Empty,
                            FullName = row.ContainsKey("Họ tên") ? row["Họ tên"]?.ToString() ?? string.Empty : (row.ContainsKey("FullName") ? row["FullName"]?.ToString() ?? string.Empty : string.Empty),
                            Email = row.ContainsKey("Email") ? row["Email"]?.ToString() : null,
                            Phone = row.ContainsKey("Số điện thoại") ? row["Số điện thoại"]?.ToString() : (row.ContainsKey("Phone") ? row["Phone"]?.ToString() : null),
                            TenPhongBan = row.ContainsKey("Phòng ban") ? row["Phòng ban"]?.ToString() : (row.ContainsKey("TenPhongBan") ? row["TenPhongBan"]?.ToString() : null),
                            TenChucVu = row.ContainsKey("Chức vụ") ? row["Chức vụ"]?.ToString() : (row.ContainsKey("TenChucVu") ? row["TenChucVu"]?.ToString() : null),
                            Role = row.ContainsKey("Role") ? row["Role"]?.ToString() : (row.ContainsKey("Vai trò") ? row["Vai trò"]?.ToString() : null),
                            IsActive = true,
                            IsSystemAdmin = false
                        };

                        if (row.ContainsKey("Trạng thái"))
                        {
                            var activeStr = row["Trạng thái"]?.ToString()?.Trim()?.ToLower();
                            if (activeStr == "khóa" || activeStr == "khoa" || activeStr == "0" || activeStr == "false")
                            {
                                dto.IsActive = false;
                            }
                        }

                        if (row.ContainsKey("Admin"))
                        {
                            var adminStr = row["Admin"]?.ToString()?.Trim()?.ToLower();
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
