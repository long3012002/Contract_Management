Hãy tinh chỉnh UI/UX màn hình "Chi tiết hợp đồng" hiện tại.

MỤC TIÊU:
Giữ nguyên layout, cấu trúc, chức năng và business logic hiện tại.
Không redesign lại màn hình.

Mục tiêu chính là:
- Giảm visual noise.
- Giảm số lượng font bold.
- Giảm cảm giác "card trong card".
- Giảm density của sidebar.
- Làm hierarchy thông tin rõ hơn.
- Ưu tiên khả năng scan nhanh.
- Giữ phong cách enterprise/internal system, sạch và chuyên nghiệp.
- Không thêm màu sắc hoặc decoration không cần thiết.

==================================================
1. TYPOGRAPHY
==================================================

Đây là ưu tiên quan trọng nhất.

Hiện tại màn hình đang có hơi nhiều text bold/semibold.

Thiết lập hierarchy rõ ràng:

LEVEL 1:
- Tên hợp đồng.
- Các con số quan trọng.

Ví dụ:
"Hợp đồng C-Agent"
"4.000.000 đ"

→ Có thể dùng font-weight 600.

LEVEL 2:
- Tên đợt thanh toán.
- Tên công ty/thành viên.
- Các section heading.

→ font-weight 500–600.

LEVEL 3:
- Label.
- Metadata.
- Mã hợp đồng.
- Ngày tháng.
- Mô tả.

→ font-weight 400–500.

Không sử dụng bold cho toàn bộ một dòng.

Ví dụ hiện tại:

"Mã HĐ: HD-2026-2 · Ngày ký: 21/08/2026 · Hết hạn: 18/12/2026"

Hãy đảm bảo:
- Label nhẹ.
- Value có emphasis vừa phải.
- Metadata không bold toàn bộ.

==================================================
2. SECTION HEADING
==================================================

Các heading hiện tại như:

"CÁC BÊN KÝ KẾT HỢP ĐỒNG"
"THÀNH VIÊN LIÊN DANH (2)"
"HỒ SƠ ĐÍNH KÈM"

Không nên dùng ALL CAPS.

Đổi thành:

"Các bên ký kết hợp đồng"
"Thành viên liên danh (2)"
"Hồ sơ đính kèm"

Dùng:
- font-size vừa phải.
- font-weight 600.
- text màu chính.
- không cần letter-spacing lớn.

Mục tiêu là heading trông nhẹ và hiện đại hơn.

==================================================
3. GIẢM CẢM GIÁC "CARD TRONG CARD"
==================================================

Không xóa toàn bộ card hiện tại.

Giữ card cho các khu vực chính:

- Thông tin hợp đồng.
- Tổng quan thanh toán.
- Danh mục hàng hóa & dịch vụ.
- Hồ sơ đính kèm.
- Các bên ký kết.

Nhưng bên trong card:

Không tạo quá nhiều card nhỏ nếu không cần thiết.

Đặc biệt khu vực:

"Các bên ký kết hợp đồng"

nên ưu tiên:
- spacing.
- divider.
- background nhẹ.
- border nhẹ.

thay vì nhiều nested card.

Mục tiêu:

CARD
  Section
    Content

thay vì:

CARD
  CARD
    CARD
      Content

==================================================
4. TỔNG QUAN GIÁ TRỊ HỢP ĐỒNG
==================================================

Giữ layout hiện tại:

Tổng giá trị HĐ
Đã thanh toán
Còn lại phải trả

Nhưng tăng hierarchy nhẹ cho giá trị.

Ví dụ:

Tổng giá trị HĐ
4.000.000 đ

Đã thanh toán
0 đ

Còn lại phải trả
4.000.000 đ

Label:
- font-size nhỏ hơn.
- muted.

Value:
- font-size lớn hơn label khoảng 1–2 cấp.
- font-weight 600.
- không cần quá lớn.

Không biến khu vực này thành dashboard KPI quá màu mè.

==================================================
5. TIẾN ĐỘ THANH TOÁN
==================================================

Giữ progress bar hiện tại.

Nhưng khi progress = 0%:

- Không làm progress bar quá nổi bật.
- Track nên rất nhẹ.
- "0%" có thể dùng muted text.

Khi progress > 0:
- hiển thị progress rõ ràng.
- vẫn giữ màu sắc tiết chế.

Không thêm animation.

==================================================
6. PAYMENT INSTALLMENTS
==================================================

Giữ nguyên timeline các đợt thanh toán vì pattern hiện tại dễ hiểu.

Ví dụ:

Tạm ứng License
HD-2026-2 · Đợt 1
Dự kiến: 16/8/2026 · Chờ thanh toán

2.000.000 đ
[Xác nhận trả]

Tinh chỉnh:

- Tên đợt: semibold.
- Metadata: regular/muted.
- Trạng thái: màu nhẹ.
- Số tiền: semibold.
- Button "Xác nhận trả": nhỏ gọn hơn.

Không làm button quá nổi bật so với nội dung.

Chỉ action chính mới được visual emphasis.

==================================================
7. PAYMENT TABS
==================================================

Giữ:

Tất cả
Chờ thanh toán
Đã trả
Quá hạn

Nhưng active state cần rõ ràng hơn.

Ví dụ:

[Tất cả (2)]  Chờ thanh toán (2)  Đã trả (0)  Quá hạn (0)

Active tab:
- background rất nhẹ
HOẶC
- underline nhẹ
- font-weight 500–600

Inactive tab:
- muted.
- regular.

Không sử dụng nhiều màu.

==================================================
8. SIDEBAR - HỒ SƠ ĐÍNH KÈM
==================================================

Giữ section hiện tại.

Nếu không có file:

"Không có tệp đính kèm"

Hiển thị nhẹ nhàng, không cần card riêng.

Nếu có file:
- filename.
- file type/icon.
- size nếu có.
- action xem/tải.

Không tạo UI quá nặng cho empty state.

==================================================
9. SIDEBAR - CÁC BÊN KÝ KẾT
==================================================

Đây là khu vực cần giảm density.

Giữ thông tin:

- Chủ đầu tư.
- Liên danh.
- Thành viên liên danh.

Nhưng typography phải thoáng hơn.

Không viết toàn bộ heading bằng uppercase.

Thông tin quan trọng:
- Tên đơn vị.
- Vai trò.

Thông tin phụ:
- MST.
- Địa chỉ.
- SĐT.
- Email.
- Tài khoản.

Thông tin phụ nên có visual weight thấp hơn.

==================================================
10. THÀNH VIÊN LIÊN DANH
==================================================

Hiện tại mỗi thành viên đang hiển thị quá nhiều thông tin cùng lúc.

Không nhất thiết phải hiển thị tất cả thông tin ở trạng thái mặc định.

Ưu tiên hiển thị:

Tên công ty
MST
Đại diện
SĐT

Các thông tin ít quan trọng hơn:

- Địa chỉ.
- Email.
- Tài khoản thanh toán.

Có thể:
- giảm font-size.
- giảm contrast.
- hoặc gom vào một khu vực chi tiết nếu component hiện tại đã hỗ trợ.

Nếu thay đổi interaction thì chỉ làm ở mức đơn giản:

"Xem chi tiết"

Không xây dựng accordion phức tạp nếu không cần.

==================================================
11. SIDEBAR DENSITY
==================================================

Sidebar phải dễ scan.

Sắp xếp hierarchy:

Tên công ty
↓
MST
↓
Thông tin liên hệ

Không để mọi dòng đều có icon + bold + text đậm.

Các icon:
- nhỏ.
- muted.
- chỉ dùng khi thực sự giúp scan.

Không cần icon cho mọi thông tin nếu làm UI rối hơn.

==================================================
12. BUTTONS
==================================================

Rà soát toàn bộ button trên màn hình.

Chỉ giữ primary emphasis cho action quan trọng nhất.

Ví dụ:

Primary:
"Thanh lý HĐ" hoặc action chính theo business logic hiện tại.

Secondary:
"Xem/Tải HĐ"
"Xem Dự án"

Tertiary:
"Các action nhỏ khác"

Không để tất cả button đều có visual weight ngang nhau.

Button:
- height vừa phải.
- radius đồng nhất.
- icon nhỏ.
- text không quá bold.

==================================================
13. BADGE / STATUS
==================================================

Giữ các badge:

"Đang thực hiện"
"Còn 128 ngày"
"Chờ thanh toán"
"Đã thanh toán"
"Quá hạn"

Nhưng giảm độ nổi.

Badge nên:
- background rất nhẹ.
- border nhẹ nếu cần.
- font-weight 500.
- font-size nhỏ.

Không dùng badge quá saturated.

Status phải dễ nhận biết nhưng không chiếm attention hơn tên hợp đồng.

==================================================
14. SPACING
==================================================

Rà soát spacing toàn màn hình.

Ưu tiên:
- section spacing rõ.
- content spacing vừa phải.
- tránh quá sát.
- nhưng cũng không tạo khoảng trắng quá lớn.

Các section khác nhau nên được phân biệt bằng:
- khoảng cách.
- divider.
- heading.

Không nhất thiết phải thêm border cho mọi thứ.

==================================================
15. MÀU SẮC
==================================================

Giữ palette hiện tại của hệ thống/Co-opBank.

Không thêm màu mới.

Chỉ sử dụng màu mạnh cho:
- Primary action.
- Status quan trọng.
- Warning/error khi cần.

Phần lớn UI nên sử dụng:
- text primary.
- text secondary.
- muted text.
- border nhẹ.
- background trung tính.

==================================================
16. RESPONSIVE
==================================================

Không phá vỡ layout desktop hiện tại.

Kiểm tra thêm khi:
- màn hình nhỏ hơn.
- sidebar hẹp.
- nội dung bảng/section dài.

Nếu không đủ không gian:
- ưu tiên nội dung chính.
- sidebar có thể chuyển xuống dưới hoặc stack theo layout hiện tại.

Không thêm horizontal scroll không cần thiết.

==================================================
17. QUAN TRỌNG - KHÔNG OVER-DESIGN
==================================================

Không thêm:

- Gradient.
- Shadow mạnh.
- Animation.
- Glassmorphism.
- Card màu sắc.
- Icon decoration.
- KPI dashboard phức tạp.
- Tooltip không cần thiết.
- Accordion phức tạp.
- Modal mới nếu không cần.

Đây là màn hình quản lý hợp đồng doanh nghiệp.

Phong cách mong muốn:

"CLEAN - PROFESSIONAL - INFORMATION DENSE nhưng dễ scan"

==================================================
18. GIỮ NGUYÊN BUSINESS LOGIC
==================================================

Không thay đổi:

- API.
- Data model.
- Business logic.
- Permission.
- Payment logic.
- Contract logic.
- Navigation.
- Các action hiện tại.

Chỉ chỉnh:
- UI.
- UX.
- Typography.
- Spacing.
- Visual hierarchy.
- Component structure nếu cần.

==================================================
19. FINAL CHECK
==================================================

Sau khi hoàn thành hãy tự review lại màn hình và đảm bảo:

1. Không còn quá nhiều text bold.
2. Heading không còn ALL CAPS không cần thiết.
3. Không có quá nhiều card nested.
4. Tên hợp đồng là thông tin nổi bật nhất.
5. Giá trị hợp đồng dễ scan.
6. Payment timeline dễ đọc.
7. Sidebar không bị quá dày.
8. Thông tin thành viên liên danh có hierarchy.
9. Button primary/secondary rõ ràng.
10. UI nhìn nhẹ hơn phiên bản hiện tại nhưng không mất thông tin nghiệp vụ.

Quan trọng nhất:

KHÔNG redesign lại toàn bộ màn hình.

Hãy thực hiện theo hướng "refine existing UI", giữ lại những gì đang tốt và chỉ sửa những điểm thực sự gây visual noise.