using System;
using System.Collections.Generic;

namespace demo1.Entity;

public enum LoaiPhuLuc
{
    DieuChinhGiaTri = 1,     // Điều chỉnh giá trị hợp đồng (+/-)
    GiaHanThoiGian = 2,      // Gia hạn / điều chỉnh thời gian thực hiện
    BoSungHangHoaDichVu = 3, // Bổ sung / thay đổi danh mục hàng hóa, dịch vụ
    ThayDoiDieuKhoan = 4     // Thay đổi điều khoản hành chính / thông tin khác
}

public enum TrangThaiPhuLuc
{
    Active = 1,              // Hiệu lực
    Expired = 2,             // Hết hiệu lực
    Cancelled = 3            // Hủy
}

public class PhuLucHopDong : BaseEntity
{
    public Guid HopDongId { get; set; }
    public virtual HopDong HopDong { get; set; } = null!;

    public string SoPhuLuc { get; set; } = string.Empty;
    public string TenPhuLuc { get; set; } = string.Empty;
    public LoaiPhuLuc LoaiPhuLuc { get; set; }
    public TrangThaiPhuLuc TrangThai { get; set; } = TrangThaiPhuLuc.Active;

    public DateTime NgayKy { get; set; }
    public DateTime NgayHieuLuc { get; set; }

    public decimal GiaTriDieuChinh { get; set; } = 0;
    public DateTime? ExpiredDateMoi { get; set; }
    public string? ThoiHanThucHienMoi { get; set; }

    public string? NoiDungDieuChinh { get; set; }
    public string? GhiChu { get; set; }

    public virtual ICollection<DotThanhToan> DotThanhToans { get; set; } = new List<DotThanhToan>();
    public virtual ICollection<HangHoaDichVu> HangHoaDichVus { get; set; } = new List<HangHoaDichVu>();
}
