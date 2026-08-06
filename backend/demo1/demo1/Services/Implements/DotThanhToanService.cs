using System;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements;

public class DotThanhToanService(AppDbContext context) : IDotThanhToanService
{
    public async Task<PagedResult<DotThanhToanReportDto>> GetFilteredPaymentPhasesAsync(DotThanhToanFilterDto filter)
    {
        var query = context.DotThanhToans
            .Include(d => d.HopDong)
                .ThenInclude(h => h.GoiThau)
            .Include(d => d.HopDong)
                .ThenInclude(h => h.DuAn)
            .Where(d => d.HopDong.IsActive) // Chỉ lấy các đợt thanh toán thuộc hợp đồng đang hoạt động
            .AsQueryable();

        // 1. Lọc theo năm
        if (filter.Year.HasValue)
        {
            int year = filter.Year.Value;
            query = query.Where(d =>
                (d.NgayThanhToan.HasValue && d.NgayThanhToan.Value.Year == year) ||
                (!d.NgayThanhToan.HasValue && d.CreatedAt.Year == year)
            );
        }

        // 2. Lọc theo khoảng thời gian ngày thanh toán
        if (filter.FromDate.HasValue)
        {
            query = query.Where(d => d.NgayThanhToan >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(d => d.NgayThanhToan <= filter.ToDate.Value);
        }

        // 3. Lọc theo trạng thái thanh toán
        if (filter.IsPaid.HasValue)
        {
            query = query.Where(d => d.IsPaid == filter.IsPaid.Value);
        }

        // 4. Tìm kiếm từ khóa nâng cao
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string searchLower = filter.Search.Trim().ToLower();
            query = query.Where(d =>
                d.TenDot.ToLower().Contains(searchLower) ||
                d.HopDong.Code.ToLower().Contains(searchLower) ||
                d.HopDong.Name.ToLower().Contains(searchLower) ||
                (d.HopDong.GoiThau != null && d.HopDong.GoiThau.Name.ToLower().Contains(searchLower)) ||
                (d.HopDong.GoiThau != null && d.HopDong.GoiThau.Code.ToLower().Contains(searchLower)) ||
                (d.HopDong.DuAn != null && d.HopDong.DuAn.Name.ToLower().Contains(searchLower)) ||
                (d.HopDong.DuAn != null && d.HopDong.DuAn.Code.ToLower().Contains(searchLower))
            );
        }

        // Đếm tổng số bản ghi
        int totalItems = await query.CountAsync();

        // Phân trang và lấy dữ liệu
        var items = await query
            .OrderByDescending(d => d.NgayThanhToan)
            .ThenByDescending(d => d.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(d => new DotThanhToanReportDto
            {
                Id = d.Id,
                TenDot = d.TenDot,
                TyLeThanhToan = d.TyLeThanhToan,
                GiaTriThanhToan = d.GiaTriThanhToan,
                NgayThanhToan = d.NgayThanhToan,
                DieuKienThanhToan = d.DieuKienThanhToan,
                IsPaid = d.IsPaid,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,

                HopDongId = d.HopDongId,
                HopDongCode = d.HopDong.Code,
                HopDongName = d.HopDong.Name,

                GoiThauId = d.HopDong.GoiThauId,
                GoiThauCode = d.HopDong.GoiThau != null ? d.HopDong.GoiThau.Code : null,
                GoiThauName = d.HopDong.GoiThau != null ? d.HopDong.GoiThau.Name : null,

                DuAnId = d.HopDong.DuAnId,
                DuAnCode = d.HopDong.DuAn != null ? d.HopDong.DuAn.Code : null,
                DuAnName = d.HopDong.DuAn != null ? d.HopDong.DuAn.Name : null
            })
            .ToListAsync();

        return new PagedResult<DotThanhToanReportDto>
        {
            Items = items,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalItems = totalItems
        };
    }
}
