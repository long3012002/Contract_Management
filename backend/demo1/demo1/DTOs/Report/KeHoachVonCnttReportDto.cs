using System;
using System.Collections.Generic;

namespace demo1.DTOs;

public class NguonVonHeaderDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class KeHoachVonCnttPhanKyDto
{
    public int Nam { get; set; }
    public decimal GiaTri { get; set; }
}

public class KeHoachVonCnttReportRowDto
{
    public int Stt { get; set; }
    public Guid? DuAnId { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public decimal TongMucDauTu { get; set; }

    // Nguồn vốn (Tương thích ngược)
    public decimal VonTuCo { get; set; } // Vốn tự có cho XDCB & MSTSCĐ
    public decimal QuyDauTuPhatTrien { get; set; }
    public decimal NguonKhac { get; set; }

    // Nguồn vốn động theo danh mục
    public Guid? NguonVonId { get; set; }
    public string? TenNguonVon { get; set; }
    public Dictionary<Guid, decimal> NguonVonChiTiet { get; set; } = new();

    // Phân kỳ đầu tư theo năm
    public List<KeHoachVonCnttPhanKyDto> PhanKyDauTu { get; set; } = new();

    public string? TrangThaiText { get; set; }
    public string? DonViDeXuatChiDao { get; set; }
    public string? GhiChu { get; set; }
    public int NhomTrangThai { get; set; } // 1: Đã phê duyệt/đang triển khai, 2: Đề xuất mới

    // Phân loại dự án
    public string LoaiDuAn { get; set; } = string.Empty;
    public Guid? PhanLoaiDuAnId { get; set; }
    public string? PhanLoaiDuAnCode { get; set; }
    public string? TenPhanLoaiDuAn { get; set; }
}

public class KeHoachVonCnttReportGroupDto
{
    public int NhomTrangThai { get; set; }
    public string TenNhom { get; set; } = string.Empty;
    public string LoaiDuAnKey { get; set; } = string.Empty;
    public List<KeHoachVonCnttReportRowDto> Rows { get; set; } = new();
    public decimal TongMucDauTuNhom { get; set; }
    public decimal TongVonTuCoNhom { get; set; }
    public decimal TongQuyDauTuPhatTrienNhom { get; set; }
    public decimal TongNguonKhacNhom { get; set; }
    public Dictionary<Guid, decimal> TongNguonVonByDanhMucNhom { get; set; } = new();
    public Dictionary<int, decimal> TongPhanKyNhom { get; set; } = new();
}

public class KeHoachVonCnttReportResponseDto
{
    public string Title { get; set; } = "TỔNG HỢP KẾ HOẠCH VỐN ĐẦU TƯ CNTT GIAI ĐOẠN";
    public int FromYear { get; set; }
    public int ToYear { get; set; }
    public string Unit { get; set; } = "Đồng";
    public int TongSoDuAn { get; set; }
    public List<NguonVonHeaderDto> DanhSachNguonVon { get; set; } = new();
    public List<KeHoachVonCnttReportGroupDto> Groups { get; set; } = new();
    public decimal TongCongMucDauTu { get; set; }
    public decimal TongCongVonTuCo { get; set; }
    public decimal TongCongQuyDauTuPhatTrien { get; set; }
    public decimal TongCongNguonKhac { get; set; }
    public Dictionary<Guid, decimal> TongCongNguonVonByDanhMuc { get; set; } = new();
    public Dictionary<int, decimal> TongCongPhanKy { get; set; } = new();
}
