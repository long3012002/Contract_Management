using System;

namespace demo1.DTOs;

public class GoiThauDto : IHasId
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public Guid? DuAnId { get; set; }
    public string? DuAnName { get; set; } // Auxiliary for easier UI viewing
    
    public decimal GiaTriGoiThau { get; set; }
    public decimal TongGiaTriHopDong { get; set; }
    
    public string? SoQuyetDinhKHLCNT { get; set; }
    public DateTime? NgayPheDuyetKHLCNT { get; set; }
    public string? SoQuyetDinhKQLCNT { get; set; }
    public DateTime? NgayPheDuyetKQLCNT { get; set; }

    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Mã dự án triển khai (tương thích mẫu Báo cáo 3)</summary>
    public string? MaDuAn { get; set; }

    /// <summary>Hình thức lựa chọn nhà thầu (Đấu thầu rộng rãi qua mạng, Chỉ định thầu...)</summary>
    public string? HinhThucLcnt { get; set; }

    /// <summary>Phương thức lựa chọn nhà thầu (1 GĐ 1 THS, 1 GĐ 2 THS...)</summary>
    public string? PhuongThucLcnt { get; set; }

    /// <summary>Tên nhà thầu trúng thầu / ký hợp đồng</summary>
    public string? TenNhaThauTrungThau { get; set; }

    /// <summary>Giá trị tiết kiệm sau đấu thầu = GiaTriGoiThau - TongGiaTriHopDong</summary>
    public decimal GiaTriTietKiem => GiaTriGoiThau > TongGiaTriHopDong ? GiaTriGoiThau - TongGiaTriHopDong : 0;

    /// <summary>Tỷ lệ sử dụng dự toán (%) = (TongGiaTriHopDong / GiaTriGoiThau) * 100</summary>
    public double TyLeSuDungDuToanPercent => GiaTriGoiThau > 0 ? Math.Round((double)(TongGiaTriHopDong / GiaTriGoiThau) * 100, 2) : 0;

    /// <summary>Trạng thái gói thầu (Đã hoàn thành LCNT, Đang lựa chọn nhà thầu...)</summary>
    public string TrangThaiGoiThau { get; set; } = "Đang thực hiện";

    /// <summary>Số hợp đồng hoặc chuỗi các số hợp đồng liên kết với gói thầu</summary>
    public string? SoHopDong { get; set; }

    /// <summary>ID hợp đồng liên kết (hợp đồng đầu tiên nếu có)</summary>
    public Guid? HopDongId { get; set; }

    /// <summary>Mã hợp đồng liên kết (hợp đồng đầu tiên nếu có)</summary>
    public string? HopDongCode { get; set; }

    /// <summary>Danh sách thông tin tóm tắt các hợp đồng thuộc gói thầu</summary>
    public List<GoiThauHopDongSummaryDto> HopDongs { get; set; } = new();
}

public class GoiThauHopDongSummaryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? SoHopDong { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal GiaTriHopDong { get; set; }
}
