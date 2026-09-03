using System;
using System.Collections.Generic;

namespace demo1.DTOs;

public class KeHoachVonReportRowDto
{
    public int Stt { get; set; }
    public Guid? DuAnId { get; set; }
    public string? DonViChiNhanh { get; set; } = string.Empty;
    public string TenDuAn { get; set; } = string.Empty;
    public string? QuyMoXaydung { get; set; }
    public string? SuCanThiet { get; set; }
    public string? HangMucCongViec { get; set; }
    
    // Đề xuất phê duyệt / Nguồn vốn
    public decimal VonDieuLeVaQuyDuTru { get; set; }
    public decimal QuyPhucLoi { get; set; }
    public decimal QuyDauTuPhatTrien { get; set; }
    public decimal NguonKhac { get; set; }
    public decimal TongDeXuatPheDuyet { get; set; }

    public string? GhiChu { get; set; }
    public string? SoQuyetDinhNghiQuyet { get; set; }
    public int PhuLucType { get; set; } // 1: XDCB, 2: Xe, 3: TSCĐ/CCLĐ, 4: CNTT, 5: Thẻ&NHS, 6: Phụ biểu 01

    public string? NhomKyThuatCode { get; set; }
    public string? TenNhomKyThuat { get; set; }
}

public class KeHoachVonReportNhomKyThuatDto
{
    public string NhomKyThuatCode { get; set; } = string.Empty;
    public string TenNhomKyThuat { get; set; } = string.Empty;
    public List<KeHoachVonReportRowDto> Rows { get; set; } = new();
    public decimal TongVonDieuLeVaQuyDuTru { get; set; }
    public decimal TongQuyPhucLoi { get; set; }
    public decimal TongQuyDauTuPhatTrien { get; set; }
    public decimal TongNguonKhac { get; set; }
    public decimal TongCongDeXuat { get; set; }
}

public class KeHoachVonReportPhuLucDto
{
    public int PhuLucType { get; set; }
    public string TenPhuLuc { get; set; } = string.Empty;
    public List<KeHoachVonReportNhomKyThuatDto> NhomKyThuats { get; set; } = new();
    public List<KeHoachVonReportRowDto> Rows { get; set; } = new();
    public decimal TongVonDieuLeVaQuyDuTru { get; set; }
    public decimal TongQuyPhucLoi { get; set; }
    public decimal TongQuyDauTuPhatTrien { get; set; }
    public decimal TongNguonKhac { get; set; }
    public decimal TongCongDeXuat { get; set; }
}

public class KeHoachVonReportResponseDto
{
    public string Title { get; set; } = "KẾ HOẠCH ĐẦU TƯ & MUA SẮM";
    public int Year { get; set; }
    public string Unit { get; set; } = "Triệu đồng";
    public List<KeHoachVonReportPhuLucDto> PhuLucs { get; set; } = new();
    public decimal TongCacPhuLuc { get; set; }
}
