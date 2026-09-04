# Redesign DataTable Enterprise UI/UX

## Mục tiêu

Thiết kế lại DataTable theo phong cách Enterprise hiện đại, lấy cảm hứng
từ eOffice của CoopBank.

### 1. Information Density

-   Giảm chiều cao row còn **68--72px**.
-   Giảm vertical padding.
-   Hiển thị khoảng **11--13 dòng** trên màn hình FullHD.
-   Hạn chế khoảng trắng dư thừa.

### 2. Kiểu ngăn cách giữa các dòng

Không dùng border-bottom mặc định.

Mỗi row là một block riêng: - Background: `#FFFFFF` - Border:
`1px solid #E8EDF3` - Border-radius: `8px` - Gap giữa các row: `6–8px` -
Không dùng shadow lớn. - Hover: - Background: `#F8FBFF` - Border-color:
Primary nhẹ - Transition: `150ms`

### 3. Header

-   Background: `#F8FAFC`
-   Font-weight: `500`
-   Font-size: `13px`
-   Text: `gray-600`
-   Giảm chiều cao header.

### 4. Typography

-   Chỉ **Tên dự án** dùng `font-medium`.
-   Các cột khác `font-normal`.
-   Line-height: `1.4`.

### 5. Column spacing

-   Thu nhỏ khoảng cách giữa các cột.
-   Tối ưu việc đọc theo chiều ngang.

### 6. Badge

-   Height: `20px`
-   Font-size: `11px`
-   Border-radius: `8px`
-   Padding nhỏ gọn.

### 7. Tổng vốn

-   Căn phải.
-   Font-medium.
-   Canh thẳng các chữ số.

### 8. Action

-   Giữ icon ba chấm.
-   Bình thường opacity \~60%.
-   Hover mới nổi bật.

### 9. Hover

-   Background rất nhẹ.
-   Có thể thêm border-left 3px màu Primary.

### 10. Khoảng trắng

-   Giảm padding table khoảng 15%.

### 11. Responsive

-   Ưu tiên desktop.
-   Không thay đổi layout desktop.

### 12. Định hướng

Chỉ tinh chỉnh DataTable, không redesign toàn bộ giao diện.

------------------------------------------------------------------------

# Gợi ý triển khai

Ưu tiên: - CSS Grid thay cho `<table>` nếu cần mở rộng. - Sticky
Header. - Virtualization. - Row Selection. - Expand Row. - Drag & Drop.

Mỗi row hoạt động như một "card mỏng", giúp dễ scan dữ liệu và mở rộng
tính năng trong tương lai.
