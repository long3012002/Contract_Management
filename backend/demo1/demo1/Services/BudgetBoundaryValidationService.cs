using System;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.Entity;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services;

public interface IBudgetBoundaryValidationService
{
    Task ValidateGoiThauBudgetAsync(Guid duAnId, decimal giaTriGoiThau, Guid? currentGoiThauId = null);
    Task ValidateHopDongBudgetAsync(Guid goiThauId, decimal giaTriHopDong, Guid? currentHopDongId = null);
    Task ValidateDotThanhToanBudgetAsync(Guid hopDongId, decimal soTienDeXuat, Guid? currentDotThanhToanId = null);
}

public class BudgetBoundaryValidationService : IBudgetBoundaryValidationService
{
    private readonly AppDbContext _context;

    public BudgetBoundaryValidationService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ràng buộc 1: Tổng giá các gói thầu của dự án không được vượt quá Tổng ngân sách được duyệt của Dự án.
    /// Ngân sách được duyệt lấy từ tổng SoTienDuocDuyet thuộc các Kế hoạch vốn đã ở trạng thái Approved (3).
    /// </summary>
    public async Task ValidateGoiThauBudgetAsync(Guid duAnId, decimal giaTriGoiThau, Guid? currentGoiThauId = null)
    {
        var duAn = await _context.DuAns
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == duAnId);

        if (duAn == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Dự án với ID [{duAnId}].");
        }

        if (duAn.TrangThai == (int)TrangThaiDuAn.Merged)
        {
            throw new InvalidOperationException($"Dự án [{duAn.Code}] đã ở trạng thái Đã gộp (Merged), không thể thêm/chỉnh sửa gói thầu.");
        }

        // Tính tổng ngân sách được duyệt từ các Kế hoạch vốn đã Approved
        var tongNganSachDuocDuyet = await _context.KeHoachVonDuAns
            .AsNoTracking()
            .Where(k => k.DuAnId == duAnId && k.KeHoachVon.TrangThai == 3) // Approved
            .SumAsync(k => (decimal?)k.SoTienDuocDuyet) ?? duAn.DuToanPheDuyet;

        // Nếu dự án chưa có KHV được duyệt, lấy tạm DuToanPheDuyet nếu > 0
        if (tongNganSachDuocDuyet <= 0 && duAn.DuToanPheDuyet > 0)
        {
            tongNganSachDuocDuyet = duAn.DuToanPheDuyet;
        }

        // Tính tổng giá trị các gói thầu hiện có của dự án
        var tongGiaTriGoiThauHienTai = await _context.GoiThaus
            .AsNoTracking()
            .Where(g => g.DuAnId == duAnId && (!currentGoiThauId.HasValue || g.Id != currentGoiThauId.Value))
            .SumAsync(g => (decimal?)g.GiaTriGoiThau) ?? 0m;

        var tongGiaTriSauKhiThem = tongGiaTriGoiThauHienTai + giaTriGoiThau;

        if (tongNganSachDuocDuyet > 0 && tongGiaTriSauKhiThem > tongNganSachDuocDuyet)
        {
            var vuot = tongGiaTriSauKhiThem - tongNganSachDuocDuyet;
            throw new InvalidOperationException(
                $"Giá trị Gói thầu ({giaTriGoiThau:#,##0} VNĐ) làm cho Tổng giá trị các gói thầu ({tongGiaTriSauKhiThem:#,##0} VNĐ) vượt quá Ngân sách được duyệt của Dự án [{duAn.Code}] ({tongNganSachDuocDuyet:#,##0} VNĐ). Vượt quá: {vuot:#,##0} VNĐ.");
        }
    }

    /// <summary>
    /// Ràng buộc 2: Giá trị Hợp đồng không được vượt quá Giá gói thầu được duyệt.
    /// </summary>
    public async Task ValidateHopDongBudgetAsync(Guid goiThauId, decimal giaTriHopDong, Guid? currentHopDongId = null)
    {
        var goiThau = await _context.GoiThaus
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == goiThauId);

        if (goiThau == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Gói thầu với ID [{goiThauId}].");
        }

        var giaTriGoiThau = goiThau.GiaTriGoiThau;

        var tongHopDongHienTai = await _context.HopDongs
            .AsNoTracking()
            .Where(h => h.GoiThauId == goiThauId && (!currentHopDongId.HasValue || h.Id != currentHopDongId.Value))
            .SumAsync(h => (decimal?)h.GiaTriHopDong) ?? 0m;

        var tongHopDongSauKhiThem = tongHopDongHienTai + giaTriHopDong;

        if (giaTriGoiThau > 0 && tongHopDongSauKhiThem > giaTriGoiThau)
        {
            var vuot = tongHopDongSauKhiThem - giaTriGoiThau;
            throw new InvalidOperationException(
                $"Giá trị Hợp đồng ({giaTriHopDong:#,##0} VNĐ) làm cho Tổng giá trị hợp đồng ({tongHopDongSauKhiThem:#,##0} VNĐ) vượt quá Giá gói thầu được duyệt ({giaTriGoiThau:#,##0} VNĐ). Vượt quá: {vuot:#,##0} VNĐ.");
        }
    }

    /// <summary>
    /// Ràng buộc 3: Tổng các đợt thanh toán không được vượt quá Giá trị Hợp đồng thực tế (Giá trị HĐ gốc + các Phụ lục điều chỉnh giá).
    /// </summary>
    public async Task ValidateDotThanhToanBudgetAsync(Guid hopDongId, decimal soTienDeXuat, Guid? currentDotThanhToanId = null)
    {
        var hopDong = await _context.HopDongs
            .Include(h => h.PhuLucHopDongs)
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == hopDongId);

        if (hopDong == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Hợp đồng với ID [{hopDongId}].");
        }

        // Giá trị Hợp đồng thực tế = Giá trị HĐ gốc + Tổng giá trị phụ lục điều chỉnh
        var tongPhuLuc = hopDong.PhuLucHopDongs?.Sum(p => p.GiaTriDieuChinh) ?? 0m;
        var giaTriHopDongThucTe = hopDong.GiaTriHopDong + tongPhuLuc;

        // Tổng các đợt thanh toán đã tạo trước đó
        var tongDaThanhToan = await _context.DotThanhToans
            .AsNoTracking()
            .Where(d => d.HopDongId == hopDongId && (!currentDotThanhToanId.HasValue || d.Id != currentDotThanhToanId.Value))
            .SumAsync(d => (decimal?)d.GiaTriThanhToan) ?? 0m;

        var tongThanhToanSauKhiThem = tongDaThanhToan + soTienDeXuat;

        if (giaTriHopDongThucTe > 0 && tongThanhToanSauKhiThem > giaTriHopDongThucTe)
        {
            var vuot = tongThanhToanSauKhiThem - giaTriHopDongThucTe;
            var giaTriConLai = Math.Max(0, giaTriHopDongThucTe - tongDaThanhToan);
            throw new InvalidOperationException(
                $"Tổng giá trị đợt thanh toán đề xuất ({soTienDeXuat:#,##0} VNĐ) vượt quá giá trị còn lại của Hợp đồng [{hopDong.Code}] ({giaTriConLai:#,##0} VNĐ). Giá trị HĐ thực tế: {giaTriHopDongThucTe:#,##0} VNĐ, Đã thanh toán: {tongDaThanhToan:#,##0} VNĐ. Vượt quá: {vuot:#,##0} VNĐ.");
        }
    }
}
