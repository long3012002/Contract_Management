# Quy tắc sử dụng Font Semibold trong UI

Hãy audit toàn bộ UI và hạn chế tối đa việc sử dụng `font-semibold` / `font-weight: 600`.

## Mục tiêu

UI cần có cảm giác:
- Gọn gàng, nhẹ mắt, chuyên nghiệp.
- Dễ quét thông tin.
- Không bị cảm giác "chữ nào cũng đậm".
- Tạo hierarchy bằng kích thước, spacing, màu sắc và layout trước khi dùng font-weight.

## Quy tắc chính

### 1. Ưu tiên Font Weight thấp hơn

Ưu tiên theo thứ tự:

- `font-normal` / `400`: mặc định cho phần lớn nội dung.
- `font-medium` / `500`: dùng để nhấn mạnh nhẹ.
- `font-semibold` / `600`: chỉ sử dụng cho những thành phần thực sự cần nhấn mạnh.
- `font-bold` / `700`: hạn chế tối đa, chỉ dành cho heading hoặc trường hợp đặc biệt.

Không sử dụng `font-semibold` một cách mặc định cho:
- Label của form.
- Nội dung bảng.
- Giá trị dữ liệu.
- Button thông thường.
- Text mô tả.
- Metadata.
- Badge/status thông thường.
- Navigation item thông thường.
- Các đoạn text mà `font-medium` hoặc `font-normal` đã đủ rõ ràng.

### 2. Khi nào được sử dụng Semibold?

Chỉ sử dụng `font-semibold` khi cần tạo hierarchy rõ ràng, ví dụ:

- Heading hoặc section title quan trọng.
- Tiêu đề của card cần nổi bật.
- Tên entity chính trong một danh sách.
- Tab/Navigation đang active nếu cần phân biệt rõ.
- Các thông tin quan trọng cần người dùng nhận diện nhanh.
- CTA hoặc trạng thái đặc biệt khi thiết kế thực sự cần nhấn mạnh.

Ngay cả trong các trường hợp trên, hãy kiểm tra xem có thể đạt hierarchy bằng:
- Font size.
- Màu sắc.
- Khoảng cách.
- Background.
- Border.
- Position/layout.

trước khi tăng font-weight lên `600`.

### 3. Tránh "Bold Everything"

Không tạo hierarchy bằng cách làm nhiều thành phần cùng lúc `font-semibold`.

Ví dụ không nên:

- Label: `600`
- Value: `600`
- Card title: `600`
- Button: `600`
- Table header: `600`
- Table content: `600`

Điều này khiến toàn bộ UI trở nên nặng và làm mất hierarchy.

Thay vào đó, nên phân cấp:

- Heading → `600`
- Label → `400` hoặc `500`
- Value → `400` hoặc `500`
- Description → `400`
- Metadata → `400`
- Button → `500`
- Table header → `500`
- Table content → `400`

### 4. Đối với hệ thống web nội bộ

Ưu tiên sự rõ ràng và khả năng scan nhanh hơn việc tạo cảm giác nổi bật.

Đặc biệt với:
- Bảng dữ liệu.
- Danh sách hợp đồng.
- Danh sách dự án.
- Danh sách thanh toán.
- Form nhập liệu.
- Dashboard nghiệp vụ.

Không nên có quá nhiều text `600`.

Người dùng cần có thể nhìn nhanh và phân biệt:

**Tiêu đề → thông tin chính → thông tin phụ → trạng thái → hành động**

mà không cần dựa vào việc mọi thứ đều được in đậm.

## 5. Quy tắc khi review code

Khi phát hiện:

```tsx
className="font-semibold"
```

hãy đặt câu hỏi:

> "Thành phần này có thực sự cần weight 600 để người dùng nhận diện không?"

Nếu không, đổi về:

```tsx
font-medium
```

hoặc:

```tsx
font-normal
```

Không thay đổi máy móc toàn bộ `font-semibold` thành `font-normal`. Hãy đánh giá vai trò của từng component và hierarchy tổng thể.

## 6. Quy tắc ưu tiên

Khi cần làm một thành phần nổi bật hơn, ưu tiên theo thứ tự:

1. Layout / vị trí
2. Khoảng cách
3. Font size
4. Màu sắc
5. Background / border
6. Font weight

Font weight chỉ là một trong nhiều công cụ tạo hierarchy, không phải công cụ mặc định.

## 7. Yêu cầu khi chỉnh sửa

Khi audit UI:

- Tìm tất cả các trường hợp sử dụng `font-semibold`.
- Phân loại theo component.
- Xác định trường hợp nào thực sự cần `600`.
- Chuyển các trường hợp không cần thiết về `500` hoặc `400`.
- Giữ `600` cho các điểm hierarchy quan trọng.
- Không làm mất khả năng phân biệt thông tin.
- Không thay đổi font-size nếu không cần thiết.
- Không thay đổi màu sắc chỉ để bù cho việc giảm font-weight.
- Giữ UI nhất quán giữa các component cùng loại.

### Nguyên tắc cuối cùng

> **Semibold là trạng thái nhấn mạnh, không phải font-weight mặc định.**

Mục tiêu không phải là loại bỏ hoàn toàn `font-semibold`, mà là đảm bảo mỗi lần sử dụng `600` đều có lý do về mặt hierarchy và UX.