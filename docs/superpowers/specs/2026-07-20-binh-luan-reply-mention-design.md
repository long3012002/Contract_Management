# Thiết Kế Chi Tiết: Tính Năng Rep Comment & Tag Mention (@User) Cho Công Việc Gói Thầu

**Ngày khởi tạo:** 2026-07-20  
**Trạng thái:** Đã cập nhật theo phản hồi người dùng (Spec Review)  
**Phạm vi:** Frontend React 19 (UI Components, Tailwind CSS v4, Lucide Icons, Sheet/Drawer, Local Storage Mock Data)

---

## 1. Tổng Quan & Mục Tiêu

Bổ sung 2 tính năng tương tác nâng cao cho phần Bình luận Công việc Gói thầu:
1. **Trả lời Bình luận (Reply / Threading)**: Cho phép thảo luận theo nhánh 1 cấp (1-level nested structure), phản hồi trực tiếp một comment của đồng nghiệp.
2. **Tag Người Dùng (@mention)**: Gõ `@` trong ô nhập xuất hiện menu gợi ý danh sách nhân viên trong hệ thống. Bình luận đã đăng hiển thị tag nổi bật (`@Tên Nhân Viên` với nền mờ primary).

---

## 2. Thiết Kế Cấu Trúc Dữ Liệu & State (Mock Data)

### 2.1 Cấu trúc Bình luận (`BinhLuanModel`)
Bổ sung các thuộc tính phục vụ Reply & Tag:

```javascript
{
  id: 'comment-123',
  congViecGoiThauId: 'task-456',
  parentId: null, // null nếu là comment gốc, hoặc commentId cha nếu là reply
  userId: 'user-001',
  userName: 'Phạm Đức Anh',
  userRole: 'Ban Quản lý Dự án',
  userAvatar: 'DA',
  noiDung: 'Nhờ @Nguyễn Thanh Tùng kiểm tra phụ lục hợp đồng nhé.',
  mentions: [
    { userId: 'usr-002', userName: 'Nguyễn Thanh Tùng' }
  ],
  createdAt: '2026-07-20T14:30:00.000Z',
  updatedAt: null,
  replies: [] // Danh sách bình luận con (nếu là comment gốc)
}
```

---

## 3. Thiết Kế Giao Diện UI/UX

### 3.1 Phân Nhánh Trả Lời (Reply UI)
- **Nút "Trả lời"**: Xuất hiện bên cạnh mốc thời gian và nút Sửa/Xóa của từng `CommentItem`.
- **Trạng thái Trọng tâm Ô nhập (Replying Banner)**:
  - Khi bấm "Trả lời" một comment, ô nhập ở Footer Sheet xuất hiện một thanh nhãn nhỏ:
    `Đang trả lời Phạm Đức Anh` kèm nút `X` để hủy.
  - Con trỏ tự động focus vào ô nhập text.
- **Hiển thị Nhánh con (Nested Replies)**:
  - Các bình luận con (`replies`) được xếp lề lùi vào trong (`pl-6 sm:pl-8 border-l-2 border-primary/20`) nằm ngay bên dưới comment gốc tương ứng.
  - Tổng số reply hiển thị bằng nút thu gọn/mở rộng nếu cần (Ví dụ: `Ẩn 2 phản hồi` / `Xem 2 phản hồi`).

### 3.2 Menu Gợi Ý & Highlight Tag (`@mention`)
- **Menu Dropdown Gợi ý khi gõ `@` (`MentionDropdown`)**:
  - Khi người dùng gõ `@` trong ô nhập text, hiển thị ngay popover gợi ý danh sách nhân viên từ `MOCK_USERS`.
  - Hỗ trợ lọc theo tên khi tiếp tục gõ (Ví dụ: `@Tùng` -> Lọc hiển thị Nguyễn Thanh Tùng).
  - Điều hướng bàn phím: Dùng phím mũi tên `↑` `↓` để di chuyển vệt sáng, phím `Enter` hoặc click để chọn.
  - Sau khi chọn: Tên `@Nguyễn Thanh Tùng` tự động chèn vào nội dung.
- **Render Tag Nổi bật trong Nội dung Bình luận**:
  - Tên `@Tên Nhân Viên` được parse và hiển thị dưới dạng badge xanh brand cuốn hút:
    `font-semibold text-primary bg-primary/10 px-1.5 py-0.5 rounded-md`
  - Giúp người dùng lướt nhanh nội dung và nhận biết ngay ai đang được gọi tên.

---

## 4. Danh Sách Component Cần Triển Khai / Cập Nhật

```
frontend/src/features/bid-packages/
├── hooks/
│   └── useCongViecComments.js        # Cập nhật: Thêm parentId support, replies nesting logic, mention parsing
└── components/
    └── CongViecGoiThau/
        └── Comments/
            ├── MentionDropdown.jsx    # [NEW] Dropdown popup gợi ý nhân viên khi gõ @
            ├── CommentInput.jsx       # Cập nhật: Tích hợp trigger @, replying banner, keyboard navigation
            ├── CommentItem.jsx        # Cập nhật: Nút Reply, render nested replies, format text highlight @mentions
            └── CommentList.jsx        # Cập nhật: Group comments gốc và replies tương ứng
```

---

## 5. Kế Hoạch Kiểm Thử (Verification Plan)

1. **Kiểm tra Reply**:
   - Bấm nút "Trả lời" ở Comment A -> Footer xuất hiện "Đang trả lời Comment A".
   - Nhập nội dung và Gửi -> Comment mới xuất hiện thụt lùi lề trái ngay bên dưới Comment A.
2. **Kiểm tra Tag `@mention`**:
   - Gõ `@` trong ô nhập -> Bật popup danh sách nhân viên.
   - Gõ tiếp `@tùng` -> Popup lọc ra Nguyễn Thanh Tùng -> Ấn Enter -> Tên được điền tự động.
   - Bấm Gửi -> Bình luận hiển thị nhãn `@Nguyễn Thanh Tùng` được highlight màu xanh nổi bật.
