# Chuẩn hóa Font Weight và Typography Hierarchy toàn bộ Frontend Project

## Mục tiêu

Audit và refactor toàn bộ hệ thống `font-weight` của frontend để giao diện có hierarchy rõ ràng, nhẹ mắt, chuyên nghiệp và nhất quán.

Hiện tại project đang có xu hướng sử dụng quá nhiều `font-bold` cho label, text và các thành phần UI. Đặc biệt các form nghiệp vụ có nhiều input khiến giao diện trở nên nặng và khó scan.

**Không được thực hiện thay đổi máy móc kiểu `font-bold` → `font-medium` trên toàn project.**

Hãy phân tích vai trò của từng text element và áp dụng typography hierarchy phù hợp.

---

# 1. Typography hierarchy chuẩn

Áp dụng hệ thống font-weight sau:

| Vai trò | Font weight | Tailwind | Mục đích |
|---|---:|---|---|
| Display / Page title quan trọng | 700 | `font-bold` | Tiêu đề lớn |
| Modal title | 700 | `font-bold` | Tiêu đề modal |
| Section heading | 600 | `font-semibold` | Phân chia section |
| Card heading | 600 | `font-semibold` | Tiêu đề card |
| Active tab | 600 | `font-semibold` | Trạng thái active |
| Button text | 500–600 | `font-medium` / `font-semibold` | CTA |
| Form label | **500** | `font-medium` | Label input |
| Table column header | 500–600 | `font-medium` / `font-semibold` | Header bảng |
| Important data/value | 500 | `font-medium` | Giá trị cần nhấn |
| Normal body text | 400 | `font-normal` | Nội dung thông thường |
| Input text | 400–500 | `font-normal` / `font-medium` | Giá trị người dùng nhập |
| Placeholder | 400 | `font-normal` | Placeholder |
| Description/helper text | 400 | `font-normal` | Text phụ |
| Caption | 400 | `font-normal` | Thông tin phụ |
| Metadata | 400 | `font-normal` | Ngày tháng, người tạo... |
| Error message | 400–500 | `font-normal` / `font-medium` | Validation |
| Badge/status | 500 | `font-medium` | Trạng thái |

## Nguyên tắc quan trọng

### `font-bold` (`700`) phải được sử dụng có chọn lọc

Không sử dụng `font-bold` cho:

- Form label thông thường
- Placeholder
- Body text
- Description
- Metadata
- Table cell
- Dropdown option
- Helper text
- Các text phụ
- Các label không mang tính heading

`font-bold` chỉ nên xuất hiện ở các heading hoặc nội dung thực sự cần nhấn mạnh.

---

# 2. Form label

Đây là khu vực cần ưu tiên refactor.

Các label như:

```text
Số hợp đồng *
Giá trị hợp đồng (VND) *
Tên hợp đồng *
Dự án *
Gói thầu
Nhà thầu / Đối tác *
Ngày ký HĐ
Ngày hết hạn HĐ
Nội dung HĐ
```

phải sử dụng:

```text
font-weight: 500
```

Tailwind:

```tsx
font-medium
```

Không sử dụng:

```tsx
font-bold
```

cho các form label thông thường.

Ví dụ:

```tsx
<label className="text-sm font-medium">
  Số hợp đồng <span>*</span>
</label>
```

---

# 3. Required indicator

Không làm toàn bộ label thành bold chỉ vì có dấu `*`.

Không:

```tsx
<label className="font-bold">
  Số hợp đồng *
</label>
```

Nên:

```tsx
<label className="font-medium">
  Số hợp đồng <span className="font-medium">*</span>
</label>
```

Nếu design system đang có màu riêng cho required indicator thì giữ nguyên màu đó.

Không thay đổi màu sắc nếu không cần thiết.

---

# 4. Modal

Modal title:

```tsx
font-bold
```

hoặc tương đương `font-weight: 700`.

Ví dụ:

```text
Thêm hợp đồng mới
```

Section title:

```tsx
font-semibold
```

Ví dụ:

```text
Thông tin hợp đồng
Kế hoạch thanh toán
Danh mục hàng hóa, dịch vụ
```

Không sử dụng `font-bold` cho section heading nếu không thực sự cần.

Hierarchy mong muốn:

```text
Thêm hợp đồng mới       → 700

Thông tin hợp đồng      → 600

Số hợp đồng             → 500
```

---

# 5. Tabs

Tab inactive:

```tsx
font-medium
```

Tab active:

```tsx
font-semibold
```

Không sử dụng `font-bold` cho toàn bộ tab.

Ví dụ:

```text
Kế hoạch thanh toán       → 600 active
Danh mục hàng hóa...      → 500 inactive
```

---

# 6. Buttons

Button text không nên mặc định là `font-bold`.

Ưu tiên:

```tsx
font-medium
```

hoặc:

```tsx
font-semibold
```

Tùy mức độ quan trọng của button.

Ví dụ:

```text
+ Thêm đợt       → 500
Hủy              → 500
Thêm             → 500/600
Xóa              → 500/600
```

Không sử dụng `font-bold` cho toàn bộ button trong project.

---

# 7. Table

Table header:

```tsx
font-medium
```

hoặc:

```tsx
font-semibold
```

Tùy thiết kế hiện tại.

Table cell:

```tsx
font-normal
```

Không sử dụng `font-bold` cho toàn bộ dữ liệu trong table.

Chỉ dùng `font-medium` cho những data thực sự quan trọng.

Ví dụ:

```text
STT                    → 500
Số hợp đồng            → 500
Tên hợp đồng            → 400
Nhà thầu                → 400
Giá trị                 → 500
Ngày ký                 → 400
Trạng thái               → 500
```

---

# 8. Badge / Status

Status badge nên sử dụng:

```tsx
font-medium
```

Ví dụ:

```text
Đang thực hiện
Đã hoàn thành
Đã hết hạn
```

Không cần:

```tsx
font-bold
```

trừ khi có lý do UX rõ ràng.

---

# 9. Input / Select / Textarea

Input value:

```tsx
font-normal
```

hoặc `font-medium` nếu giá trị cần nổi bật.

Placeholder:

```tsx
font-normal
```

Tuyệt đối không sử dụng `font-bold` cho placeholder.

Select value:

```tsx
font-normal
```

Dropdown option:

```tsx
font-normal
```

Selected option có thể:

```tsx
font-medium
```

---

# 10. Description / Helper / Metadata

Các text như:

```text
Chưa thiết lập đợt thanh toán nào.
Vui lòng chọn dự án trước.
Cập nhật lần cuối...
Người tạo...
Ngày tạo...
```

nên:

```tsx
font-normal
```

Không sử dụng bold nếu không có lý do đặc biệt.

---

# 11. Dashboard / Card

Card title:

```tsx
font-semibold
```

Primary metric/value:

```tsx
font-semibold
```

Label:

```tsx
font-normal
```

Metadata:

```tsx
font-normal
```

Ví dụ:

```text
Tổng số hợp đồng       → 400
```

Trong đó:

```text
Tổng số hợp đồng → 400
```

`400` có thể dùng `font-semibold`, còn label nên `font-normal`.

---

# 12. Sidebar / Navigation

Navigation item bình thường:

```tsx
font-normal
```

Active navigation:

```tsx
font-medium
```

Không cần `font-bold` cho toàn bộ sidebar.

Ví dụ:

```text
Dashboard
Dự án nguồn
Dự án triển khai
Gói thầu
Nhà thầu
Hợp đồng
```

Inactive:

```tsx
font-normal
```

Active:

```tsx
font-medium
```

---

# 13. Breadcrumb

Breadcrumb không nên quá bold.

Ví dụ:

```text
Trang chủ
Hợp đồng
```

Có thể sử dụng:

```tsx
font-normal
```

cho item thông thường.

Current page:

```tsx
font-medium
```

Không cần `font-bold`.

---

# 14. Search toàn bộ project

Hãy audit toàn bộ source code và tìm các pattern như:

```text
font-bold
font-extrabold
font-black
font-semibold
font-medium
font-normal
font-[700]
font-[600]
font-[500]
font-[800]
font-[900]
fontWeight
font-weight
```

Đồng thời kiểm tra:

```tsx
style={{ fontWeight: ... }}
```

CSS:

```css
font-weight:
```

SCSS:

```scss
font-weight:
```

CSS modules:

```css
font-weight:
```

và các component typography abstraction nếu project có.

---

# 15. Không được chỉ sửa Tailwind class

Nếu project có các component dùng typography abstraction như:

```tsx
<Typography />
<Text />
<Heading />
<Label />
```

hãy kiểm tra chúng trước.

Nếu có design system typography chung, ưu tiên sửa ở component/base layer thay vì sửa hàng trăm component riêng lẻ.

Mục tiêu là:

```text
Typography system
        ↓
Reusable components
        ↓
Feature components
        ↓
Pages
```

không phải:

```text
Mỗi component tự định nghĩa font-weight
```

---

# 16. Ưu tiên sửa ở component dùng chung

Kiểm tra các component như:

```text
Button
Input
Select
Textarea
Modal
Dialog
Drawer
Table
Tabs
Badge
Card
FormField
FormLabel
Dropdown
Pagination
Breadcrumb
Sidebar
```

Nếu component dùng chung đang mặc định:

```tsx
font-bold
```

hãy sửa về typography hợp lý ở component đó.

Ví dụ:

```tsx
<FormLabel className="font-bold">
```

→

```tsx
<FormLabel className="font-medium">
```

Điều này tốt hơn việc sửa từng page.

---

# 17. Không phá vỡ hierarchy hiện tại

Không giảm tất cả typography xuống `font-normal`.

Ví dụ:

```text
Page title       → 700
Section title    → 600
Form label       → 500
Body             → 400
```

Đây là hierarchy cần duy trì.

Mục tiêu không phải:

```text
Everything → 400
```

mà là:

```text
700 → 600 → 500 → 400
```

với mỗi weight có một vai trò rõ ràng.

---

# 18. Đặc biệt kiểm tra các màn hình nghiệp vụ

Ưu tiên audit:

```text
Hợp đồng
Gói thầu
Dự án
Nhà thầu
Đợt thanh toán
Hàng hóa & Dịch vụ
Công việc
Người dùng
Phân quyền
Thông báo
Lịch sử hoạt động
Báo cáo
```

Đây là các màn hình có nhiều thông tin và nhiều form/table nên typography hierarchy rất quan trọng.

---

# 19. Quy tắc tránh "bold everywhere"

Sau khi refactor, hãy kiểm tra từng màn hình theo câu hỏi:

> "Nếu tôi nhìn nhanh màn hình này trong 2–3 giây, đâu là thứ mắt tôi phải nhìn đầu tiên?"

Nếu quá nhiều text có cùng độ đậm, hierarchy chưa tốt.

Ví dụ không nên:

```text
THÊM HỢP ĐỒNG MỚI        700
THÔNG TIN HỢP ĐỒNG       700
SỐ HỢP ĐỒNG              700
GIÁ TRỊ HỢP ĐỒNG         700
TÊN HỢP ĐỒNG             700
DỰ ÁN                    700
NHÀ THẦU                 700
```

Nên:

```text
Thêm hợp đồng mới        700
Thông tin hợp đồng       600

Số hợp đồng              500
Giá trị hợp đồng         500
Tên hợp đồng             500
Dự án                    500
Nhà thầu                 500
```

---

# 20. Không thay đổi ngoài phạm vi

Trong task này:

**KHÔNG tự ý thay đổi:**

- Business logic
- API
- State management
- Routing
- Validation logic
- Data fetching
- API contract
- Database
- Màu sắc toàn hệ thống
- Spacing toàn hệ thống
- Layout
- Component behavior
- Responsive behavior

Chỉ thay đổi typography/font-weight khi cần thiết.

Nếu phát hiện vấn đề typography khác như font-size, line-height, color contrast nhưng không liên quan trực tiếp đến font-weight, hãy ghi nhận vào report thay vì tự ý sửa.

---

# 21. Kiểm tra consistency

Sau khi refactor, đảm bảo cùng một loại component có cùng typography.

Ví dụ:

Tất cả `FormLabel`:

```tsx
font-medium
```

Tất cả `ModalTitle`:

```tsx
font-bold
```

Tất cả `SectionTitle`:

```tsx
font-semibold
```

Tất cả `BodyText`:

```tsx
font-normal
```

Tất cả `StatusBadge`:

```tsx
font-medium
```

Không để tình trạng:

```text
Form A label → 500
Form B label → 600
Form C label → 700
```

nếu không có lý do UX rõ ràng.

---

# 22. Nếu project đang sử dụng Tailwind

Ưu tiên semantic typography class hoặc reusable component.

Nếu phù hợp với architecture hiện tại, có thể tạo abstraction:

```tsx
<FormLabel />
<SectionTitle />
<PageTitle />
<BodyText />
<Caption />
```

hoặc class dùng chung.

Không tạo abstraction quá mức nếu project hiện tại chưa có nhu cầu.

Ưu tiên giải pháp đơn giản, dễ maintain.

---

# 23. Acceptance criteria

Sau khi hoàn thành:

- Không còn `font-bold` được sử dụng bừa bãi cho form label.
- Form label mặc định là `font-medium`.
- Body text mặc định là `font-normal`.
- Section heading dùng `font-semibold`.
- Page/modal title dùng `font-bold`.
- Button không mặc định là `font-bold`.
- Placeholder không dùng bold.
- Table cell không dùng bold nếu không cần.
- Sidebar item không dùng bold.
- Badge/status dùng medium.
- Typography hierarchy nhất quán giữa các feature.
- Không thay đổi business logic.
- Không thay đổi behavior.
- Không làm thay đổi layout ngoài những ảnh hưởng tự nhiên từ typography.

---

# 24. Quy trình thực hiện

Thực hiện theo thứ tự:

### Phase 1 — Audit

Quét toàn bộ frontend source code.

Liệt kê:

```text
File
Component
Current font-weight
Element type
Recommended font-weight
Reason
```

Không sửa code ngay nếu chưa hiểu context.

### Phase 2 — Design system / shared components

Xác định các component dùng chung và sửa typography ở layer này trước.

### Phase 3 — Feature components

Refactor các component còn lại theo hierarchy đã thống nhất.

### Phase 4 — Pages

Kiểm tra các page để đảm bảo không còn override typography sai.

### Phase 5 — Final audit

Search lại toàn bộ project:

```text
font-bold
font-extrabold
font-black
font-[700]
font-[800]
font-[900]
fontWeight: 700
fontWeight: 800
fontWeight: 900
font-weight: 700
font-weight: 800
font-weight: 900
```

Review từng kết quả.

**Không cần xóa toàn bộ `font-bold`.**

Chỉ giữ lại những nơi thực sự đóng vai trò heading/primary emphasis.

---

# 25. Output cuối cùng

Sau khi hoàn thành, hãy báo cáo:

## Typography changes

- Tổng số file đã thay đổi
- Tổng số component đã thay đổi
- Bao nhiêu `font-bold` được loại bỏ
- Bao nhiêu label chuyển `700 → 500`
- Bao nhiêu heading chuyển `700 → 600`
- Bao nhiêu body text chuyển `600/700 → 400`
- Những component dùng chung đã được chuẩn hóa

## Remaining bold usage

Liệt kê các vị trí vẫn còn `font-bold` và giải thích ngắn gọn tại sao chúng được giữ lại.

Ví dụ:

```text
ModalTitle → giữ 700 vì là primary heading
PageTitle → giữ 700 vì là page heading
SectionTitle → chuyển 700 → 600
FormLabel → chuyển 700 → 500
```

## Important findings

Nếu phát hiện typography inconsistency khác, chỉ ghi nhận để review sau, không tự ý mở rộng scope.

---

## Nguyên tắc cuối cùng

Hãy thiết kế typography theo nguyên tắc:

> **Bold để tạo hierarchy, không phải để làm mọi thứ nổi bật.**

Ưu tiên:

```text
700 — Heading
600 — Section / emphasis
500 — Label / important data
400 — Normal content
```

Mục tiêu cuối cùng là giao diện **nhẹ mắt, dễ scan, chuyên nghiệp và nhất quán**, đặc biệt với các màn hình nghiệp vụ có nhiều form, table và dữ liệu như hệ thống quản lý dự án/hợp đồng.