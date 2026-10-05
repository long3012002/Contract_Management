using System;
using System.Collections.Generic;

namespace demo1.DTOs;

/// <summary>
/// Thông tin một Hợp đồng gọn nhẹ dùng trong cấu trúc phân cấp Dự án.
/// Chỉ bao gồm các trường cần thiết để hiển thị trên danh sách dự án.
/// </summary>
public class HopDongInHierarchyDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? SoHopDong { get; set; }
    public string? NhaThauName { get; set; }
    public string? TenLienDanhNhaThau { get; set; }
    public decimal GiaTriHopDong { get; set; }
    public decimal TongGiaTriHienTai { get; set; }
    public DateTime? NgayKy { get; set; }
    public string TrangThaiCalculatedText { get; set; } = "Đang hiệu lực";
    public string Status { get; set; } = "Active";
    public Guid? GoiThauId { get; set; }
    public Guid? DuAnId { get; set; }
}

/// <summary>
/// Thông tin một Gói thầu kèm danh sách Hợp đồng con — dùng trong cấu trúc phân cấp Dự án.
/// </summary>
public class GoiThauInHierarchyDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal GiaTriGoiThau { get; set; }
    public decimal TongGiaTriHopDong { get; set; }
    public string TrangThaiGoiThau { get; set; } = "Đang thực hiện";

    /// <summary>Danh sách hợp đồng thuộc gói thầu này.</summary>
    public List<HopDongInHierarchyDto> HopDongs { get; set; } = new();
}

/// <summary>
/// Response của endpoint GET /api/NghiepVu/du-an/{id}/hierarchy.
/// Trả về cấu trúc phân cấp: Gói thầu (kèm hợp đồng con) + Hợp đồng chưa gán gói thầu.
/// </summary>
public class DuAnHierarchyDto
{
    public Guid DuAnId { get; set; }

    /// <summary>Danh sách gói thầu (mỗi gói thầu kèm danh sách hợp đồng con).</summary>
    public List<GoiThauInHierarchyDto> GoiThaus { get; set; } = new();

    /// <summary>Hợp đồng không thuộc gói thầu nào (ký trực tiếp với dự án).</summary>
    public List<HopDongInHierarchyDto> UnassignedContracts { get; set; } = new();

    public int TotalGoiThaus => GoiThaus.Count;
    public int TotalHopDongs => GoiThaus.Sum(g => g.HopDongs.Count) + UnassignedContracts.Count;
}
