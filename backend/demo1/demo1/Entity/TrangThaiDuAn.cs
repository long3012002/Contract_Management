namespace demo1.Entity;

public enum TrangThaiDuAn
{
    TatCa = 0,             // Tất cả trạng thái
    BanNhap = 1,           // Bản nháp
    DaTrinh = 2,           // Đã trình
    DaDuyet = 3,           // Đã duyệt
    DangTrienKhai = 4,     // Đang triển khai
    NghiemThu = 5,         // Nghiệm thu
    ThanhToan = 6,         // Thanh toán
    QuyetToan = 7,         // Quyết toán
    HoanThanh = 8          // Hoàn thành
}

public static class TrangThaiDuAnExtensions
{
    public static string GetDisplayName(this TrangThaiDuAn trangThai)
    {
        return trangThai switch
        {
            TrangThaiDuAn.TatCa => "Tất cả trạng thái",
            TrangThaiDuAn.BanNhap => "Bản nháp",
            TrangThaiDuAn.DaTrinh => "Đã trình",
            TrangThaiDuAn.DaDuyet => "Đã duyệt",
            TrangThaiDuAn.DangTrienKhai => "Đang triển khai",
            TrangThaiDuAn.NghiemThu => "Nghiệm thu",
            TrangThaiDuAn.ThanhToan => "Thanh toán",
            TrangThaiDuAn.QuyetToan => "Quyết toán",
            TrangThaiDuAn.HoanThanh => "Hoàn thành",
            _ => "Không xác định"
        };
    }
}
