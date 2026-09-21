namespace demo1.Entity;

public enum TrangThaiDuAn
{
    TatCa = 0,             // Tất cả trạng thái
    Draft = 1,             // Bản nháp
    Submitted = 2,         // Đã trình
    Approved = 3,          // Đã duyệt
    Implementing = 4,      // Đang triển khai
    Acceptance = 5,        // Nghiệm thu
    Payment = 6,           // Thanh toán
    Settlement = 7,        // Quyết toán
    Completed = 8,         // Hoàn thành
    Suspended = 9,         // Tạm dừng
    Merged = 10,           // Đã gộp

    // Aliases hỗ trợ tương thích ngược
    DangTrienKhai = 4,
    HoanThanh = 8
}

public static class TrangThaiDuAnExtensions
{
    public static string GetDisplayName(this TrangThaiDuAn trangThai)
    {
        return trangThai switch
        {
            TrangThaiDuAn.TatCa => "Tất cả trạng thái",
            TrangThaiDuAn.Draft => "Bản nháp",
            TrangThaiDuAn.Submitted => "Đã trình",
            TrangThaiDuAn.Approved => "Đã duyệt",
            TrangThaiDuAn.Implementing => "Đang triển khai",
            TrangThaiDuAn.Acceptance => "Nghiệm thu",
            TrangThaiDuAn.Payment => "Thanh toán",
            TrangThaiDuAn.Settlement => "Quyết toán",
            TrangThaiDuAn.Completed => "Hoàn thành",
            TrangThaiDuAn.Suspended => "Tạm dừng",
            TrangThaiDuAn.Merged => "Đã gộp",
            _ => "Không xác định"
        };
    }
}

