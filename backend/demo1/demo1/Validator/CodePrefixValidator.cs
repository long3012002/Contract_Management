using System;

namespace demo1.Validator;

public static class CodePrefixValidator
{
    public static void ValidateDuAnCode(string? code)
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

    public static string FormatDuAnCode(string? code)
    {
        ValidateDuAnCode(code);
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

    public static void ValidateDoiTacCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Mã nhà thầu không được để trống.");
        }
        var trimmed = code.Trim();
        if (!trimmed.StartsWith("NT-", StringComparison.OrdinalIgnoreCase) || trimmed.Length <= 3)
        {
            throw new ArgumentException("Mã nhà thầu phải có tiền tố 'NT-' theo định dạng NT-[Tên ngắn gọn] (Ví dụ: NT-CMC).");
        }
    }

    public static string FormatDoiTacCode(string? code)
    {
        ValidateDoiTacCode(code);
        return code!.Trim();
    }

    public static void ValidateNguonVonCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Mã nguồn vốn không được để trống.");
        }
        var trimmed = code.Trim();
        if (!trimmed.StartsWith("NV-", StringComparison.OrdinalIgnoreCase) || trimmed.Length <= 3)
        {
            throw new ArgumentException("Mã nguồn vốn phải có tiền tố 'NV-' theo định dạng NV-[Tên viết tắt] (Ví dụ: NV-NSNN).");
        }
    }

    public static string FormatNguonVonCode(string? code)
    {
        ValidateNguonVonCode(code);
        return code!.Trim();
    }

    public static void ValidatePhanLoaiDuAnCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Mã loại dự án không được để trống.");
        }
        var trimmed = code.Trim();
        if (!trimmed.StartsWith("PL-", StringComparison.OrdinalIgnoreCase) || trimmed.Length <= 3)
        {
            throw new ArgumentException("Mã loại dự án phải có tiền tố 'PL-' theo định dạng PL-[Tên viết tắt] (Ví dụ: PL-HTPM).");
        }
    }

    public static string FormatPhanLoaiDuAnCode(string? code)
    {
        ValidatePhanLoaiDuAnCode(code);
        return code!.Trim();
    }

    public static void ValidateLoaiHopDongCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Mã loại hợp đồng không được để trống.");
        }
        var trimmed = code.Trim();
        if (!trimmed.StartsWith("PLHD-", StringComparison.OrdinalIgnoreCase) || trimmed.Length <= 5)
        {
            throw new ArgumentException("Mã loại hợp đồng phải có tiền tố 'PLHD-' theo định dạng PLHD-[Tên viết tắt] (Ví dụ: PLHD-BT).");
        }
    }

    public static string FormatLoaiHopDongCode(string? code)
    {
        ValidateLoaiHopDongCode(code);
        return code!.Trim();
    }
}
