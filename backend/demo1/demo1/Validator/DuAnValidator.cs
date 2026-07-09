using System;

namespace demo1.Validator;

public static class DuAnValidator
{
    public static void EnsureValid(decimal duToanPheDuyet, DateTime? ngayBatDau, DateTime? ngayKetThuc, int? namBatDau, int? namKetThuc)
    {
        if (duToanPheDuyet < 0)
        {
            throw new ArgumentException("Dự toán phê duyệt dự án không được âm.");
        }

        if (ngayBatDau.HasValue && ngayKetThuc.HasValue && ngayBatDau.Value > ngayKetThuc.Value)
        {
            throw new ArgumentException("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
        }

        if (namBatDau.HasValue && namKetThuc.HasValue && namBatDau.Value > namKetThuc.Value)
        {
            throw new ArgumentException("Năm bắt đầu không được lớn hơn năm kết thúc.");
        }
    }
}
