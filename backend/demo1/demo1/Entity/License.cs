using System;

namespace demo1.Entity;

public class License : BaseEntity
{
    public Guid DuAnId { get; set; }
    public virtual DuAn? DuAn { get; set; }

    public Guid? HopDongId { get; set; }
    public virtual HopDong? HopDong { get; set; }

    public Guid? NhaCungCapId { get; set; }
    public virtual DoiTac? NhaCungCap { get; set; }

    // 1 = Co thoi han (Term-based), 2 = Vinh vien (Perpetual), 3 = Theo thiet bi vat ly (Hardware-based), 4 = Theo so luong nguoi dung (Per user)
    public int LoaiLicense { get; set; } = 1;

    public int? SoLuong { get; set; }
    
    // Physical hardware specs: Serial Number, MAC Address, IP, Hostname...
    public string? ThongTinThietBi { get; set; }

    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }

    // Alert threshold in days before expiration (default 30 days)
    public int CanhBaoTruocNgay { get; set; } = 30;

    // 1 = Active (Con hieu luc), 2 = ExpiringSoon (Sap het han), 3 = Expired (Da het han), 4 = Terminated (Da huy/Ngung su dung)
    public int TrangThai { get; set; } = 1;

    public string? GhiChu { get; set; }
}
