# Feature Spec: Người liên quan & Phản hồi 24h cho CongViecGoiThau

**Ngày tạo:** 2026-07-21  
**Trạng thái:** Đã duyệt thiết kế  

---

## 1. Tổng quan & Mục tiêu

Khi người dùng tạo mới danh sách công việc (hoặc chỉnh sửa công việc lẻ) thuộc Gói thầu, hệ thống cần bổ sung khả năng chọn **"Người liên quan"** (stakeholders/assignees).

Sau khi công việc được tạo thành công:
1. Hệ thống tự động gửi **Notification** tới tất cả những người liên quan được chọn.
2. Mỗi người liên quan có thời hạn **24 giờ** kể từ thời điểm giao việc để thực hiện một trong hai hành động:
   * **Xác nhận** (Bấm nút "Xác nhận đã nhận việc").
   * **Bình luận** (Viết 1 phản hồi/bình luận tại công việc đó).
3. Hệ thống hiển thị **Badge đếm ngược 24h** và trạng thái phản hồi rõ ràng trên Bảng danh sách công việc, Chi tiết công việc và Menu thông báo.

---

## 2. Giao diện & Trải nghiệm Người dùng (UI/UX)

### 2.1. Modal "Thêm danh sách công việc mới" (Batch & Single Form)

* **Top Bar (Chọn người liên quan chung):**
  * Bổ sung ô chọn `Người liên quan chung` (Multi-select dropdown chọn từ danh sách User trong hệ thống).
  * Khi thay đổi danh sách này, toàn bộ các dòng công việc hiện có trong bảng batch bên dưới sẽ tự động được gán cùng danh sách người liên quan đó.
* **Cột "Người liên quan" trong Bảng Batch (`BatchCongViecFormRow`):**
  * Bổ sung cột "Người liên quan" hiển thị dạng Multi-select tags / avatars.
  * Cho phép người dùng chỉnh sửa (thêm/bớt người) riêng cho từng dòng công việc.
* **Form đơn (`SingleCongViecForm`):**
  * Bổ sung trường chọn "Người liên quan" tương tự cho trường hợp tạo lẻ/chỉnh sửa.

### 2.2. Header Notification & Tương tác nhanh

* **Popup Thông báo (`HeaderNotifications.jsx`):**
  * Khi nhận thông báo loại `TASK_ASSIGNED`, thông báo sẽ có kèm 2 nút tương tác nhanh:
    * **[✓ Xác nhận]**: Cho phép người dùng bấm xác nhận trực tiếp từ menu thông báo mà không cần mở trang chi tiết.
    * **[💬 Bình luận]**: Mở nhanh khung nhập bình luận.

### 2.3. Hiển thị Trạng thái & Badge 24h trên Bảng công việc

* **Trạng thái đếm ngược & Badge:**
  * 🟧 **`⏱️ Còn Xh`** (Màu cam): Người dùng chưa tương tác, đang trong thời hạn 24h (X = số giờ còn lại).
  * 🟩 **`✓ Đã xác nhận`** (Màu xanh lá): Người dùng đã bấm nút Xác nhận.
  * 🟦 **`💬 Đã bình luận`** (Màu xanh dương): Người dùng đã gửi 1 bình luận.
  * 🟥 **`⚠️ Quá hạn 24h`** (Màu đỏ): Quá 24 giờ kể từ thời điểm tạo việc mà người dùng chưa bấm xác nhận hoặc chưa gửi bình luận.

---

## 3. Kiến trúc Backend & Cơ sở dữ liệu

### 3.1. Entity `CongViecNguoiLienQuan`

* `Id` (Guid): Primary Key.
* `CongViecGoiThauId` (Guid): Foreign Key tham chiếu tới `CongViecGoiThau`.
* `UserId` (Guid): Foreign Key tham chiếu tới `User`.
* `TrangThaiXacNhan` (string):
  * `Pending`: Chờ phản hồi.
  * `Confirmed`: Đã bấm nút xác nhận.
  * `Commented`: Đã phản hồi bằng bình luận.
  * `Overdue`: Quá hạn 24h (tính toán động dựa trên `HanXacNhanAt`).
* `HanXacNhanAt` (DateTime): Thời hạn phản hồi, mặc định = `CreatedAt + 24 hours`.
* `XacNhanAt` (DateTime?): Thời điểm người dùng thực hiện xác nhận hoặc comment.
* `LoaiXacNhan` (string?): `DirectConfirm` hoặc `Comment`.

### 3.2. API Endpoints & Logic Tự động hóa

1. **Tạo mới / Cập nhật Công việc:**
   * Cập nhật DTO `CreateCongViecGoiThauDto` và `BatchCreateCongViecGoiThauDto` tiếp nhận mảng `NguoiLienQuanIds`.
   * Lưu các bản ghi `CongViecNguoiLienQuan` tương ứng.
   * Gửi Notification qua `NotificationService` & `NotificationHub` (SignalR).
2. **Xác nhận nhanh:**
   * `POST /api/CongViecGoiThau/{id}/xac-nhan`: Cập nhật `TrangThaiXacNhan = Confirmed`, `XacNhanAt = DateTime.UtcNow`.
3. **Tự động ghi nhận khi Comment:**
   * Trong `CommentCongViecGoiThauService`: Khi tạo comment mới, kiểm tra xem `UserId` đó có thuộc danh sách `CongViecNguoiLienQuan` của công việc đó hay không. Nếu có và `TrangThaiXacNhan == Pending`, tự động chuyển thành `Commented`.

---

## 4. Kế hoạch Kiểm thử (Verification Plan)

1. **Tạo danh sách công việc hàng loạt:**
   * Chọn người liên quan chung -> Kiểm tra xem các dòng bên dưới có nhận đúng danh sách hay không.
   * Chỉnh sửa người liên quan riêng ở 1 dòng -> Kiểm tra xem dòng đó có lưu đúng danh sách riêng hay không.
2. **Gửi Notification & Đếm ngược 24h:**
   * Đăng nhập bằng tài khoản người liên quan -> Kiểm tra menu Thông báo hiển thị đúng nút [✓ Xác nhận].
   * Bấm nút [✓ Xác nhận] -> Kiểm tra trạng thái chuyển thành `✓ Đã xác nhận`.
3. **Tự động chuyển trạng thái khi Comment:**
   * Nhập bình luận ở công việc được giao -> Kiểm tra badge chuyển sang `💬 Đã bình luận`.
4. **Kiểm tra Quá hạn 24h:**
   * Thử nghiệm trường hợp `HanXacNhanAt` đã qua 24h -> Badge đổi sang `⚠️ Quá hạn 24h`.
