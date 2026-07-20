using System;

namespace demo1.DTOs;

public class LicenseDto : IHasId
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Guid DuAnId { get; set; }
    public string? DuAnName { get; set; }
    public string? DuAnCode { get; set; }

    public Guid? HopDongId { get; set; }
    public string? HopDongName { get; set; }
    public string? HopDongCode { get; set; }

    public Guid? NhaCungCapId { get; set; }
    public string? NhaCungCapName { get; set; }

    // 1 = Co thoi han, 2 = Vinh vien, 3 = Theo thiet bi vat ly, 4 = Theo so luong nguoi dung
    public int LoaiLicense { get; set; } = 1;
    public string LoaiLicenseText => LoaiLicense switch
    {
        1 => "Có thời hạn (Subscription/Term)",
        2 => "Vĩnh viễn (Perpetual)",
        3 => "Theo thiết bị vật lý (Hardware-based)",
        4 => "Theo số lượng người dùng (Per user)",
        _ => "Khác"
    };

    public int? SoLuong { get; set; }
    public string? ThongTinThietBi { get; set; }

    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }

    public int CanhBaoTruocNgay { get; set; } = 30;

    // 1 = Active, 2 = ExpiringSoon, 3 = Expired, 4 = Terminated
    public int TrangThai { get; set; } = 1;
    public string TrangThaiText => TrangThai switch
    {
        1 => "Còn hiệu lực",
        2 => "Sắp hết hạn",
        3 => "Đã hết hạn",
        4 => "Đã hủy / Tạm ngưng",
        _ => "Không xác định"
    };

    public int? DaysRemaining
    {
        get
        {
            if (LoaiLicense == 2 || !NgayKetThuc.HasValue) return null; // Perpetual license
            return (NgayKetThuc.Value.Date - DateTime.Today).Days;
        }
    }

    public string? GhiChu { get; set; }
}

public class CreateLicenseDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid DuAnId { get; set; }
    public Guid? HopDongId { get; set; }
    public Guid? NhaCungCapId { get; set; }

    public int LoaiLicense { get; set; } = 1;
    public int? SoLuong { get; set; }
    public string? ThongTinThietBi { get; set; }

    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }

    public int CanhBaoTruocNgay { get; set; } = 30;
    public string? GhiChu { get; set; }
}

public class UpdateLicenseDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid DuAnId { get; set; }
    public Guid? HopDongId { get; set; }
    public Guid? NhaCungCapId { get; set; }

    public int LoaiLicense { get; set; } = 1;
    public int? SoLuong { get; set; }
    public string? ThongTinThietBi { get; set; }

    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }

    public int CanhBaoTruocNgay { get; set; } = 30;
    public int TrangThai { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public string? GhiChu { get; set; }
}

public class LicenseSummaryDto
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int ExpiredCount { get; set; }
    public int TerminatedCount { get; set; }

    public int PerpetualCount { get; set; }
    public int TermBasedCount { get; set; }
    public int HardwareBasedCount { get; set; }
    public int PerUserCount { get; set; }
}
