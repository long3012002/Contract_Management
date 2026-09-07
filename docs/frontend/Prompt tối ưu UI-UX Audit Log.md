Bạn là Senior UI/UX Designer + Frontend Engineer, hãy audit và cải thiện UI/UX cho component **Audit Log / Lịch sử hoạt động & Biến động dự án** dựa trên giao diện hiện tại.

## 1. Mục tiêu

Thiết kế lại Audit Log theo hướng:

- Gọn hơn.
- Dễ quét thông tin bằng mắt.
- Ưu tiên thông tin nghiệp vụ, hạn chế thông tin kỹ thuật.
- Phân cấp thị giác rõ ràng.
- Không sử dụng quá nhiều font-weight Semibold/Bold.
- Không làm giao diện quá nhiều màu.
- Phù hợp với web ứng dụng nội bộ doanh nghiệp/ngân hàng.
- Người dùng có thể nhanh chóng trả lời:
  1. Ai đã thay đổi?
  2. Thay đổi lúc nào?
  3. Đối tượng nào bị thay đổi?
  4. Thay đổi cái gì?
  5. Từ giá trị nào → giá trị nào?

Không thay đổi business logic hoặc dữ liệu backend. Chỉ cải thiện cách trình bày dữ liệu.

---

# 2. Nguyên tắc thiết kế tổng thể

Áp dụng nguyên tắc:

**Information first, decoration second.**

Audit Log không phải dashboard nên không cần nhiều màu sắc, badge hoặc hiệu ứng.

Ưu tiên:

- Typography
- Spacing
- Alignment
- Contrast
- Grouping
- Scanability

Hạn chế:

- Card lồng card quá nhiều.
- Border quá dày.
- Background màu quá rõ.
- Shadow mạnh.
- Badge không cần thiết.
- Icon trang trí.
- Font-weight 600/700 quá nhiều.

---

# 3. Typography

Sử dụng typography tiết chế.

### Page title

`Lịch sử hoạt động & Biến động dự án`

- Font size: 20–22px
- Font weight: 600
- Color: #1F2937 hoặc màu text-primary hiện tại.

Không sử dụng 700 nếu không thực sự cần.

### Description

`Theo dõi toàn bộ nhật ký thay đổi thông tin dự án, hợp đồng và các đợt thanh toán liên quan`

- Font size: 14–15px
- Font weight: 400
- Color: secondary text.
- Line-height: khoảng 1.5.

### Audit action

Ví dụ:

`Cập nhật`

- Font size: 14px
- Font weight: 600

Không làm toàn bộ dòng audit bằng Semibold.

### Entity

Thay vì:

`UPDATE DuAns`

hiển thị theo hướng nghiệp vụ:

`Cập nhật · Dự án`

hoặc:

`Cập nhật`
`Dự án`

Nếu hệ thống có mapping entity thì ưu tiên tên thân thiện:

- `DuAns` → `Dự án`
- `HopDongs` → `Hợp đồng`
- `GoiThaus` → `Gói thầu`
- `ThanhToans` → `Đợt thanh toán`

Không hiển thị trực tiếp tên table/entity/database cho người dùng cuối.

---

# 4. Audit item layout

Giảm chiều cao của mỗi audit item.

Không nên để mỗi item là một card lớn với quá nhiều padding như hiện tại.

Đề xuất cấu trúc:

[Timeline dot]
        |
        └── [Action + Entity]                  [Time]
            Người thực hiện
            Thay đổi:
            Field        Old value → New value

Ví dụ:

●  Cập nhật · Dự án                         14:06 · 22/08/2026
   admin · Thành viên

   2 trường thông tin đã thay đổi

   Chủ đầu tư      CoopBank → COB
   Cập nhật lúc    — → 22/08/2026 14:06

---

# 5. Timeline

Giữ timeline vì đây là pattern phù hợp với Audit Log.

Nhưng làm timeline nhẹ hơn:

- Dot nhỏ khoảng 8–10px.
- Đường timeline màu neutral rất nhạt.
- Không dùng màu quá nổi.
- Khoảng cách giữa các item khoảng 20–24px.
- Timeline không nên chiếm nhiều diện tích ngang.

Timeline chỉ đóng vai trò định hướng thời gian, không phải thành phần chính.

---

# 6. Action + Entity

Hiện tại đang hiển thị:

`UPDATE DuAns`

Hãy chuyển thành dạng dễ đọc:

`Cập nhật · Dự án`

hoặc:

`Cập nhật`
`Dự án`

Nếu muốn giữ badge:

- Action: text rõ ràng.
- Entity: badge rất nhẹ.
- Badge không sử dụng màu quá mạnh.

Ví dụ:

Cập nhật  [Dự án]

Không sử dụng:

`UPDATE [DuAns]`

vì đây là cách biểu diễn thiên về developer/database hơn là người dùng nghiệp vụ.

---

# 7. Timestamp

Timestamp hiện tại đang quá nổi bật.

Đưa timestamp về secondary information:

`14:06 · 22/08/2026`

- Font size: 13–14px
- Font weight: 400
- Color: secondary text.
- Icon clock nhỏ.

Không cần làm timestamp bằng màu quá đậm.

Nếu audit log nằm trong một modal:

- Có thể hiển thị ngày ở dạng `14:06 · 22/08/2026`.
- Nếu nhiều record cùng ngày, có thể giảm lặp ngày bằng grouping theo ngày.

Ví dụ:

### Hôm nay — 22/08/2026

14:06 · Cập nhật Dự án  
11:21 · Cập nhật Hợp đồng  
10:43 · Cập nhật Hợp đồng

Điều này giúp giảm đáng kể lượng text lặp lại.

---

# 8. Người thực hiện

Hiện tại:

`Thực hiện bởi: admin (Thành viên)`

Có thể rút gọn:

`admin · Thành viên`

hoặc:

`admin` với role hiển thị nhỏ hơn.

Ví dụ:

admin · Thành viên

- Username: 14px, weight 500
- Role: 13px, weight 400
- Color nhẹ hơn.

Không cần icon user quá lớn.

---

# 9. "Các trường thay đổi"

Hiện tại:

`Các trường thay đổi: ["ChuDauTu","UpdatedAt"]`

Đây là thông tin kỹ thuật và không nên ưu tiên hiển thị.

Không hiển thị raw array:

`["ChuDauTu","UpdatedAt"]`

Thay bằng:

`2 trường thông tin đã thay đổi`

Nếu có nhu cầu xem chi tiết technical field thì đưa vào:

`Chi tiết`

hoặc expandable section.

Ví dụ:

`2 trường thông tin đã thay đổi  ˅`

Click vào mới hiển thị field details.

---

# 10. Bảng Old Value / New Value

Giữ lại concept:

`Trường thay đổi | Giá trị cũ | Giá trị mới`

nhưng tối ưu lại.

Không cần một card có border riêng nằm bên trong card audit.

Có thể dùng layout dạng compact:

| Trường | Thay đổi |
|---|---|
| Chủ đầu tư | CoopBank → COB |
| Cập nhật lúc | — → 22/08/2026 14:06 |

Hoặc:

**Chủ đầu tư**

`CoopBank` → `COB`

Trong đó:

- Old value: neutral/muted + strikethrough nhẹ.
- Arrow: neutral.
- New value: màu text chính hoặc success rất nhẹ.

Không dùng màu đỏ/xanh quá mạnh.

---

# 11. Không highlight UpdatedAt quá mức

Đây là điểm rất quan trọng.

`UpdatedAt` thường là field hệ thống tự động cập nhật.

Nếu mọi thao tác đều sinh audit:

`UpdatedAt`

thì Audit Log sẽ bị nhiễu.

Ưu tiên:

### Option A — Tốt nhất

Không hiển thị `UpdatedAt` trong danh sách field changed nếu đây là field hệ thống tự động.

Ví dụ:

Thay vì:

2 trường thay đổi:
- ChuDauTu
- UpdatedAt

chỉ hiển thị:

1 trường thay đổi:
- Chủ đầu tư: CoopBank → COB

### Option B

Nếu bắt buộc phải hiển thị:

Đánh dấu đây là system field và đưa xuống secondary information.

Ví dụ:

`Cập nhật lúc: ...`

Không để `UpdatedAt` cạnh các field nghiệp vụ như một thay đổi quan trọng.

---

# 12. Color system

Giữ giao diện neutral.

Không sử dụng màu sắc để trang trí.

Đề xuất hierarchy:

### Primary text

Dark neutral.

### Secondary text

Gray-blue/neutral.

### Old value

Muted gray + strikethrough.

### New value

Dark text hoặc success nhẹ.

### Timeline

Neutral light.

### Border

Very light neutral.

### Action

Không cần màu đỏ/xanh theo kiểu status nếu không có ý nghĩa nghiệp vụ.

Chỉ sử dụng màu semantic khi thực sự cần:

- Create → success nhẹ
- Update → primary/neutral
- Delete → danger
- Approve → success
- Reject → danger

Không tô màu toàn bộ card.

---

# 13. Card design

Giảm cảm giác "card trong card".

Hiện tại đang có:

Modal
→ Audit card
→ Table card

Điều này tạo quá nhiều lớp container.

Đề xuất:

Modal
→ Timeline item
→ Compact change list

Chỉ sử dụng border ở cấp cần thiết.

Có thể bỏ border của audit item nếu timeline đã đủ phân tách.

Nếu giữ card:

- Background: white
- Border: rất nhẹ
- Border radius: 10–12px
- Padding: 16–20px
- Không dùng shadow hoặc chỉ dùng shadow cực nhẹ.

---

# 14. Modal

Modal hiện tại khá rộng và cao.

Giữ modal đủ rộng để xem diff nhưng không để nội dung trải quá rộng.

Đề xuất:

- Width khoảng 800–960px tùy responsive.
- Header cố định.
- Content scroll riêng.
- Footer cố định.
- Không để toàn bộ modal scroll khiến header/footer biến mất.

Header:

Lịch sử hoạt động & Biến động dự án

Description bên dưới.

Footer:

[Đóng]

Nút Đóng có thể giữ dạng secondary button.

---

# 15. Empty state

Nếu không có audit log:

Không để màn hình trống.

Hiển thị:

Không có lịch sử hoạt động

Chưa có thay đổi nào được ghi nhận cho đối tượng này.

Có thể sử dụng icon history nhẹ.

---

# 16. Loading state

Không sử dụng spinner lớn.

Dùng skeleton cho:

- timeline
- action
- timestamp
- change rows

Mục tiêu là giữ layout ổn định khi loading.

---

# 17. Responsive

Đảm bảo hoạt động tốt với:

- Desktop
- Laptop
- Màn hình độ phân giải thấp

Ở màn hình nhỏ:

Không để:

Field | Old Value | New Value

bị ép quá chật.

Có thể chuyển thành:

Chủ đầu tư
CoopBank → COB

Cập nhật lúc
— → 22/08/2026 14:06

---

# 18. Information hierarchy cuối cùng

Mỗi audit item cần có hierarchy theo thứ tự:

1. **Action + Entity**
2. **Timestamp**
3. **User**
4. **Số lượng field thay đổi**
5. **Các thay đổi quan trọng**
6. Technical/system fields

Ví dụ UI mong muốn:

┌──────────────────────────────────────────────────────┐

Cập nhật  [Dự án]                       14:06 · 22/08/2026

admin · Thành viên

2 trường thông tin đã thay đổi

Chủ đầu tư
CoopBank  →  COB

Cập nhật lúc
—  →  22/08/2026 14:06

└──────────────────────────────────────────────────────┘

---

# 19. Quy tắc typography bắt buộc

Không sử dụng Semibold/Bold tràn lan.

Quy tắc:

- Page title: 600
- Section title: 600
- Action: 600
- Entity: 500
- Important value: 500
- Normal content: 400
- Secondary information: 400
- Timestamp: 400
- Old value: 400
- Technical information: 400

Không dùng `font-weight: 600/700` cho cả một đoạn text dài.

Mục tiêu là tạo hierarchy bằng:

**size + spacing + position + color**

thay vì chỉ dựa vào font-weight.

---

# 20. Quy tắc UX quan trọng

Audit Log phải giúp người dùng "scan" được lịch sử trong vài giây.

Khi nhìn vào một item, người dùng phải nhận biết ngay:

**Ai → làm gì → trên đối tượng nào → khi nào → thay đổi gì**

Không bắt người dùng đọc:

- JSON
- database field name
- raw entity name
- system field
- metadata không cần thiết.

Ẩn technical details sau expandable section nếu cần cho Admin/IT.

---

# 21. Yêu cầu khi chỉnh code

- Không thay đổi API.
- Không thay đổi database schema.
- Không thay đổi business logic.
- Không thay đổi audit data structure nếu không cần thiết.
- Tận dụng component hiện tại nếu có thể.
- Chỉ refactor component nếu giúp UI rõ ràng hơn.
- Giữ nguyên khả năng scroll và đóng modal.
- Không thêm UI library mới chỉ để giải quyết vấn đề styling.
- Ưu tiên Tailwind/CSS/component hiện có của project.
- Không hard-code dữ liệu audit.
- Mapping entity technical → business name nên được thực hiện bằng một mapping function/config.
- Mapping field technical → label hiển thị cũng nên được tách riêng.

Ví dụ:

`ChuDauTu` → `Chủ đầu tư`

`UpdatedAt` → `Cập nhật lúc`

`DuAns` → `Dự án`

`HopDongs` → `Hợp đồng`

---

# 22. Acceptance Criteria

Sau khi hoàn thành, UI phải đạt các tiêu chí:

- [ ] Nhìn vào audit log có thể scan nhanh người thực hiện, hành động, đối tượng và thời gian.
- [ ] Không còn hiển thị raw entity như `DuAns`, `HopDongs` cho user thông thường.
- [ ] Không còn hiển thị raw array như `["ChuDauTu","UpdatedAt"]`.
- [ ] `UpdatedAt` không gây nhiễu audit log.
- [ ] Giảm chiều cao mỗi audit item.
- [ ] Giảm nested card.
- [ ] Giảm khoảng trắng không cần thiết.
- [ ] Typography rõ hierarchy nhưng không lạm dụng Semibold.
- [ ] Màu sắc trung tính, phù hợp web nội bộ doanh nghiệp/ngân hàng.
- [ ] Old value và New value dễ phân biệt nhưng không dùng màu quá mạnh.
- [ ] Timeline nhẹ, không chiếm visual attention.
- [ ] Modal header/footer hoạt động tốt khi content scroll.
- [ ] Responsive tốt.
- [ ] Không thay đổi business logic/API.
- [ ] Code component dễ maintain và tái sử dụng.

## Quan trọng

Đừng chỉ "làm cho đẹp".

Hãy ưu tiên **information hierarchy, scanability, cognitive load và khả năng đọc nhanh của người dùng nghiệp vụ**.

Nếu phải lựa chọn giữa nhiều decoration và khả năng đọc nhanh, luôn ưu tiên khả năng đọc nhanh.

Sau khi chỉnh sửa, hãy tự review UI một lần nữa và loại bỏ các thành phần:

- không cung cấp thông tin hữu ích,
- quá nổi bật so với mức độ quan trọng,
- lặp lại thông tin,
- hoặc mang tính kỹ thuật nhưng không cần thiết đối với người dùng nghiệp vụ.