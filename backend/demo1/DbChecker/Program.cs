using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using demo1.Data;
using demo1.Entity;

namespace DbChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=localhost;Port=5432;Database=QLDA;Username=quangmd;Password=31122001;")
                .Options;

            using var db = new AppDbContext(options);

            var duAns = db.DuAns.ToList();
            int countPk = 0;

            foreach (var da in duAns)
            {
                int year = da.NamBatDau ?? 2025;
                bool exists = db.DuAnPhanKyVons.Any(x => x.DuAnId == da.Id);
                if (!exists && da.DuToanPheDuyet > 0)
                {
                    if (da.DuToanPheDuyet > 50_000_000_000m)
                    {
                        decimal p1 = Math.Round(da.DuToanPheDuyet * 0.6m, 0);
                        decimal p2 = da.DuToanPheDuyet - p1;

                        db.DuAnPhanKyVons.Add(new DuAnPhanKyVon
                        {
                            Id = Guid.NewGuid(),
                            DuAnId = da.Id,
                            Nam = year,
                            SoTienPhanKy = p1,
                            TyLePercent = 60,
                            GhiChu = $"Kế hoạch vốn năm {year} (60%)",
                            CreatedAt = DateTime.UtcNow
                        });

                        db.DuAnPhanKyVons.Add(new DuAnPhanKyVon
                        {
                            Id = Guid.NewGuid(),
                            DuAnId = da.Id,
                            Nam = year + 1,
                            SoTienPhanKy = p2,
                            TyLePercent = 40,
                            GhiChu = $"Kế hoạch vốn năm {year + 1} (40%)",
                            CreatedAt = DateTime.UtcNow
                        });
                        countPk += 2;
                    }
                    else
                    {
                        db.DuAnPhanKyVons.Add(new DuAnPhanKyVon
                        {
                            Id = Guid.NewGuid(),
                            DuAnId = da.Id,
                            Nam = year,
                            SoTienPhanKy = da.DuToanPheDuyet,
                            TyLePercent = 100,
                            GhiChu = $"Bố trí vốn năm {year} (100%)",
                            CreatedAt = DateTime.UtcNow
                        });
                        countPk++;
                    }
                    db.SaveChanges();
                }
            }

            Console.WriteLine($"Đã bổ sung thành công {countPk} bản ghi Phân kỳ vốn cho các dự án.");
            Console.WriteLine($"Tổng Phân kỳ vốn hiện tại: {db.DuAnPhanKyVons.Count()}");
        }
    }
}
