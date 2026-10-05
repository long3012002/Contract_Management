using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace demo1.Services.Implements;

/// <summary>
/// Service xử lý tổng hợp và xuất báo cáo nghiệp vụ.
/// Được chia thành các partial files trong thư mục Reports/ theo từng nhóm báo cáo cụ thể.
/// </summary>
public partial class ReportService : IReportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReportService> _logger;
    private readonly ICurrentUserService? _currentUserService;

    public ReportService(AppDbContext context, ILogger<ReportService> logger, ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    #region Shared Helpers

    private static (decimal Factor, string UnitName) ParseUnit(string? donViTinh)
    {
        if (string.IsNullOrWhiteSpace(donViTinh))
            return (1m, "Đồng");

        var s = donViTinh.Trim().ToLowerInvariant();
        if (s == "4" || s.Contains("tỷ") || s.Contains("ty"))
            return (1_000_000_000m, "Tỷ đồng");
        if (s == "3" || s.Contains("triệu") || s.Contains("trieu"))
            return (1_000_000m, "Triệu đồng");
        if (s == "2" || s.Contains("nghìn") || s.Contains("ngan") || s == "k")
            return (1_000m, "Nghìn đồng");
        if (s == "1" || s.Contains("đồng") || s.Contains("dong") || s == "vnd")
            return (1m, "Đồng");

        return (1m, "Đồng");
    }

    private static (DateTime StartOfPeriod, DateTime EndOfPeriod, string PeriodDisplayName, string PeriodName) CalculateReportPeriod(int year, int period, DateTime? fromDate, DateTime? toDate)
    {
        if (period == 0 || fromDate.HasValue || toDate.HasValue)
        {
            DateTime start = fromDate ?? new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end;
            if (toDate.HasValue)
            {
                end = toDate.Value;
                if (end.TimeOfDay == TimeSpan.Zero)
                {
                    end = end.Date.AddDays(1).AddTicks(-1);
                }
            }
            else
            {
                end = new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            }

            if (start.Kind != DateTimeKind.Utc) start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            if (end.Kind != DateTimeKind.Utc) end = DateTime.SpecifyKind(end, DateTimeKind.Utc);
            return (start, end, $"từ {start:dd/MM/yyyy} đến {end:dd/MM/yyyy}", "TuyChon");
        }

        switch (period)
        {
            case 1: // Cả năm (Kỳ 1)
                return (
                    new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    $"năm {year}",
                    "1N"
                );
            case 2: // 6 tháng đầu năm (Kỳ 2)
                return (
                    new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(year, 6, 30, 23, 59, 59, DateTimeKind.Utc),
                    $"6T đầu năm {year}",
                    "6T"
                );
            case 3: // 6 tháng cuối năm (Kỳ 3)
                return (
                    new DateTime(year, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    $"6T cuối năm {year}",
                    "6TCuoi"
                );
            case 4: // Quý 1
                return (
                    new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(year, 3, 31, 23, 59, 59, DateTimeKind.Utc),
                    $"Quý 1 năm {year}",
                    "Q1"
                );
            case 5: // Quý 2
                return (
                    new DateTime(year, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(year, 6, 30, 23, 59, 59, DateTimeKind.Utc),
                    $"Quý 2 năm {year}",
                    "Q2"
                );
            case 6: // Quý 3
                return (
                    new DateTime(year, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(year, 9, 30, 23, 59, 59, DateTimeKind.Utc),
                    $"Quý 3 năm {year}",
                    "Q3"
                );
            case 7: // Quý 4
                return (
                    new DateTime(year, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    $"Quý 4 năm {year}",
                    "Q4"
                );
            default:
                if (period >= 11 && period <= 22) // Tháng 1 - 12
                {
                    int month = period - 10;
                    int days = DateTime.DaysInMonth(year, month);
                    return (
                        new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(year, month, days, 23, 59, 59, DateTimeKind.Utc),
                        $"Tháng {month} năm {year}",
                        $"T{month}"
                    );
                }

                return (
                    new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    $"năm {year}",
                    "1N"
                );
        }
    }



    private string GetLoaiHopDongName(int loaiHopDong)
    {
        return loaiHopDong switch
        {
            1 => "HĐ Mua sắm / Triển khai",
            2 => "HĐ Bảo trì / Bảo dưỡng",
            3 => "HĐ Tư vấn / Dịch vụ",
            _ => "Hợp đồng khác"
        };
    }



    #endregion
}
