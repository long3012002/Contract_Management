using System;
using System.Collections.Generic;

namespace demo1.DTOs;

public class LicenseSlaSummaryDto
{
    public int TongSoLicense { get; set; }
    public int SoLicenseDaHetHan { get; set; }
    public int SoLicenseSapHetHan30Ngay { get; set; }
    public int SoLicenseSapHetHan90Ngay { get; set; }
    public int SoLicenseAnToan { get; set; }
    public decimal TongChiPhiGiaHanDuKien { get; set; }
}

public class LicenseSlaItemDto
{
    public Guid LicenseId { get; set; }
    public string MaLicense { get; set; } = string.Empty;
    public string TenHeThong { get; set; } = string.Empty;
    public string TenHangSanXuat { get; set; } = string.Empty;
    public string TenNhaCungCap { get; set; } = string.Empty;
    public int LoaiLicense { get; set; }
    public string TenLoaiLicense { get; set; } = string.Empty;
    public int? SoLuong { get; set; }
    public string ThongTinThietBi { get; set; } = string.Empty;
    public DateTime? NgayBatDau { get; set; }
    public string ThoiHan { get; set; } = string.Empty;
    public DateTime? NgayKetThuc { get; set; }
    public int CanhBaoTruocNgay { get; set; }
    public int SoNgayConLai { get; set; }
    public int MaTrangThai { get; set; }
    public string TenTrangThai { get; set; } = string.Empty;
    public string TagCanhBaoRuiRo { get; set; } = string.Empty; // 🔴 ĐÃ HẾT HẠN, 🟡 SẮP HẾT HẠN, 🟢 AN TOÀN
    public string DeXuatHanhDong { get; set; } = string.Empty;
    public string GhiChu { get; set; } = string.Empty;
    
    // Contract & Project info
    public Guid? HopDongId { get; set; }
    public string TenHopDong { get; set; } = string.Empty;
    public decimal GiaTriHopDong { get; set; }
    public Guid? DuAnId { get; set; }
    public string TenDuAn { get; set; } = string.Empty;
}

public class LicenseSlaReportResponseDto
{
    public string TieuDe { get; set; } = "BÁO CÁO QUẢN LÝ HẠN LICENSE / BẢO TRÌ & SLA NHÀ THẦU";
    public string DonViTinh { get; set; } = "Đồng";
    public DateTime NgayTaoBaoCao { get; set; } = DateTime.UtcNow;
    public LicenseSlaSummaryDto TongHop { get; set; } = new();
    public List<LicenseSlaItemDto> DanhSachChiTiet { get; set; } = new();
}
