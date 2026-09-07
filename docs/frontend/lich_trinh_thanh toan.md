Thiết kế lại màn hình **“Lịch trình thanh toán”** hiện tại từ dạng timeline/card sang **dạng bảng (Data Table)**.

### Mục tiêu

Ưu tiên **dễ đọc, dễ scan, so sánh nhanh các đợt thanh toán**. Không cần giữ timeline theo tháng.

### 1. Giữ nguyên phần tổng quan tài chính

Giữ panel **“Tổng quan tài chính”** bên phải như hiện tại, gồm:

* Tổng kinh phí hợp đồng
* Đã thanh toán
* Còn phải thanh toán
* Đồng tiền giải ngân
* Tổng số đợt

Có thể tinh gọn spacing để bảng có thêm không gian hiển thị.

### 2. Chuyển danh sách đợt thanh toán thành Data Table

Bỏ hoàn toàn timeline và các card từng đợt.

Tạo bảng với các cột:

| Cột             | Nội dung                             |
| --------------- | ------------------------------------ |
| Đợt             | Đợt 1, Đợt 2...                      |
| Ngày thanh toán | `16/08/2026` + dòng phụ `Còn 3 ngày` |
| Nội dung        | Ví dụ: `Tạm ứng License`             |
| Hợp đồng        | `Hợp đồng C-Agent`                   |
| Số tiền         | `2.000.000 đ`                        |
| Tỷ lệ           | `50% giá trị HĐ`                     |
| Trạng thái      | `Chờ thanh toán`, `Đã thanh toán`    |
| Điều kiện       | Điều kiện thanh toán nếu có          |
| Thao tác        | Menu `⋮`                             |

### 3. Hierarchy và typography

* **Số tiền:** font-weight 600, căn phải.
* **Nội dung:** font-weight 500 hoặc 600.
* **Ngày:** ưu tiên hiển thị rõ ràng, dòng `Còn X ngày` dùng text nhỏ hơn và secondary.
* **Hợp đồng:** dùng màu primary nhưng không quá nổi.
* **Trạng thái:** sử dụng badge nhỏ, màu cam cho `Chờ thanh toán`, màu xanh cho `Đã thanh toán`.
* **Label/header của bảng:** không bold quá mức; dùng `font-weight: 500–600`.
* Không sử dụng quá nhiều màu hoặc font-weight đậm.
* Giữ visual style hiện tại của hệ thống: **clean, enterprise, tối giản, dễ scan**.

### 4. Filter phía trên bảng

Giữ:

* `Tất cả (3)`
* `Chờ thanh toán (3)`
* `Đã thanh toán (0)`
* Filter `Tất cả hợp đồng (2)`
* Nút `+ Thêm đợt`

Các filter phải nằm **ngay phía trên bảng**, có spacing hợp lý và không chiếm quá nhiều chiều cao.

### 5. Ngày sắp đến hạn

Các đợt thanh toán gần đến hạn cần được nhận biết nhanh:

* `Còn 3 ngày`: màu cảnh báo nhẹ.
* Không dùng background màu quá mạnh.
* Nếu đã quá hạn: hiển thị `Quá hạn X ngày` bằng màu đỏ.
* Nếu còn nhiều ngày: dùng màu secondary bình thường.

### 6. Điều kiện thanh toán

Không để nội dung điều kiện dài làm bảng quá rộng.

Ví dụ:
`Sau khi ký biên bản nghiệm thu`

Nếu quá dài:

* truncate bằng `...`
* hover/tooltip để xem đầy đủ.

### 7. Thao tác

Không đặt nút **“Xác nhận thanh toán”** lớn trong từng dòng như giao diện hiện tại.

Thay bằng menu `⋮`:

* Xem chi tiết
* Chỉnh sửa
* Xác nhận thanh toán
* Xóa

Chỉ hiển thị các action mà người dùng hiện tại có quyền thực hiện.

### 8. Responsive và chiều rộng

Ưu tiên các cột quan trọng:

**Ngày → Nội dung → Hợp đồng → Số tiền → Trạng thái**

Các cột phụ như `Điều kiện` có thể thu nhỏ hoặc truncate.

Không để bảng bị dồn chật. Nếu viewport không đủ rộng, cho phép **horizontal scroll nhẹ** thay vì làm chữ quá nhỏ.

### 9. Empty state

Nếu chưa có đợt thanh toán:

* Hiển thị empty state đơn giản.
* Nội dung: `Chưa có đợt thanh toán`
* Có CTA `+ Thêm đợt thanh toán`.

### 10. Quan trọng

Không redesign toàn bộ page.

**Chỉ thay đổi khu vực “Lịch trình thanh toán” từ timeline/card → Data Table.**

Giữ nguyên:

* Sidebar
* Header
* Breadcrumb
* Thông tin dự án
* Tabs
* Tổng quan tài chính
* Màu sắc và design system hiện tại

Kết quả cuối cùng phải tạo cảm giác **một màn hình quản lý thanh toán của hệ thống enterprise**, ưu tiên **scan nhanh, so sánh các đợt thanh toán và thao tác nhanh**, không màu mè và không sử dụng quá nhiều font bold.
