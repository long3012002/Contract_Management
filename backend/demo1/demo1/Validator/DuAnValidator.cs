using System;

namespace demo1.Validator;

public static class DuAnValidator
{
    public static void EnsureValid(decimal duToanPheDuyet, DateTime? ngayBatDau, DateTime? ngayKetThuc, int? namBatDau, int? namKetThuc, DateTime? ngayKetThucThucTe = null)
    {
        if (duToanPheDuyet < 0)
        {
            throw new ArgumentException("Dự toán phê duyệt dự án không được âm.");
        }

        if (ngayBatDau.HasValue && ngayKetThuc.HasValue && ngayBatDau.Value > ngayKetThuc.Value)
        {
            throw new ArgumentException("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
        }

        if (ngayBatDau.HasValue && ngayKetThucThucTe.HasValue && ngayBatDau.Value > ngayKetThucThucTe.Value)
        {
            throw new ArgumentException("Ngày bắt đầu không được lớn hơn ngày kết thúc thực tế.");
        }

        if (namBatDau.HasValue && namKetThuc.HasValue && namBatDau.Value > namKetThuc.Value)
        {
            throw new ArgumentException("Năm bắt đầu không được lớn hơn năm kết thúc.");
        }
    }

    public static void ValidateNguonVon(decimal duToanPheDuyet, IEnumerable<DTOs.CreateDuAnNguonVonDto>? danhSachNguonVon)
    {
        if (danhSachNguonVon != null && danhSachNguonVon.Any())
        {
            var tongNguonVon = danhSachNguonVon.Sum(x => x.SoTien);
            if (tongNguonVon > duToanPheDuyet)
            {
                throw new InvalidOperationException($"Tổng số tiền các nguồn vốn ({tongNguonVon:N0} VNĐ) không được vượt quá tổng mức đầu tư dự án ({duToanPheDuyet:N0} VNĐ).");
            }
        }
    }
}
