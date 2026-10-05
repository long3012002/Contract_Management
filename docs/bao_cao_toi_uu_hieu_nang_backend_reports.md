# BÁO CÁO RÀ SOÁT HIỆU NĂNG CÁC API BÁO CÁO BACKEND & HƯỚNG DẪN TỐI ƯU

> **Tài liệu dành cho**: Backend Developer  
> **Phạm vi rà soát**: `ReportsController.cs`, `ReportService.cs`  
> **Mục tiêu**: Xóa bỏ các điểm nghẽn (bottlenecks) gây chậm, tốn RAM và tải DB không cần thiết khi dữ liệu tăng trưởng.

---

## TỔNG QUAN CÁC ĐIỂM NGHẼN CHÍNH

| Mức độ | Báo cáo bị ảnh hưởng | Vấn đề cốt lõi | Tác động hiệu năng |
| :--- | :--- | :--- | :--- |
| 🔴 **Nghiêm trọng** | `License SLA Report` | Load 100% bảng `Licenses` về RAM rồi mới filter bằng C# | Gây tràn bộ nhớ, Full Table Scan, CPU/GC Spike |
| 🔴 **Nghiêm trọng** | `Kế hoạch vốn CNTT` | Load toàn bộ bảng `DuAnGopLinks` không điều kiện lọc; Vòng lặp $O(N \times M)$ | Quét dư thừa toàn bộ lịch sử gộp dự án, chậm khi dữ liệu lớn |
| 🟡 **Cao** | `Báo cáo Đầu tư` | Quét & Group By toàn bộ bảng `DotThanhToans` trong lịch sử hệ thống | Tải dữ liệu thanh toán của các dự án ngoài phạm vi báo cáo |
| 🟡 **Cao** | `Theo dõi HĐ`, `Thanh toán HĐ` | Dùng `.Include()` nạp toàn bộ Object Entity thay vì Projection DTO | Khởi tạo hàng nghìn object entity không cần thiết trên RAM |
| 🔵 **Trung bình** | Tất cả báo cáo có phân quyền | Subquery lồng `EXISTS` kiểm tra quyền phức tạp theo từng dòng | Gây Nested Loop scans trên DB nếu thiếu Index |

---

## CHI TIẾT VẤN ĐỀ & HƯỚNG DẪN KHẮC PHỤC

---

### 1. [🔴 Nghiêm trọng] Full Table Scan & Filter In-Memory trong Báo cáo License SLA
* **Vị trí**: `ReportService.cs` (Dòng ~3262 – 3311)
* **Phương thức**: `GetLicenseSlaReportAsync`

#### Vấn đề hiện tại:
Backend truy vấn nạp **toàn bộ** danh sách bản ghi `Licenses` (kèm quan hệ `DuAn`, `HopDong`, `NhaCungCap`) về bộ nhớ máy chủ trước:
```csharp
// ĐANG LÀM: Load TOÀN BỘ dữ liệu về RAM
var list = await query.ToListAsync();

foreach (var lic in list)
{
    // Tính toán trạng thái bằng C#
    if (endDate.HasValue && endDate.Value < today) riskTag = "🔴 ĐÃ HẾT HẠN";
    else if (...) riskTag = "🟡 SẮP HẾT HẠN";
    else riskTag = "🟢 AN TOÀN";

    // Sau đó mới filter bằng C# continue!
    if (statusFilter.HasValue)
    {
        if (statusFilter.Value == 1 && !riskTag.Contains("ĐÃ HẾT HẠN")) continue;
        if (statusFilter.Value == 2 && !riskTag.Contains("SẮP HẾT HẠN")) continue;
        if (statusFilter.Value == 3 && !riskTag.Contains("AN TOÀN")) continue;
    }
}
```

#### Cách khắc phục:
Đẩy điều kiện lọc thời gian xuống Database Query (`IQueryable`) trước khi gọi `ToListAsync()`. Database có thể tận dụng Index trên cột `NgayKetThuc`:
```csharp
// ĐỀ XUẤT FIX:
if (statusFilter.HasValue)
{
    switch (statusFilter.Value)
    {
        case 1: // Đã hết hạn
            query = query.Where(l => l.NgayKetThuc.HasValue && l.NgayKetThuc.Value.Date < today);
            break;
        case 2: // Sắp hết hạn (trong khoảng CanhBaoTruocNgay)
            query = query.Where(l => l.NgayKetThuc.HasValue 
                && l.NgayKetThuc.Value.Date >= today 
                && EF.Functions.DateDiffDay(today, l.NgayKetThuc.Value.Date) <= l.CanhBaoTruocNgay);
            break;
        case 3: // Còn hiệu lực / An toàn
            query = query.Where(l => !l.NgayKetThuc.HasValue 
                || EF.Functions.DateDiffDay(today, l.NgayKetThuc.Value.Date) > l.CanhBaoTruocNgay);
            break;
    }
}

var list = await query.ToListAsync();
```

---

### 2. [🔴 Nghiêm trọng] Tải toàn bộ bảng `DuAnGopLinks` trong Báo cáo Kế hoạch vốn CNTT
* **Vị trí**: `ReportService.cs` (Dòng ~2920 – 2946)
* **Phương thức**: `GetKeHoachVonCnttReportAsync`

#### Vấn đề hiện tại:
Mỗi khi người dùng xem hoặc xuất báo cáo Kế hoạch vốn CNTT, hệ thống lại thực thi câu lệnh nạp **toàn bộ dữ liệu lịch sử gộp dự án từ trước đến nay** mà không hề kèm theo điều kiện lọc nào:
```csharp
// ĐANG LÀM: Nạp toàn bộ bảng DuAnGopLinks không có WHERE
var allGopLinks = await _context.DuAnGopLinks
    .AsNoTracking()
    .Include(g => g.SourceDuAn)
    .Include(g => g.TargetDuAn)
        .ThenInclude(t => t.DanhSachNguonVon)
    .ToListAsync();
```

#### Cách khắc phục:
Chỉ lấy các liên kết gộp liên quan đến các dự án xuất hiện trong kỳ báo cáo (`projectIds` đã lấy ở bước trước):
```csharp
// ĐỀ XUẤT FIX:
var targetProjectIds = projects.Select(p => p.Id).ToHashSet();

var relevantGopLinks = await _context.DuAnGopLinks
    .AsNoTracking()
    .Where(g => targetProjectIds.Contains(g.TargetDuAnId) || (g.SourceDuAnId.HasValue && targetProjectIds.Contains(g.SourceDuAnId.Value)))
    .Include(g => g.SourceDuAn)
    .Include(g => g.TargetDuAn)
        .ThenInclude(t => t.DanhSachNguonVon)
    .ToListAsync();
```

---

### 3. [🟡 Cao] Quét toàn bộ dữ liệu thanh toán trong Báo cáo Đầu tư
* **Vị trí**: `ReportService.cs` (Dòng ~221 – 241)
* **Phương thức**: `GetInvestmentReportAsync`

#### Vấn đề hiện tại:
Báo cáo đầu tư đã lọc ra danh sách `projectsData` (chỉ gồm các dự án trong kỳ và người dùng có quyền xem). Tuy nhiên bước tính lũy kế thanh toán lại truy vấn **toàn bộ các đợt thanh toán của tất cả các dự án trong hệ thống**:
```csharp
// ĐANG LÀM: Quét toàn bộ bảng DotThanhToans của tất cả dự án trong DB
var performedValues = await _context.DotThanhToans
    .AsNoTracking()
    .Where(dt => dt.IsPaid 
        && dt.HopDong != null 
        && dt.HopDong.IsActive 
        && !dt.HopDong.IsDeleted 
        && dt.HopDong.DuAnId.HasValue) // KHÔNG lọc theo dự án đang xét!
    .Select(...)
    .GroupBy(x => x.DuAnId)
    .ToDictionaryAsync(...);
```

#### Cách khắc phục:
Truyền danh sách `projectIds` đã lọc vào điều kiện truy vấn để Database chỉ quét và tính tổng cho đúng các dự án cần hiển thị:
```csharp
// ĐỀ XUẤT FIX:
var targetDuAnIds = projectsData.Select(p => p.Id).ToList();

var performedValues = await _context.DotThanhToans
    .AsNoTracking()
    .Where(dt => dt.IsPaid 
        && dt.HopDong != null 
        && dt.HopDong.IsActive 
        && !dt.HopDong.IsDeleted 
        && dt.HopDong.DuAnId.HasValue
        && targetDuAnIds.Contains(dt.HopDong.DuAnId.Value)) // THÊM ĐIỀU KIỆN NÀY
    .Select(dt => new
    {
        DuAnId = dt.HopDong.DuAnId!.Value,
        PaymentDate = dt.NgayThanhToanThucTe ?? dt.NgayThanhToan ?? dt.CreatedAt,
        dt.GiaTriThanhToan
    })
    .GroupBy(x => x.DuAnId)
    .Select(g => new
    {
        DuAnId = g.Key,
        KyTruoc = g.Where(x => x.PaymentDate < startOfPeriod).Sum(x => x.GiaTriThanhToan),
        TrongKy = g.Where(x => x.PaymentDate >= startOfPeriod && x.PaymentDate <= endOfPeriod).Sum(x => x.GiaTriThanhToan)
    })
    .ToDictionaryAsync(x => x.DuAnId, x => x);
```

---

### 4. [🟡 Cao] Nạp toàn bộ Entity đồ sộ thay vì dùng DTO Projection
* **Vị trí**:
  - `GetContractPaymentReportAsync` (Dòng ~1157 – 1218)
  - `GetTheoDoiHopDongReportAsync` (Dòng ~1751 – 1811)
  - `GetTienDoThanhToanDuAnThauReportAsync` (Dòng ~4365 – 4401)

#### Vấn đề hiện tại:
Truy vấn sử dụng nhiều `.Include()` lồng nhau (`HopDong` -> `DotThanhToans`, `DuAn`, `GoiThau`, `NhaThau`), sau đó nạp toàn bộ cấu trúc Entity vào RAM rồi chạy vòng lặp C# để tính tổng:
```csharp
var query = _context.HopDongs
    .Include(h => h.DotThanhToans)
    .Include(h => h.DuAn)
    .Include(h => h.GoiThau)
    .Include(h => h.NhaThau)
    ...
var contracts = await query.ToListAsync(); // Tạo hàng ngàn Entity object trên RAM
```

#### Cách khắc phục:
Chuyển sang dùng `.Select()` (Projection) để EF Core chỉ SELECT đúng các cột cần dùng và để SQL Server / PostgreSQL thực hiện `SUM()` trực tiếp trên Database:
```csharp
// ĐỀ XUẤT FIX (Ví dụ cho HopDong & DotThanhToans):
var contractSummaries = await query
    .Select(h => new
    {
        h.Id,
        h.Code,
        h.SoHopDong,
        h.Name,
        h.GiaTriHopDong,
        DuAnCode = h.DuAn != null ? h.DuAn.Code : null,
        DuAnName = h.DuAn != null ? h.DuAn.Name : null,
        NhaThauName = h.NhaThau != null ? h.NhaThau.Name : null,
        // Tổng hợp tiền thanh toán ngay trong câu SQL:
        TongDaThanhToan = h.DotThanhToans.Where(d => d.IsPaid).Sum(d => (decimal?)d.GiaTriThanhToan) ?? 0m,
        PhaiThanhToanTrongNam = h.DotThanhToans
            .Where(d => (d.NgayThanhToan.HasValue && d.NgayThanhToan.Value.Year == year) || (!d.NgayThanhToan.HasValue && d.CreatedAt.Year == year))
            .Sum(d => (decimal?)d.GiaTriThanhToan) ?? 0m,
        DaThanhToanTrongNam = h.DotThanhToans
            .Where(d => d.IsPaid && ((d.NgayThanhToanThucTe ?? d.NgayThanhToan).HasValue && (d.NgayThanhToanThucTe ?? d.NgayThanhToan)!.Value.Year == year))
            .Sum(d => (decimal?)d.GiaTriThanhToan) ?? 0m
    })
    .ToListAsync();
```
*Lợi ích*: Giảm tới 70-80% lượng RAM sử dụng và giảm băng thông truyền dữ liệu giữa Database và Backend.

---

### 5. [🔵 Trung bình] Tối ưu Subquery Phân quyền & Chỉ mục (Indexes)
* **Vị trí**: Các đoạn kiểm tra quyền User trong `ReportService.cs` (Ví dụ dòng ~179 – 183)

#### Vấn đề:
Điều kiện lọc quyền sinh ra nhiều `EXISTS (SELECT 1 ...)` lồng nhau:
```csharp
query = query.Where(da => da.CreatedByUserId == currentUser.Id || da.ChuDuAnId == currentUser.Id
    || _context.UserPermissions.Any(up => up.UserId == currentUser.Id && up.DuAnId == da.Id)
    || _context.CongViecNguoiLienQuans.Any(...)
    || ...);
```

#### Đề xuất tối ưu:
1. **Lấy trước tập ID được cấp quyền (Pre-fetch ID set)** nếu user không phải Admin:
   ```csharp
   var allowedDuAnIds = await _context.UserPermissions
       .Where(up => up.UserId == currentUser.Id)
       .Select(up => up.DuAnId)
       .ToListAsync();
   ```
2. **Bổ sung Database Indexes**: Đảm bảo các bảng sau có Index để hỗ trợ các câu truy vấn báo cáo:
   - Bảng `DotThanhToans`: Index trên `(HopDongId, IsPaid)` và `NgayThanhToanThucTe`.
   - Bảng `HopDongs`: Index trên `(DuAnId, IsActive, IsDeleted)` và `(GoiThauId)`.
   - Bảng `Licenses`: Index trên `(NgayKetThuc, TrangThai)`.
   - Bảng `DuAnGopLinks`: Index trên `TargetDuAnId` và `SourceDuAnId`.
