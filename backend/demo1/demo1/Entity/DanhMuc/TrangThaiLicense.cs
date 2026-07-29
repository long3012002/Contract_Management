namespace demo1.Entity.DanhMuc;

public enum TrangThaiLicense
{
    ConHieuLuc = 1,             // Còn hiệu lực
    SapHetHan = 2,              // Sắp hết hạn
    DaHetHan = 3,               // Đã hết hạn
    DaHuyHoacTamNgung = 4        // Đã hủy / Tạm ngưng
}

public static class TrangThaiLicenseExtensions
{
    public static string GetDisplayName(this TrangThaiLicense trangThai)
    {
        return trangThai switch
        {
            TrangThaiLicense.ConHieuLuc => "Còn hiệu lực",
            TrangThaiLicense.SapHetHan => "Sắp hết hạn",
            TrangThaiLicense.DaHetHan => "Đã hết hạn",
            TrangThaiLicense.DaHuyHoacTamNgung => "Đã hủy / Tạm ngưng",
            _ => "Không xác định"
        };
    }
}
