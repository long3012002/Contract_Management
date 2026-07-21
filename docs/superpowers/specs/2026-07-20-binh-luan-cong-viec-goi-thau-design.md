# Thiết Kế Chi Tiết: Tính Năng Bình Luận Công Việc Gói Thầu (Bid Package Task Comments)

**Ngày khởi tạo:** 2026-07-20  
**Trạng thái:** Đã cập nhật theo phản hồi người dùng (Frontend First + Mock Data)  
**Phạm vi:** Frontend React 19 (UI Components, Tailwind CSS v4, Lucide Icons, Sheet/Drawer, Mock Data State/Local Storage)

---

## 1. Tổng Quan & Mục Tiêu

Tính năng **Bình luận công việc gói thầu** giai đoạn 1 sẽ ưu tiên phát triển **Giao diện (UI/UX) và Mock Data** trên Frontend để người dùng trải nghiệm ngay mà chưa phụ thuộc vào Backend API.

### Yêu cầu chính (User Requirements):
- **Mock Data & State Management:** Sử dụng mock data phong phú với các comment mẫu (tên nhân viên, phòng ban, nội dung, thời gian), lưu tạm vào State/React Query client side.
- **Vị trí UI:** Khi click icon **Comment** ở mỗi dòng công việc trong bảng danh sách (`CongViecTable`), một **Right Sheet / Drawer** trượt ra từ bên phải màn hình.
- **Badge Đếm Số Lượng:** Mỗi dòng công việc trên bảng có icon comment kèm theo **Badge số lượng comment** (Ví dụ: `💬 3`). Nếu chưa có comment nào thì hiển thị số `0` mờ.
- **Định dạng bình luận:** Plain Text (Văn bản thuần túy, có hỗ trợ xuống dòng), hiển thị rõ thông tin người bình luận (Tên, Chức danh/Phòng ban, Avatar viết tắt), thời gian tạo, và nhãn "(đã chỉnh sửa)".
- **Cấu trúc danh sách:** Tuyến tính theo thời gian (Flat List), cuộn mượt xuống bình luận mới nhất.
- **Thao tác tương tác Mock:** Thêm mới comment, Sửa comment, Xóa comment với cập nhật tức thì trên UI và badge đếm.

---

## 2. Kiến Trúc Backend (.NET Web API & EF Core)

### 2.1 Entity: `BinhLuanCongViecGoiThau`
Tạo file entity mới tại `backend/demo1/demo1/Entity/BinhLuanCongViecGoiThau.cs`:

```csharp
using System;

namespace demo1.Entity;

public class BinhLuanCongViecGoiThau : BaseEntity
{
    public Guid CongViecGoiThauId { get; set; }
    public virtual CongViecGoiThau? CongViecGoiThau { get; set; }

    public Guid UserId { get; set; }
    public virtual User? User { get; set; }

    public string NoiDung { get; set; } = string.Empty;
}
```

- Kế thừa từ `BaseEntity` (đã có `Id`, `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`, `IsDeleted`).

### 2.2 Cập nhật DTOs & Mapping
Tạo DTOs tại `backend/demo1/demo1/DTOs/BinhLuanCongViecGoiThau/BinhLuanDto.cs`:

- `BinhLuanDto`:
  - `Guid Id`
  - `Guid CongViecGoiThauId`
  - `Guid UserId`
  - `string NguoiTaoName` (Họ tên hoặc Username người dùng)
  - `string? NguoiTaoChucVu` (Tên chức danh/Phòng ban)
  - `string NoiDung`
  - `DateTime CreatedAt`
  - `DateTime? UpdatedAt`
  - `bool IsEdited` (`UpdatedAt.HasValue && UpdatedAt != CreatedAt`)
  - `bool CanEdit` (True nếu `UserId == currentUserId`)
  - `bool CanDelete` (True nếu `UserId == currentUserId` hoặc `isAdmin`)

- `CreateBinhLuanDto`:
  - `Guid CongViecGoiThauId`
  - `string NoiDung`

- `UpdateBinhLuanDto`:
  - `string NoiDung`

- **Cập nhật `CongViecGoiThauDto`**:
  - Thêm trường `public int SoBinhLuan { get; set; }` để trả về tổng số bình luận của công việc cho bảng danh sách.

### 2.3 Controllers & Endpoints API
Tạo Controller mới tại `backend/demo1/demo1/Controllers/BinhLuanCongViecGoiThausController.cs`:

| HTTP Method | Route Endpoint | Mô tả |
| :--- | :--- | :--- |
| **GET** | `/api/cong-viec-goi-thau/{congViecId}/comments` | Lấy danh sách tất cả bình luận của 1 công việc |
| **POST** | `/api/cong-viec-goi-thau/{congViecId}/comments` | Thêm bình luận mới cho công việc |
| **PUT** | `/api/comments/{id}` | Cập nhật nội dung bình luận (Chỉ tác giả) |
| **DELETE** | `/api/comments/{id}` | Xóa bình luận (Tác giả hoặc Admin) |

---

## 3. Thiết Kế Giao Diện Frontend (React 19 & UI Components)

### 3.1 Cấu Trúc File Frontend
Tạo các component trong thư mục tính năng `frontend/src/features/bid-packages/`:

```
frontend/src/features/bid-packages/
├── api/
│   └── commentApi.js                 # Axios API calls cho comment
├── hooks/
│   └── useCongViecComments.js        # Custom hook chứa TanStack Query logic
└── components/
    └── CongViecGoiThau/
        ├── CongViecTable.jsx          # Cập nhật: Thêm cột/nút Comment + Badge
        ├── CongViecTableRow.jsx       # Cập nhật: Render nút Comment & badge count
        └── Comments/
            ├── CongViecCommentSheet.jsx # Drawer slide-over bên phải
            ├── CommentList.jsx          # Danh sách bình luận
            ├── CommentItem.jsx          # Từng thẻ bình luận (Avatar, Nội dung, Nút sửa/xóa)
            └── CommentInput.jsx         # Ô nhập text + Nút gửi comment
```

### 3.2 Tương Tác UX & Luồng Hoạt Động (User Flow)

1. **Hiển thị trên Bảng Danh Sách Công Việc (`CongViecTableRow`)**:
   - Ở mỗi hàng công việc, thêm button action chứa icon `MessageSquare` + Badge số lượng (ví dụ: `💬 3`).
   - Khi hover vào nút comment, tooltip hiển thị "Xem & Thêm bình luận".
   - Bấm vào nút này mở `CongViecCommentSheet` trượt từ cạnh phải màn hình qua.

2. **Giao diện Drawer (`CongViecCommentSheet`)**:
   - **Header**:
     - Tiêu đề: "Thảo luận & Bình luận"
     - Subtitle: Tên công việc (Ví dụ: `STT 1. Hợp đồng thi công xây dựng`)
     - Badge Tình trạng công việc (Hoàn thành / Đang thực hiện / Chưa thực hiện)
     - Nút đóng Drawer (`X`)
   - **Body (`CommentList`)**:
     - Danh sách hiển thị theo thứ tự thời gian tăng dần (cũ nhất ở trên, mới nhất ở dưới).
     - Mỗi item (`CommentItem`) gồm:
       - Avatar viết tắt họ tên (ví dụ: "NV" cho Nguyen Van A) với màu sắc ngẫu nhiên/hài hòa.
       - Tên người bình luận + Chức danh/Phòng ban.
       - Thời gian đăng (dạng friendly format: `10 phút trước` hoặc `14:30 20/07/2026`).
       - Thao tác: Nút 3 chấm hoặc nút Sửa/Xóa mờ (chỉ xuất hiện khi rê chuột hoặc đúng quyền).
       - Nội dung bình luận (Hỗ trợ xuống dòng `white-space: pre-wrap`).
     - Tự động cuộn mượt xuống bình luận mới nhất (`scrollIntoView`) khi mở Drawer hoặc sau khi gửi comment mới.
   - **Footer (`CommentInput`)**:
     - Ô `Textarea` tự động co giãn dòng, placeholder "Nhập bình luận của bạn (Ctrl + Enter để gửi)...".
     - Nút "Gửi" (`Send` icon) có hiệu ứng loading khi đang gửi.

---

## 4. Quản Lý State & React Query (`useCongViecComments`)

Theo đúng quy định dự án (`AGENTS.md`): Không gọi directamente `useQuery` / `useMutation` ở UI component, mà gói toàn bộ trong custom hook `useCongViecComments(congViecId)`.

### Logic Hook `useCongViecComments`:
- **Query Key:** `['cong-viec-comments', congViecId]`
- **Mutations:**
  - `createComment`: Sau khi tạo thành công -> Invalidate `['cong-viec-comments', congViecId]` VÀ `['cong-viec-goi-thau', goiThauId]` (để cập nhật lại số lượng `SoBinhLuan` trên bảng).
  - `updateComment`: Invalidate `['cong-viec-comments', congViecId]`.
  - `deleteComment`: Invalidate `['cong-viec-comments', congViecId]` VÀ `['cong-viec-goi-thau', goiThauId]`.

---

## 5. Kế Hoạch Kiểm Thử & Xác Nhận (Verification Plan)

### Kiểm thử tự động / Build & Lint:
- Chạy `dotnet build` backend để đảm bảo không lỗi biên dịch DB Migration & Controller mới.
- Chạy `npm run build` hoặc check ESLint frontend để đảm bảo không có warning/error.

### Kiểm thử thủ công (Manual Testing):
1. **Kiểm tra Badge:** Mở danh sách công việc của 1 gói thầu, kiểm tra số lượng `SoBinhLuan` hiển thị chính xác trên từng dòng.
2. **Mở Drawer:** Bấm nút comment trên công việc -> Drawer trượt từ bên phải mượt mà.
3. **Đăng Bình Luận:** Nhập comment văn bản -> Bấm Gửi -> Comment xuất hiện ngay lập tức, ô nhập reset, badge số lượng trên bảng tăng +1.
4. **Sửa / Xóa Bình Luận:** 
   - Kiểm tra user tạo comment có nút Sửa/Xóa.
   - Chỉnh sửa nội dung -> Hiển thị nhãn "(đã chỉnh sửa)".
   - Xóa comment -> Confirm dialog xuất hiện -> Xóa thành công -> Badge giảm -1.
