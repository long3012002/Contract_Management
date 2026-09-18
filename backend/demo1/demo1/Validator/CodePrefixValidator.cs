using System;

namespace demo1.Validator;

public static class CodePrefixValidator
{
    public static void ValidateDuAnCode(string? code, int loaiDuAn)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Mã dự án không được để trống.");
        }
    }

    public static void ValidateGoiThauCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Mã gói thầu không được để trống.");
        }
    }

    public static void ValidateHopDongCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Số/Mã hợp đồng không được để trống.");
        }
    }

    public static void ValidateDotThanhToanTen(string? tenDot)
    {
        if (string.IsNullOrWhiteSpace(tenDot))
        {
            throw new ArgumentException("Tên đợt thanh toán không được để trống.");
        }
    }

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
