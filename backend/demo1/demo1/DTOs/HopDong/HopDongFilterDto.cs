using System;

namespace demo1.DTOs;

public class HopDongFilterDto
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Cursor { get; set; }

    public Guid? DuAnId { get; set; }
    public Guid? GoiThauId { get; set; }
    public Guid? ChuDauTuId { get; set; }
    public Guid? NhaThauId { get; set; }
    public int? LoaiHopDong { get; set; }
    public Guid? LoaiHopDongId { get; set; }
    /// <summary>
    /// Tham số lọc loại hợp đồng hợp nhất (Chấp nhận cả chuỗi số Enum: "1", "2"... hoặc GUID chuỗi: "a1b2c3...")
    /// </summary>
    public string? ContractTypeId { get; set; }
    public int? HinhThucThanhToan { get; set; }

    public DateTime? FromNgayHieuLuc { get; set; }
    public DateTime? ToNgayHieuLuc { get; set; }
    public decimal? MinGiaTri { get; set; }
    public decimal? MaxGiaTri { get; set; }
}
