using System;
using System.Collections.Generic;
using System.Linq;
using demo1.DTOs;

namespace demo1.Validator;

public static class GoiThauValidator
{
    public static void EnsureValid(decimal giaTriGoiThau, decimal nguongCanhBaoPercent)
    {
        if (giaTriGoiThau < 0)
        {
            throw new ArgumentException("Giá trị gói thầu không được âm.");
        }

        if (nguongCanhBaoPercent <= 0 || nguongCanhBaoPercent > 100)
        {
            throw new ArgumentException("Ngưỡng cảnh báo phải nằm trong khoảng 1 đến 100.");
        }
    }

    public static void ValidateBidders(decimal giaTriGoiThau, ICollection<NhaThauGoiThauInputDto>? bidders)
    {
        if (bidders == null || bidders.Count == 0)
        {
            return;
        }

        // Check duplicate bidders
        var bidderIds = bidders.Select(b => b.NhaThauId).ToList();
        if (bidderIds.Count != bidderIds.Distinct().Count())
        {
            throw new InvalidOperationException("Mỗi nhà thầu chỉ được xuất hiện một lần trong gói thầu.");
        }

        if (bidders.Count == 1)
        {
            var single = bidders.First();
            if (single.IsLienDanh)
            {
                throw new InvalidOperationException("Gói thầu chỉ có một nhà thầu thì không phải là liên danh.");
            }
            if (single.TyLeLienDanh != 100)
            {
                throw new InvalidOperationException("Tỷ lệ liên danh của nhà thầu duy nhất phải là 100%.");
            }
            if (single.GiaTriDamNhan != giaTriGoiThau)
            {
                throw new InvalidOperationException($"Giá trị đảm nhận của nhà thầu duy nhất phải bằng giá trị gói thầu ({giaTriGoiThau:N0} VNĐ).");
            }
        }
        else // bidders.Count > 1
        {
            if (bidders.Any(b => !b.IsLienDanh))
            {
                throw new InvalidOperationException("Gói thầu có nhiều nhà thầu thì tất cả phải tham gia với tư cách liên danh.");
            }

            var totalRate = bidders.Sum(b => b.TyLeLienDanh);
            if (totalRate != 100)
            {
                throw new InvalidOperationException("Tổng tỷ lệ liên danh của các thành viên phải bằng 100%.");
            }

            var totalValue = bidders.Sum(b => b.GiaTriDamNhan);
            if (totalValue != giaTriGoiThau)
            {
                throw new InvalidOperationException($"Tổng giá trị đảm nhận của các thành viên ({totalValue:N0} VNĐ) phải bằng giá trị gói thầu ({giaTriGoiThau:N0} VNĐ).");
            }
        }
    }
}
