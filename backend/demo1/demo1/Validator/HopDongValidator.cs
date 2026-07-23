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

            var sumGiaTri = dotThanhToans.Sum(d => d.GiaTriThanhToan > 0 ? d.GiaTriThanhToan : (d.TyLeThanhToan * giaTriHopDong / 100));
            if (sumGiaTri > giaTriHopDong)
            {
                throw new ArgumentException($"Tổng giá trị các đợt thanh toán ({sumGiaTri:N0} VNĐ) không được vượt quá giá trị hợp đồng ({giaTriHopDong:N0} VNĐ).");
            }

            if (dotThanhToans.Any(d => d.TyLeThanhToan < 0 || d.GiaTriThanhToan < 0))
            {
                throw new ArgumentException("Tỷ lệ thanh toán và giá trị thanh toán không được âm.");
            }
        }
    }

    public static void ValidateBidders(decimal giaTriHopDong, ICollection<NhaThauGoiThauInputDto>? bidders)
    {
        if (bidders == null || bidders.Count == 0)
        {
            return;
        }

        // Check duplicate bidders
        var bidderIds = bidders.Select(b => b.NhaThauId).ToList();
        if (bidderIds.Count != bidderIds.Distinct().Count())
        {
            throw new InvalidOperationException("Mỗi nhà thầu chỉ được xuất hiện một lần trong hợp đồng.");
        }

        if (bidders.Count == 1)
        {
            var single = bidders.First();
            if (single.IsLienDanh)
            {
                throw new InvalidOperationException("Hợp đồng chỉ có một nhà thầu thì không phải là liên danh.");
            }
        }
    }
}
