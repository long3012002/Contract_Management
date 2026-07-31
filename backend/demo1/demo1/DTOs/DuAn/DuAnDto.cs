using System;
using System.Collections.Generic;
using System.Linq;

namespace demo1.DTOs;

/// <summary>
/// DTO Thông tin chi tiết của Dự án.
/// </summary>
public class DuAnDto : IHasId
{
    /// <summary>
    /// Mã định danh duy nhất của Dự án (GUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Mã số dự án
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Tên dự án
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết dự án
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Dự toán phê duyệt ban đầu (VNĐ)
    /// </summary>
    public decimal DuToanPheDuyet { get; set; }
    
    /// <summary>
    /// Tổng dự toán hiện tại sau khi đã cộng/trừ các đợt điều chỉnh (VNĐ)
    /// </summary>
    public decimal TongDuToanHienTai { get; set; }
    
    /// <summary>
    /// Trạng thái thực hiện dự án (0: Khởi tạo, 1: Đang thực hiện, 2: Hoàn thành, 3: Đã đóng)
    /// </summary>
    public int TrangThai { get; set; }

    /// <summary>
    /// Phân loại/Loại hình dự án
    /// </summary>
    public int LoaiDuAn { get; set; }

    /// <summary>
    /// ID Nhóm dự án thuộc danh mục
    /// </summary>
    public Guid? NhomDuAnId { get; set; }

    /// <summary>
    /// Tên Nhóm dự án
    /// </summary>
    public string? NhomDuAnName { get; set; }

    /// <summary>
    /// ID Phân loại dự án thuộc danh mục
    /// </summary>
    public Guid? PhanLoaiDuAnId { get; set; }

    /// <summary>
    /// Tên Phân loại dự án
    /// </summary>
    public string? PhanLoaiDuAnName { get; set; }

    /// <summary>
    /// Chuỗi ID Nguồn vốn dự án (phân tách bởi dấu chấm phẩy)
    /// </summary>
    public string? NguonDuAnIds { get; set; }
    
    /// <summary>
    /// Danh sách GUID các nguồn vốn dự án
    /// </summary>
    public List<Guid> ListNguonDuAnIds
    {
        get
        {
            if (string.IsNullOrWhiteSpace(NguonDuAnIds))
                return new List<Guid>();
            return NguonDuAnIds.Split(';', StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                               .Where(g => g != Guid.Empty)
                               .ToList();
        }
    }
    
    /// <summary>
    /// Danh sách chi tiết các dự án nguồn liên kết
    /// </summary>
    public List<DuAnDto>? SourceProjects { get; set; }

    /// <summary>
    /// Tên Chủ đầu tư
    /// </summary>
    public string? ChuDauTu { get; set; }

    /// <summary>
    /// Địa điểm thực hiện dự án
    /// </summary>
    public string? DiaDiemThucHien { get; set; }

    /// <summary>
    /// Thời gian thực hiện dự án
    /// </summary>
    public string? ThoiGianThucHien { get; set; }
    
    /// <summary>
    /// Nội dung chi tiết công việc
    /// </summary>
    public string? NoiDung { get; set; }

    /// <summary>
    /// Hình thức quản lý dự án
    /// </summary>
    public int? HinhThucQuanLy { get; set; }

    /// <summary>
    /// Đơn vị/Tổ chức thực hiện
    /// </summary>
    public string? ToChucThucHien { get; set; }
    
    /// <summary>
    /// Ngày bắt đầu dự án
    /// </summary>
    public DateTime? NgayBatDau { get; set; }

    /// <summary>
    /// Ngày kết thúc dự án
    /// </summary>
    public DateTime? NgayKetThuc { get; set; }

    /// <summary>
    /// Năm bắt đầu
    /// </summary>
    public int? NamBatDau { get; set; }

    /// <summary>
    /// Năm kết thúc
    /// </summary>
    public int? NamKetThuc { get; set; }

    /// <summary>
    /// Đã kết thúc dự án hay chưa
    /// </summary>
    public bool DaKetThuc { get; set; }

    /// <summary>
    /// Đã triển khai dự án hay chưa
    /// </summary>
    public bool? DaTrienKhai { get; set; }

    /// <summary>
    /// Số quyết định phê duyệt dự án
    /// </summary>
    public string? SoQuyetDinh { get; set; }
    
    /// <summary>
    /// Trạng thái hoạt động (true: Đang hoạt động, false: Đã xóa/khóa)
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thời điểm tạo bản ghi
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm cập nhật gần nhất
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
