namespace demo1.Entity.DanhMuc;

public enum LoaiLicense
{
    CoThoiHan = 1,              // Có thời hạn (Subscription/Term)
    VinhVien = 2,               // Vĩnh viễn (Perpetual)
    TheoThietBiVatLy = 3,       // Theo thiết bị vật lý (Hardware-based)
    TheoSoLuongNguoiDung = 4    // Theo số lượng người dùng (Per user)
}

public static class LoaiLicenseExtensions
{
    public static string GetDisplayName(this LoaiLicense loai)
    {
        return loai switch
        {
            LoaiLicense.CoThoiHan => "Có thời hạn (Subscription/Term)",
            LoaiLicense.VinhVien => "Vĩnh viễn (Perpetual)",
            LoaiLicense.TheoThietBiVatLy => "Theo thiết bị vật lý (Hardware-based)",
            LoaiLicense.TheoSoLuongNguoiDung => "Theo số lượng người dùng (Per user)",
            _ => "Khác"
        };
    }
}
