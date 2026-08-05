# Báo cáo Đánh giá Độ phức tạp Thuật toán & Logic Nghẽn Hiệu năng ($O(N^2)$)

Tài liệu này chi tiết hóa các điểm có độ phức tạp thuật toán cao ($O(N^2)$ hoặc tệ hơn), các vòng lặp truy vấn lồng nhau và các phương án cải thiện hiệu năng tính toán tại Backend.

---

## 1. Logic cập nhật Ngân sách Dự án (`DuAnService.cs`)

### Mô tả vấn đề
Trong hàm `AddAdjustmentAsync` (thực hiện thêm điều chỉnh hạn mức dự án), hệ thống cập nhật lại ngân sách của tất cả các dự án triển khai có liên kết tới dự án nguồn này:
```csharp
// Load toàn bộ dự án triển khai có trong hệ thống lên bộ nhớ RAM
var implementationProjects = await DbSet.Where(da => da.LoaiDuAn == 2 && da.NguonDuAnIds != null).ToListAsync();

foreach (var ip in implementationProjects)
{
    // Cắt chuỗi string và kiểm tra cục bộ trên bộ nhớ
    var sourceIds = ip.NguonDuAnIds!.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                                   .ToList();
    if (sourceIds.Contains(id))
    {
        // TRUY VẤN DATABASE TRONG VÒNG LẶP (N+1 Queries)
        var sourceProjects = await DbSet.Include(da => da.DieuChinhs)
                                        .Where(da => sourceIds.Contains(da.Id))
                                        .ToListAsync();
        
        var goiThauBudgetsSum = await DbContext.GoiThaus
            .Where(gt => gt.DuAnId == ip.Id)
            .SumAsync(gt => gt.GiaTriGoiThau);
        ...
    }
}
```

### Đánh giá độ phức tạp
* **Độ phức tạp tính toán (CPU & RAM):** $O(M \times K)$ với $M$ là tổng số dự án triển khai trong hệ thống và $K$ là số lượng dự án nguồn được liên kết. Khi hệ thống vận hành lâu dài và số lượng dự án tăng lên, việc tải toàn bộ dữ liệu dự án lên RAM để duyệt qua và parse chuỗi sẽ gây lãng phí lớn về bộ nhớ và tài nguyên xử lý.
* **Độ phức tạp truy vấn (Database):** Trong trường hợp xấu nhất, nếu hầu hết các dự án triển khai đều chứa dự án nguồn đó, hệ thống sẽ thực hiện $2 \times M$ câu truy vấn database độc lập bên trong vòng lặp.

### Giải pháp khắc phục
* Thay vì tải toàn bộ danh sách lên RAM, sử dụng cơ chế truy vấn `Like` của SQL để lọc trực tiếp ở tầng Database:
  ```csharp
  var targetIdString = id.ToString();
  var implementationProjects = await DbSet
      .Where(da => da.LoaiDuAn == 2 && da.NguonDuAnIds != null && EF.Functions.Like(da.NguonDuAnIds, $"%{targetIdString}%"))
      .ToListAsync();
  ```
  Cách này giúp lọc chính xác các dự án triển khai bị ảnh hưởng và chỉ thực hiện vòng lặp trên các bản ghi đó.
* Gom các câu lệnh tính toán ngân sách và gói thầu lại thành truy vấn gộp (Batch Query) thay vì SELECT trong vòng lặp.

---

## 2. Background Job quét hạn xác nhận (`StakeholderConfirmationCheckWorker.cs`)

### Mô tả vấn đề
Tiến trình quét nền hàng ngày thực hiện duyệt qua toàn bộ các thành viên liên quan quá hạn xác nhận:
```csharp
var expiredRecords = await dbContext.CongViecNguoiLienQuans...ToListAsync();

foreach (var record in expiredRecords)
{
    ...
    // Truy vấn kiểm tra thông báo trùng lặp
    var alreadyNotified = await dbContext.Notifications
        .AnyAsync(n => n.UserId == record.UserId && n.Link == link && n.Title.Contains("Quá hạn"));

    // Truy vấn nạp lại thông tin Task
    var task = await dbContext.CongViecGoiThaus
        .Include(t => t.CreateUser)
        .Include(t => t.ModifiedUser)
        .FirstOrDefaultAsync(t => t.Id == record.CongViecGoiThauId);
    ...
}
```

### Đánh giá độ phức tạp
* **Độ phức tạp:** $O(E)$ với $E$ là số lượng bản ghi quá hạn. Vì mỗi vòng lặp thực hiện 2 câu truy vấn database độc lập, nếu có 500 công việc quá hạn, job này sẽ thực hiện 1000 câu truy vấn DB liên tiếp. Việc này gây quá tải kết nối và ảnh hưởng trực tiếp đến tốc độ phản hồi của API người dùng đang truy cập.

### Giải pháp khắc phục
* Sử dụng phép `JOIN` hoặc truy vấn gom nhóm trước vòng lặp để lấy toàn bộ thông tin Task và trạng thái thông báo cần thiết trong 1 câu SQL duy nhất.
* Thực hiện xử lý logic cập nhật trạng thái và tạo thông báo theo lô (Bulk Update / Bulk Insert).

---

## 3. Độ phức tạp Join trên Database ($O(T_1 \times T_2)$) do thiếu Index

### Mô tả vấn đề
* Khi thiết kế cơ sở dữ liệu quan hệ, các truy vấn ghép bảng (`JOIN`) hoặc tìm kiếm dữ liệu qua khóa ngoại (Foreign Key) mà không được đánh chỉ mục (`Index`) sẽ buộc hệ thống quản trị CSDL sử dụng thuật toán **Nested Loop Join** hoặc **Table Scan**.
* Độ phức tạp trong trường hợp này có thể lên tới $O(T_1 \times T_2)$ (với $T_1$, $T_2$ là kích thước của 2 bảng được liên kết).

### Vị trí cần bổ sung Index
* Cần kiểm tra và tạo chỉ mục bổ sung cho các trường khóa ngoại hay được tìm kiếm hoặc JOIN:
  * Bảng `CongViecGoiThaus`: Trường `GoiThauId`.
  * Bảng `AuditLogs`: Trường `Username` và `Timestamp`.
