using System;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.Entity;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Helpers;

public static class StatusPropagationHelper
{
    /// <summary>
    /// Checks if all payment installments for a contract are paid.
    /// If so, propagates status to package and project.
    /// </summary>
    public static async Task PropagateContractStatusAsync(AppDbContext context, Guid hopDongId)
    {
        var hopDong = await context.HopDongs
            .Include(h => h.DotThanhToans)
            .FirstOrDefaultAsync(h => h.Id == hopDongId);

        if (hopDong == null)
            return;

        if (hopDong.DotThanhToans != null && hopDong.DotThanhToans.Any())
        {
            bool allPaid = hopDong.DotThanhToans.All(d => d.IsPaid);
            if (allPaid && hopDong.GoiThauId.HasValue)
            {
                await PropagatePackageStatusAsync(context, hopDong.GoiThauId.Value);
            }
        }
    }

    /// <summary>
    /// Checks if the contract for a package is completed and all package tasks are done.
    /// If so, updates project status if all packages in the project are completed.
    /// </summary>
    public static async Task PropagatePackageStatusAsync(AppDbContext context, Guid goiThauId)
    {
        var goiThau = await context.GoiThaus
            .FirstOrDefaultAsync(g => g.Id == goiThauId);

        if (goiThau == null)
            return;

        var hopDong = await context.HopDongs
            .Include(h => h.DotThanhToans)
            .FirstOrDefaultAsync(h => h.GoiThauId == goiThauId);

        // Check contract status
        bool contractDone = hopDong != null && hopDong.DotThanhToans.Any() && hopDong.DotThanhToans.All(d => d.IsPaid);

        // Check tasks status
        var pendingTasksCount = await context.CongViecGoiThaus
            .Where(c => c.GoiThauId == goiThauId && c.TinhTrang != "Đã xong")
            .CountAsync();

        if (contractDone && pendingTasksCount == 0 && goiThau.DuAnId.HasValue)
        {
            await PropagateProjectStatusAsync(context, goiThau.DuAnId.Value);
        }
    }

    /// <summary>
    /// Checks if all packages in a project are completed.
    /// If so, updates project status to HoanThanh (2).
    /// </summary>
    public static async Task PropagateProjectStatusAsync(AppDbContext context, Guid duAnId)
    {
        var duAn = await context.DuAns.FirstOrDefaultAsync(d => d.Id == duAnId);
        if (duAn == null || duAn.TrangThai == 2) // 2 = HoanThanh
            return;

        var packages = await context.GoiThaus.Where(g => g.DuAnId == duAnId).ToListAsync();
        if (!packages.Any())
            return;

        bool allPackagesCompleted = true;
        foreach (var pkg in packages)
        {
            var hd = await context.HopDongs.Include(h => h.DotThanhToans).FirstOrDefaultAsync(h => h.GoiThauId == pkg.Id);
            bool hdDone = hd != null && hd.DotThanhToans.Any() && hd.DotThanhToans.All(d => d.IsPaid);
            var pendingTasks = await context.CongViecGoiThaus.CountAsync(c => c.GoiThauId == pkg.Id && c.TinhTrang != "Đã xong");
            if (!hdDone || pendingTasks > 0)
            {
                allPackagesCompleted = false;
                break;
            }
        }

        if (allPackagesCompleted)
        {
            duAn.TrangThai = 2; // 2 = HoanThanh
            duAn.DaKetThuc = true;
            duAn.NgayKetThuc = DateTime.UtcNow;
            duAn.UpdatedAt = DateTime.UtcNow;
        }
    }
}
