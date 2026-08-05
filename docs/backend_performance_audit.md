# Báo cáo Đánh giá Hiệu năng & Truy vấn N+1 - Backend

Tài liệu này tổng hợp các điểm nghẽn hiệu năng, lỗi truy vấn N+1 và các đề xuất tối ưu hóa trong phần Backend (.NET Web API / EF Core) của hệ thống Quản lý Dự án.

---

## 1. Dịch vụ Công việc Gói thầu (`CongViecGoiThauService.cs`)

### Vấn đề: N+1 Queries và SaveChanges lặp lại khi gửi thông báo
* **Mã nguồn liên quan:** `SendStakeholderNotificationsAsync` và `SendNotificationsToUsersAsync`.
* **Mô tả:**
  * Khi lưu các công việc mới, hàm `SendStakeholderNotificationsAsync` duyệt qua từng công việc trong vòng lặp và thực hiện:
    ```csharp
    var users = await DbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
    ```
    Đây là một truy vấn database trên mỗi công việc (N truy vấn SELECT).
  * Trong hàm `SendNotificationsToUsersAsync`, hệ thống thực hiện `DbContext.Notifications.Add(...)` và gọi `await DbContext.SaveChangesAsync();` **ngay trong vòng lặp**.
  * Lệnh `SaveChangesAsync()` kích hoạt cơ chế audit log và tự động truy vấn tìm thông tin User hiện tại từ DB (`var user = await Users.FirstOrDefaultAsync(...)`), làm tăng thêm số lượng câu truy vấn database.
* **Đề xuất tối ưu:**
  * Thu thập tất cả `userIds` của mọi công việc cần xử lý trước, thực hiện truy vấn bảng `Users` duy nhất 1 lần để lấy thông tin đưa vào Dictionary/Map.
  * Gom tất cả các đối tượng `Notification` cần tạo mới vào một danh sách, sau đó dùng `AddRange` và chỉ gọi `SaveChangesAsync()` một lần duy nhất ở ngoài vòng lặp.

---

## 2. Dịch vụ Gói thầu (`GoiThauService.cs`)

### Vấn đề: N+1 Queries khi tạo mới hàng loạt gói thầu (`CreateRangeAsync`)
* **Mã nguồn liên quan:** Vòng lặp gọi `CreateEntityInternalAsync` và reload dữ liệu sau khi lưu.
* **Mô tả:**
  * **Trong lúc tạo thực thể (`CreateEntityInternalAsync`):** Với mỗi gói thầu trong danh sách đầu vào, hệ thống thực hiện các thao tác:
    * `DbContext.DuAns.Include(...).Include(...).FirstOrDefaultAsync(...)` để tìm kiếm dự án liên kết.
    * `DbContext.UserPermissions.AnyAsync(...)` để kiểm tra quyền tạo của User.
    * `DbSet.AnyAsync(...)` để kiểm tra tính duy nhất của mã gói thầu (`Code`).
    $\Rightarrow$ Gây ra hiện tượng chạy $3 \times N$ câu truy vấn database độc lập.
  * **Sau khi lưu thành công (`SaveChangesAsync`):** Hệ thống lại thực hiện duyệt qua từng gói thầu vừa tạo để nạp lại dữ liệu:
    ```csharp
    foreach (var entity in entities)
    {
        var reloaded = await DbSet.Include(gt => gt.DuAn).FirstOrDefaultAsync(gt => gt.Id == entity.Id);
        result.Add(Mapper.Map<GoiThauDto>(reloaded));
    }
    ```
    Tạo ra $N$ câu truy vấn SELECT dư thừa.
* **Đề xuất tối ưu:**
  * Gom tất cả `DuAnId` và `Code` của các gói thầu gửi lên, truy vấn danh sách Dự án và kiểm tra tính trùng lặp mã bằng một câu lệnh duy nhất trước vòng lặp.
  * Nạp lại dữ liệu sau khi lưu bằng một câu lệnh `Where(gt => ids.Contains(gt.Id))` duy nhất:
    ```csharp
    var ids = entities.Select(e => e.Id).ToList();
    var reloadedEntities = await DbSet.Include(gt => gt.DuAn).Where(gt => ids.Contains(gt.Id)).ToListAsync();
    ```

---

## 3. Bản ghi Thay đổi Database (`AppDbContext.cs`)

### Vấn đề: Truy vấn thừa thông tin User trong `SaveChangesAsync`
* **Mô tả:**
  Mỗi lần gọi `SaveChangesAsync()`, hệ thống tự động tìm kiếm thông tin của User hiện tại từ database để gắn vào các trường audit (`CreateUserId`, `ModifiedUserId` cho bảng `CongViecGoiThau`):
  ```csharp
  var user = await Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
  ```
  Nếu hệ thống thực hiện nhiều thao tác lưu riêng lẻ trong một Request, bảng `Users` sẽ liên tục bị truy vấn `SELECT`.
* **Đề xuất tối ưu:**
  * Lưu thông tin `UserId` trong các Claims của token JWT và lấy thông qua `ICurrentUserService` để tránh phải SELECT từ DB mỗi khi lưu dữ liệu.

---

## 4. Dịch vụ Cơ bản (`DbCrudService.cs`)

### Vấn đề: Kiểm tra tính duy nhất của mã thực thể trong Batch (`CreateRangeAsync`)
* **Mô tả:**
  Khi tạo mới hàng loạt, hàm `CreateRangeAsync` dùng chung duyệt qua từng thực thể và gọi:
  ```csharp
  await EnsureCodeIsUniqueAsync(entity.Code);
  ```
  Hàm này sẽ thực hiện câu lệnh `AnyAsync(...)` trên database cho từng thực thể.
* **Đề xuất tối ưu:**
  * Lấy ra toàn bộ danh sách `Code` từ DTO đầu vào, chạy 1 câu truy vấn database để lấy ra các `Code` đã tồn tại trong DB, sau đó kiểm tra cục bộ trên bộ nhớ RAM.

---

## 5. Dịch vụ Hợp đồng (`HopDongService.cs`)

### Vấn đề: Kiểm tra quyền hạn dự án trong vòng lặp (`CreateRangeAsync`)
* **Mô tả:**
  Khi xác thực quyền tạo hợp đồng trong danh sách dự án liên kết:
  ```csharp
  foreach (var project in projects)
  {
      if (project.CreatedByUserId != currentUser.Id)
      {
          var hasCreatePerm = await DbContext.UserPermissions.AnyAsync(up => ...);
          ...
      }
  }
  ```
  Dẫn đến việc gọi câu truy vấn DB liên tục cho từng dự án.
* **Đề xuất tối ưu:**
  * Sử dụng truy vấn gom nhóm để lấy toàn bộ các dự án mà User có quyền tạo trong một lần:
    ```csharp
    var allowedProjectIds = await DbContext.UserPermissions
        .Where(up => up.UserId == currentUser.Id && duAnIds.Contains(up.DuAnId) && up.Permission.Code == "CREATE")
        .Select(up => up.DuAnId)
        .ToListAsync();
    ```
