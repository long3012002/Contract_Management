Bạn hãy rà soát và chuẩn hóa toàn bộ UI/UX liên quan đến **các thành phần nhập liệu (form controls)** trong toàn bộ project.

Mục tiêu: tạo một hệ thống form có giao diện **gọn, chuyên nghiệp, dễ đọc, dễ quét thông tin**, phù hợp với hệ thống quản lý nghiệp vụ doanh nghiệp/ngân hàng. Không redesign toàn bộ UI và không thay đổi business logic.

## 1. Typography của Label

Chuẩn hóa toàn bộ label của input/select/textarea/date picker/checkbox/radio:

- Không sử dụng `font-bold` hoặc `font-weight: 700` cho label thông thường.
- Label mặc định:
  - `font-size: 13px–14px`
  - `font-weight: 500`
  - màu `#4B5563`
- Label phải nhẹ hơn giá trị dữ liệu trong input.
- Chỉ sử dụng `font-weight: 600` cho section heading hoặc nhóm field quan trọng.
- Không làm toàn bộ label trong form đều bold.

Hierarchy mong muốn:

Section heading
→ Label
→ Input value

Ví dụ:

Số hợp đồng *
[ HD-2026-1 ]

Trong đó `HD-2026-1` phải có độ tương phản thị giác cao hơn label `Số hợp đồng`.

## 2. Input value

Chuẩn hóa text bên trong input:

- `font-size: 14px`
- `font-weight: 400–500`
- màu chính: `#111827`
- Không dùng font-weight quá đậm cho value.
- Giá trị người dùng nhập phải dễ đọc hơn placeholder.
- Không sử dụng màu xám cho dữ liệu đã nhập.

## 3. Placeholder

Placeholder phải có hierarchy thấp hơn input value:

- `font-size: 14px`
- `font-weight: 400`
- màu khoảng `#9CA3AF`
- Không sử dụng placeholder quá đậm.
- Không dùng placeholder như một label thay thế.

Ví dụ:

Label:
Thời hạn (Số ngày)

Placeholder:
Nhập số ngày

## 4. Required field

Chuẩn hóa dấu `*`:

- Không làm toàn bộ label màu đỏ.
- Chỉ dấu `*` thể hiện required.
- Dấu `*` sử dụng màu error.
- Font-weight của dấu `*` không cần quá đậm.

Ví dụ:

Số hợp đồng *

Không sử dụng:

Số hợp đồng *

với toàn bộ label màu đỏ.

## 5. Input border

Chuẩn hóa border:

Default:
- border mảnh, khoảng `1px`
- màu neutral nhẹ như `#D1D5DB`
- background trắng

Hover:
- border đậm hơn một chút.

Focus:
- border sử dụng màu primary của hệ thống.
- có focus ring nhẹ.
- Không tạo shadow quá lớn.

Không sử dụng border hoặc shadow quá đậm khiến form trở nên nặng nề.

## 6. Input height

Chuẩn hóa chiều cao các control:

- Input: khoảng `40px`
- Select: khoảng `40px`
- Date picker: khoảng `40px`
- Button trong form: khoảng `36–40px`
- Textarea: chiều cao linh hoạt tùy nội dung.

Các field nằm cùng một row phải có chiều cao đồng nhất.

## 7. Khoảng cách

Chuẩn hóa spacing:

- Label → input: khoảng `6–8px`
- Field → field: khoảng `16–20px`
- Section → section: khoảng `24px`
- Không để các input quá sát nhau.
- Không tạo khoảng cách quá lớn làm form bị loãng.

Ưu tiên layout compact nhưng vẫn dễ thao tác.

## 8. Error state

Chuẩn hóa validation error:

Khi input lỗi:

- border chuyển sang màu error.
- focus/error state rõ ràng.
- hiển thị error message ngay bên dưới input.
- error message:
  - `12–13px`
  - font-weight `400–500`
  - màu error.

Ví dụ:

Số hợp đồng *
[              ]

Số hợp đồng không được để trống.

Không sử dụng alert/toast thay thế hoàn toàn cho lỗi validation của từng field.

## 9. Helper text

Nếu field cần giải thích:

Label
[ Input ]
Helper text

Helper text:

- `12px–13px`
- `font-weight: 400`
- màu `#6B7280`

Không sử dụng font bold cho helper text.

## 10. Disabled state

Input disabled phải dễ phân biệt với input bình thường nhưng không quá nổi bật:

- background neutral nhẹ.
- text màu muted.
- cursor phù hợp.
- border nhẹ.

Không dùng opacity quá thấp khiến text khó đọc.

## 11. Read-only state

Phân biệt rõ:

- Editable input
- Disabled input
- Read-only value

Read-only field có thể dùng background rất nhẹ hoặc border nhẹ để người dùng hiểu rằng đây là dữ liệu hệ thống tính toán/không chỉnh sửa.

## 12. Number / Currency input

Đối với các field tiền tệ như:

- Giá trị hợp đồng
- Giá trị đợt thanh toán
- Giá trị giải ngân
- Giá trị điều chỉnh

Chuẩn hóa:

- Số tiền hiển thị có phân tách hàng nghìn.
- Không để người dùng nhập ký tự không hợp lệ.
- Đơn vị `VND` đặt ở vị trí thống nhất.
- Giá trị tiền phải dễ scan.
- Không làm đơn vị tiền nổi bật hơn giá trị.

Ví dụ:

[ 2.000.000                         ] VND

Helper text nếu cần:

= 2 triệu đồng

Helper text phải nhỏ và nhẹ hơn giá trị chính.

## 13. Date input

Chuẩn hóa các field ngày:

- icon calendar thống nhất.
- format ngày thống nhất toàn hệ thống.
- input height giống các field khác.
- placeholder rõ ràng.
- không để date picker có style khác biệt quá lớn so với input thông thường.

## 14. Select / Combobox

Select phải có hierarchy tương tự input:

Label
[ Giá trị đã chọn                 ˅ ]

Giá trị đã chọn phải đậm hơn placeholder.

Không làm icon dropdown quá nổi bật.

## 15. Checkbox / Radio

Label của checkbox/radio không được bold nếu không cần thiết.

Ví dụ:

☐ Hợp đồng liên danh (nhiều nhà thầu)

Chỉ sử dụng font-weight cao hơn nếu đây là heading hoặc nhóm lựa chọn quan trọng.

## 16. Form section

Các heading như:

Thông tin hợp đồng
Kế hoạch thanh toán
Danh mục hàng hóa, dịch vụ

được phép sử dụng:

- `font-weight: 600`
- khoảng `15–16px`

Nhưng label bên trong section phải nhẹ hơn heading.

Tránh tình trạng heading, label và input value đều có cùng font-weight.

## 17. Đặc biệt với màn hình "Chỉnh sửa hợp đồng"

Hãy áp dụng trực tiếp các nguyên tắc trên cho màn hình form hợp đồng hiện tại.

Các field như:

- Số hợp đồng
- Giá trị hợp đồng
- Tên hợp đồng
- Dự án
- Gói thầu
- Nhà thầu / Đối tác
- Trạng thái
- Ngày ký HĐ
- Ngày hết hạn HĐ
- Nội dung HĐ
- Tên đợt
- Ngày bắt đầu
- Thời hạn
- Ngày kết thúc
- Tỷ lệ
- Giá trị
- Điều kiện thanh toán

phải có typography, spacing, height và trạng thái interaction thống nhất.

## 18. Không over-design

Đây là hệ thống nghiệp vụ doanh nghiệp, vì vậy:

- Không thêm gradient.
- Không thêm shadow mạnh.
- Không thêm animation không cần thiết.
- Không dùng quá nhiều màu.
- Không tăng font-weight để tạo cảm giác nổi bật.
- Không thay đổi layout hiện tại nếu không cần thiết.
- Ưu tiên hierarchy bằng typography, spacing và contrast.
- Giữ giao diện đơn giản, chuyên nghiệp và dễ scan.

## 19. Quan trọng: tạo design token dùng chung

Không fix từng input một cách thủ công.

Hãy kiểm tra project và nếu đang có design token/theme/component dùng chung thì chuẩn hóa tại đó.

Tạo hoặc cập nhật các token/component dùng chung cho:

- Label
- Input
- Select
- Textarea
- Date input
- Number input
- Helper text
- Error text
- Disabled state
- Focus state

Mục tiêu là khi thay đổi typography của form sau này chỉ cần thay đổi ở một nơi.

## 20. Kiểm tra toàn bộ project

Sau khi implement:

1. Tìm toàn bộ input/select/textarea/date picker/form field.
2. Tìm các label đang sử dụng `font-bold`, `font-semibold`, `font-weight: 600/700`.
3. Xác định trường hợp nào thực sự cần bold.
4. Loại bỏ bold không cần thiết.
5. Đảm bảo các form có cùng visual language.
6. Không thay đổi business logic.
7. Không thay đổi API.
8. Không thay đổi validation rule hiện tại.
9. Không tạo component duplicate nếu project đã có component dùng chung.
10. Ưu tiên tái sử dụng component/design token hiện có.

### Acceptance criteria

Sau khi hoàn thành, một người dùng nhìn vào form phải có hierarchy rõ ràng:

**Section heading**
→ **Label nhẹ**
→ **Giá trị input rõ và dễ đọc**
→ **Helper/error text nhỏ hơn**

Đặc biệt:

- Label không còn cảm giác "quá nhiều chữ bold".
- Input value nổi bật hơn label.
- Placeholder rõ ràng nhưng ít nổi bật.
- Error/helper text không cạnh tranh với dữ liệu chính.
- Các input trong cùng một form có height, spacing và typography đồng nhất.
- UI tổng thể nhẹ hơn, sạch hơn nhưng vẫn đảm bảo khả năng đọc và thao tác.

Trước khi sửa, hãy kiểm tra các component form hiện có trong project và **tái sử dụng/chỉnh sửa component dùng chung thay vì sửa từng màn hình riêng lẻ**.