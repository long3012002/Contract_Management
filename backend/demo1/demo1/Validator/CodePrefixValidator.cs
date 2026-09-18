using System;

namespace demo1.Validator;

public static class CodePrefixValidator
{
    public const string PRJ_SRC_PREFIX = "PRJ_SRC-";
    public const string PRJ_SUB_PREFIX = "PRJ_SUB-";
    public const string PKG_PREFIX = "PKG-";
    public const string CTR_PREFIX = "CTR-";
    public const string PAY_PREFIX = "PAY-";

    /// <summary>
    /// Kiểm tra tính hợp lệ mã dự án (chỉ validation, không tự động chuẩn hóa/thêm tiền tố).
    /// Dự án nguồn (LoaiDuAn = 1) phải bắt đầu bằng PRJ_SRC-
    /// Dự án triển khai (LoaiDuAn = 2) phải bắt đầu bằng PRJ_SUB-
    /// </summary>
    public static void ValidateDuAnCode(string? code, int loaiDuAn)
    {
        var expectedPrefix = loaiDuAn == 2 ? PRJ_SUB_PREFIX : PRJ_SRC_PREFIX;

        if (string.IsNullOrWhiteSpace(code) || !code.Trim().StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var msg = loaiDuAn == 2
                ? $"Mã dự án triển khai phải bắt đầu bằng '{PRJ_SUB_PREFIX}'."
                : $"Mã dự án nguồn phải bắt đầu bằng '{PRJ_SRC_PREFIX}'.";
            throw new ArgumentException(msg);
        }
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ mã gói thầu (chỉ validation, phải bắt đầu bằng PKG-).
    /// </summary>
    public static void ValidateGoiThauCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || !code.Trim().StartsWith(PKG_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Mã gói thầu phải bắt đầu bằng '{PKG_PREFIX}'.");
        }
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ mã hợp đồng (chỉ validation, phải bắt đầu bằng CTR-).
    /// </summary>
    public static void ValidateHopDongCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || !code.Trim().StartsWith(CTR_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Số/mã hợp đồng phải bắt đầu bằng '{CTR_PREFIX}'.");
        }
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ tên đợt thanh toán (chỉ validation, phải bắt đầu bằng PAY-).
    /// </summary>
    public static void ValidateDotThanhToanTen(string? tenDot)
    {
        if (string.IsNullOrWhiteSpace(tenDot) || !tenDot.Trim().StartsWith(PAY_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Tên đợt thanh toán phải bắt đầu bằng '{PAY_PREFIX}'.");
        }
    }

    /* 
    ===================================================================
    [DỰ PHÒNG] Code tự động chuẩn hóa / tự động gắn tiền tố (Auto-formatting)
    Đã comment lại theo yêu cầu chỉ kiểm tra (Validation).
    ===================================================================

    public static string FormatDuAnCode(string? code, int loaiDuAn)
    {
        ValidateDuAnCode(code, loaiDuAn);
        return code!.Trim();
    }

    public static string FormatGoiThauCode(string? code)
    {
        ValidateGoiThauCode(code);
        return code!.Trim();
    }

    public static string FormatHopDongCode(string? code)
    {
        ValidateHopDongCode(code);
        return code!.Trim();
    }

    public static string FormatDotThanhToanTen(string? tenDot)
    {
        ValidateDotThanhToanTen(tenDot);
        return tenDot!.Trim();
    }
    */

    public static string FormatDuAnCode(string? code, int loaiDuAn)
    {
        ValidateDuAnCode(code, loaiDuAn);
        return code!.Trim();
    }

    public static string FormatGoiThauCode(string? code)
    {
        ValidateGoiThauCode(code);
        return code!.Trim();
    }

    public static string FormatHopDongCode(string? code)
    {
        ValidateHopDongCode(code);
        return code!.Trim();
    }

    public static string FormatDotThanhToanTen(string? tenDot)
    {
        ValidateDotThanhToanTen(tenDot);
        return tenDot!.Trim();
    }
}
