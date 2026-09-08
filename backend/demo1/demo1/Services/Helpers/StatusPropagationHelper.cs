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
    /// If so, updates contract status to HoanThanh (3).
    /// </summary>
    public static async Task PropagateContractStatusAsync(AppDbContext context, Guid hopDongId)
    {
        var hopDong = await context.HopDongs
            .Include(h => h.DotThanhToans)
            .FirstOrDefaultAsync(h => h.Id == hopDongId);

        if (hopDong == null || hopDong.TrangThai == TrangThaiHopDong.HoanThanh)
            return;

        if (hopDong.DotThanhToans != null && hopDong.DotThanhToans.Any())
        {
            bool allPaid = hopDong.DotThanhToans.All(d => d.IsPaid);
            if (allPaid)
            {
                hopDong.TrangThai = TrangThaiHopDong.HoanThanh;
                hopDong.UpdatedAt = DateTime.UtcNow;

                if (hopDong.GoiThauId.HasValue)
                {
                    await PropagatePackageStatusAsync(context, hopDong.GoiThauId.Value);
                }
            }
        }
    }

    /// <summary>
    /// Checks if the contract for a package is completed and all package tasks are done.
    /// If so, updates package status to HoanThanh (3).
    /// </summary>
    public static async Task PropagatePackageStatusAsync(AppDbContext context, Guid goiThauId)
    {
        var goiThau = await context.GoiThaus
            .Include(g => g.HopDong)
            .FirstOrDefaultAsync(g => g.Id == goiThauId);

        if (goiThau == null || goiThau.TrangThai == TrangThaiGoiThau.HoanThanh)
            return;

        // Check contract status
        bool contractDone = goiThau.HopDong != null && goiThau.HopDong.TrangThai == TrangThaiHopDong.HoanThanh;

        // Check tasks status
        var pendingTasksCount = await context.CongViecGoiThaus
            .Where(c => c.GoiThauId == goiThauId && c.TrangThai != TrangThaiCongViec.HoanThanh)
            .CountAsync();

        if (contractDone && pendingTasksCount == 0)
        {
            goiThau.TrangThai = TrangThaiGoiThau.HoanThanh;
            goiThau.UpdatedAt = DateTime.UtcNow;

            if (goiThau.DuAnId.HasValue)
            {
                await PropagateProjectStatusAsync(context, goiThau.DuAnId.Value);
            }
        }
    }

    /// <summary>
    /// Checks if all packages in a project are completed.
    /// If so, updates project status to HoanThanh (3).
    /// </summary>
    public static async Task PropagateProjectStatusAsync(AppDbContext context, Guid duAnId)
    {
        var duAn = await context.DuAns.FirstOrDefaultAsync(d => d.Id == duAnId);
        if (duAn == null || duAn.TrangThai == 3) // 3 = HoanThanh
            return;

        var packages = await context.GoiThaus.Where(g => g.DuAnId == duAnId).ToListAsync();
        if (packages.Any() && packages.All(g => g.TrangThai == TrangThaiGoiThau.HoanThanh))
        {
            duAn.TrangThai = 3; // HoanThanh
            duAn.DaKetThuc = true;
            duAn.NgayKetThuc = DateTime.UtcNow;
            duAn.UpdatedAt = DateTime.UtcNow;
        }
    }
}
