using System;
using System.Collections.Generic;
using System.Linq;
using demo1.DTOs;

namespace demo1.Validator;

public static class HopDongValidator
{
    public static void EnsureValid(decimal giaTriHopDong, List<CreateDotThanhToanDto> dotThanhToans)
    {
        if (giaTriHopDong < 0)
        {
            throw new ArgumentException("Giá trị hợp đồng không được âm.");
        }

        if (dotThanhToans != null && dotThanhToans.Any())
        {
            var sumTyLe = dotThanhToans.Sum(d => d.TyLeThanhToan);
            if (sumTyLe > 100)
            {
                throw new ArgumentException($"Tổng tỷ lệ các đợt thanh toán ({sumTyLe}%) không được vượt quá 100%.");
            }

            if (dotThanhToans.Any(d => d.TyLeThanhToan < 0 || d.GiaTriThanhToan < 0))
            {
                throw new ArgumentException("Tỷ lệ thanh toán và giá trị thanh toán không được âm.");
            }
        }
    }
}
