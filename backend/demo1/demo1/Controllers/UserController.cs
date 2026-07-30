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
    /// <summary>
    /// API Quản lý Người dùng (Danh sách Người dùng, Import Excel, Tải mẫu Import, Cập nhật và Xóa hàng loạt).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/HeThong/user")]
    public class UserController(IUserService userService, IAdminService adminService) : ControllerBase
    {
        /// <summary>
        /// Lấy danh sách người dùng kèm vai trò (Phân trang, Tìm kiếm, Lọc theo Phòng ban/Đơn vị).
        /// </summary>
        /// <param name="filter">Bộ lọc danh sách người dùng</param>
        /// <returns>Danh sách người dùng phân trang</returns>
        /// <response code="200">Lấy danh sách người dùng thành công</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<UserWithRolesDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers([FromQuery] UserFilterDto filter)
        {
            var result = await adminService.GetUsersWithRolesAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Thêm mới hoặc cập nhật hàng loạt người dùng từ danh sách DTO.
        /// </summary>
        /// <param name="dtos">Danh sách dữ liệu người dùng</param>
        /// <response code="200">Import/Tạo mới thành công</response>
        /// <response code="400">Dữ liệu đầu vào hoặc lỗi danh sách</response>
        [HttpPost("bulk-create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Import danh sách người dùng từ file Excel (.xlsx, .xls).
        /// </summary>
        /// <param name="file">File Excel chứa thông tin người dùng</param>
        /// <response code="200">Import file Excel thành công</response>
        /// <response code="400">File không đúng định dạng hoặc dữ liệu không hợp lệ</response>
        [HttpPost("import-excel")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
                            TenDonVi = rowDict.ContainsKey("Đơn vị") ? rowDict["Đơn vị"]?.ToString() : (rowDict.ContainsKey("DonVi") ? rowDict["DonVi"]?.ToString() : (rowDict.ContainsKey("TenDonVi") ? rowDict["TenDonVi"]?.ToString() : null)),
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

        /// <summary>
        /// Tải xuống file mẫu Excel để Import danh sách Người dùng.
        /// </summary>
        /// <returns>File Excel (.xlsx)</returns>
        /// <response code="200">Tải file mẫu thành công</response>
        [HttpGet("import-template")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult DownloadImportTemplate()
        {
            var templateData = new[]
            {
                new
                {
                    Username = "nguyenvana",
                    HoTen = "Nguyễn Văn A",
                    Email = "nguyenvana@co-opbank.vn",
                    SoDienThoai = "0912345678",
                    PhongBan = "Phòng Công nghệ thông tin",
                    ChucVu = "Chuyên viên",
                    DonVi = "Hội sở chính",
                    Role = "NhanVien",
                    TrangThai = "Hoạt động",
                    Admin = "Không"
                },
                new
                {
                    Username = "tranvanb",
                    HoTen = "Trần Văn B",
                    Email = "tranvanb@co-opbank.vn",
                    SoDienThoai = "0987654321",
                    PhongBan = "Phòng Kế toán",
                    ChucVu = "Trưởng phòng",
                    DonVi = "Hội sở chính",
                    Role = "QuanLy",
                    TrangThai = "Hoạt động",
                    Admin = "Có"
                }
            };

            var memoryStream = new MemoryStream();
            memoryStream.SaveAs(templateData);
            memoryStream.Position = 0;

            return File(
                memoryStream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Mau_Import_Nguoi_Dung.xlsx"
            );
        }

        /// <summary>
        /// Cập nhật thông tin chi tiết một người dùng theo ID.
        /// </summary>
        /// <param name="id">Mã định danh Người dùng (GUID)</param>
        /// <param name="dto">Thông tin cập nhật</param>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="404">Không tìm thấy người dùng</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { Message = "Dữ liệu cập nhật không được để trống." });
            }

            try
            {
                var result = await userService.UpdateUserAsync(id, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Đã xảy ra lỗi khi cập nhật người dùng.", Detail = ex.Message });
            }
        }

        /// <summary>
        /// Xóa hàng loạt người dùng theo danh sách ID (GUID).
        /// </summary>
        /// <param name="ids">Danh sách GUID người dùng cần xóa</param>
        /// <response code="200">Xóa thành công</response>
        /// <response code="404">Không tìm thấy người dùng phù hợp để xóa</response>
        [HttpDelete("bulk-delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
