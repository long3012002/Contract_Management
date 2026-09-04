Hãy tinh chỉnh UI/UX của **khu vực Bình luận / Thảo luận & Ghi chú** hiện tại.

## Mục tiêu

Giữ nguyên layout và cấu trúc hiện tại vì tổng thể đã ổn.

Chỉ cần tinh chỉnh để giao diện:

- Gọn hơn.
- Dễ đọc hơn.
- Giảm cảm giác quá nhiều chữ bold.
- Phân cấp thông tin rõ hơn.
- Comment và reply tự nhiên hơn.
- Không làm UI trở nên màu mè.
- Phù hợp với hệ thống quản lý công việc/văn phòng điện tử.

**Không redesign lại toàn bộ component.**

---

## 1. Typography

Giảm font-weight của các thông tin metadata.

### Tên người dùng

Tên người comment vẫn giữ `font-weight: 600` hoặc semibold.

Ví dụ:

```text
Admin @admin · 09:30 · 11/08/2026
```

Trong đó:

- `Admin`: semibold
- `@admin`: muted, regular
- dấu `·`: muted
- thời gian: muted, regular
- trạng thái `(đã sửa)`: muted, nhỏ hơn

Không bold toàn bộ dòng metadata.

### Reply metadata

Với:

```text
Mai Đức Quang @quangmd · trả lời Admin · 16:47 · 11/08/2026
```

Chỉ nhấn mạnh:

```text
Mai Đức Quang
```

Có thể nhấn nhẹ:

```text
Admin
```

Các phần còn lại dùng màu muted và font-weight regular.

Mục tiêu là mắt người dùng tập trung vào:

**Người viết → nội dung comment**

thay vì tập trung vào metadata.

---

## 2. Mention

Hiện tại mention đang được hiển thị giống một button/tag có border và background.

Ví dụ hiện tại:

```text
[Mai Đức Quang] alo làm xong chưa
```

Hãy đổi thành mention nhẹ hơn:

```text
@Mai Đức Quang alo làm xong chưa
```

hoặc dạng:

```text
@Mai Đức Quang
```

với:

- màu accent của hệ thống.
- font-weight khoảng 500–600.
- không cần border.
- không cần background.
- không tạo cảm giác giống button.

Mention vẫn phải dễ nhận biết và có thể click nếu hệ thống hiện tại hỗ trợ.

Áp dụng tương tự cho:

```text
@Lê Đức Anh
@Admin
```

---

## 3. Reply indentation

Giữ việc reply được thụt vào để thể hiện quan hệ.

Nhưng không thụt quá sâu.

Đề xuất:

```text
Comment cấp 1
│
└── Reply
```

Khoảng cách ngang khoảng **32–40px**.

Không tạo nesting quá sâu.

Nếu hệ thống hiện tại đang hỗ trợ nhiều cấp reply, giới hạn UI ở mức trực quan, tránh việc nội dung bị thu hẹp quá nhiều.

Ưu tiên:

**flat comment + một mức reply indentation rõ ràng.**

---

## 4. "Trả lời"

Hiện tại mỗi comment đều hiển thị:

```text
Trả lời
```

Hãy giảm mức độ nổi bật:

- font-size khoảng 13px.
- font-weight 500.
- màu muted.
- không cần background.
- không cần border.

Nếu phù hợp với interaction hiện tại, có thể chỉ hiển thị rõ hơn khi hover/focus vào comment.

Tuy nhiên vẫn phải đảm bảo người dùng dễ dàng nhận biết comment có thể reply.

---

## 5. Khoảng cách giữa các comment

Giữ khoảng trắng tương đối thoáng nhưng tránh quá nhiều khoảng trống.

Comment cấp cao:

```text
Avatar  Name · metadata
        Nội dung
        Trả lời
```

Reply:

```text
        Avatar  Name · metadata
                Nội dung
                Trả lời
```

Khoảng cách giữa các comment độc lập nên vừa phải.

Không sử dụng card riêng cho từng comment.

**Giữ nguyên flat comment layout hiện tại.**

---

## 6. Đường phân cấp reply

Nếu đang sử dụng đường dọc để thể hiện reply:

- Giữ đường dọc.
- Màu rất nhẹ.
- Không làm đường quá đậm.
- Không dùng màu accent.
- Chỉ dùng để giúp mắt nhận biết reply thuộc comment nào.

Ví dụ:

```text
Admin
hello everyone

│
└── Mai Đức Quang
    xong rồi
```

Đường này chỉ là visual hierarchy, không được trở thành element nổi bật.

---

## 7. Header

Giữ nguyên header hiện tại:

```text
Tờ trình   STT 1   Đã hoàn thành
Thảo luận & Ghi chú (3 bình luận)

                         [ Lịch sử ] [ × ]
```

Không redesign header.

Chỉ đảm bảo:

- Title nổi bật nhất.
- STT là badge nhỏ.
- Trạng thái là badge nhẹ.
- Subtitle nhỏ hơn title.
- Nút `Lịch sử` rõ nhưng không quá nổi bật.

---

## 8. Input comment

Giữ nguyên vị trí input ở cuối drawer.

Placeholder:

```text
Nhập nội dung bình luận (Gõ @ để tag, Enter để gửi)...
```

Giữ:

- Attachment.
- Enter hint.
- Send button.

Nhưng điều chỉnh interaction:

### Khi chưa nhập nội dung

Nút gửi ở trạng thái disabled/subtle.

### Khi có nội dung

Nút gửi chuyển sang trạng thái active.

Không làm nút gửi quá lớn hoặc quá nổi bật.

Input nên có cảm giác giống một vùng nhập liệu nhẹ nhàng, không phải một card lớn.

---

## 9. Không sử dụng quá nhiều màu

Chỉ sử dụng màu accent cho:

- Mention.
- Một số action quan trọng.
- Trạng thái khi thực sự cần.

Không tô màu background cho toàn bộ comment.

Không tạo mỗi trạng thái một card màu khác nhau.

Giữ phần lớn UI:

- trắng.
- xám nhạt.
- text đậm vừa phải.
- muted text.

---

## 10. Quan trọng: giữ nguyên cấu trúc

Không thay đổi:

- API.
- Data model.
- Business logic.
- Comment/reply functionality.
- Mention functionality.
- Pagination/load more nếu đang có.
- Delete/edit comment.
- History button.
- Attachment functionality.

Chỉ refactor UI/UX và CSS/component structure khi cần.

---

## 11. Kiểm tra cuối cùng

Sau khi chỉnh sửa, kiểm tra màn hình với các trường hợp:

### Comment bình thường

```text
Admin @admin · 09:30 · 11/08/2026

hello everyone

Trả lời
```

### Comment có mention

```text
Admin @admin · 15:52 · 11/08/2026

@Mai Đức Quang alo làm xong chưa

Trả lời
```

### Reply

```text
    Mai Đức Quang @quangmd · trả lời Admin · 16:47

    @Lê Đức Anh xong rồi

    Trả lời
```

Đảm bảo hierarchy nhìn vào là hiểu ngay.

### Kết quả mong muốn

Giao diện sau khi chỉnh sửa phải có cảm giác:

**sạch → nhẹ → dễ scan → ít bold → ít màu → comment là nội dung chính.**

Không thêm các UI element không cần thiết và không over-design.