namespace demo1.Entity;

public enum TrangThaiDuAn
{
    TatCa = 0,             // Tất cả trạng thái
    DangTrienKhai = 1,     // Đang triển khai
    HoanThanh = 2          // Đã hoàn thành
}

public static class TrangThaiDuAnExtensions
{
    public static string GetDisplayName(this TrangThaiDuAn trangThai)
    {
        return trangThai switch
        {
            TrangThaiDuAn.TatCa => "Tất cả trạng thái",
            TrangThaiDuAn.DangTrienKhai => "Đang triển khai",
            TrangThaiDuAn.HoanThanh => "Đã hoàn thành",
            _ => "Không xác định"
        };
    }
}

