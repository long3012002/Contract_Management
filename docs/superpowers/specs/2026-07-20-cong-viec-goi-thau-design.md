# Design Document: Quản lý Danh sách Công việc Gói Thầu (Bid Package Tasks UI/UX)

**Date**: 2026-07-20  
**Feature**: CongViecGoiThau (Bid Package Tasks Management)  
**Scope**: Frontend (`frontend/src/features/bid-packages`)  

---

## 1. Mục tiêu & Yêu cầu (Goals & Requirements)

1. **Hiển thị danh sách công việc của Gói thầu**: Khi người dùng xem chi tiết bất kỳ gói thầu nào (từ trang Sidebar "Gói thầu" hoặc từ Tab "Gói thầu" trong "Dự án triển khai"), hệ thống hiển thị Modal Chi tiết Gói thầu với danh sách các công việc thuộc gói thầu đó.
2. **Thống kê & Trạng thái**: Hiển thị card thống kê tiến độ (Tổng số công việc, số công việc đã hoàn thành, số công việc đang thực hiện) và Badge màu sắc tương ứng với tình trạng từng công việc.
3. **Phân quyền người dùng (RBAC)**:
   - **Người dùng chỉ xem (Read-only)**: Xem được danh sách công việc và thống kê. Các nút Thêm / Sửa / Xóa công việc sẽ bị ẩn.
   - **Người dùng có quyền (CRUD)**: Có thể Thêm công việc mới, Chỉnh sửa thông tin công việc, và Xóa công việc.
4. **Đồng nhất UI & Tái sử dụng**:
   - Tách biệt tuyệt đối giữa UI (Presentational components) và Business Logic (Custom hooks).
   - Tách nhỏ các component (Stats Cards, Task Table, Task Row, Task Form Modal, Detail Modal).
   - Sử dụng chung `BidPackageDetailModal` cho cả 2 luồng truy cập (Sidebar & Project Detail).

---

## 2. Thiết kế Kiến trúc Code & Folder Structure

Tất cả code nằm trong `frontend/src/features/bid-packages`:

```
frontend/src/features/bid-packages/
├── api/
│   ├── bidPackagesApi.js
│   └── congViecGoiThauApi.js            # [NEW] API calls cho cong-viec-goi-thau
├── constants/
│   ├── bidPackageConstants.js
│   └── congViecConstants.js             # [NEW] Enums, TinhTrang options, badges color mappings
├── hooks/
│   ├── useBidPackages.js
│   ├── useCongViecGoiThau.js            # [NEW] Main hook quản lý danh sách & thao tác CRUD công việc
│   ├── useCongViecGoiThauReport.js      # [NEW] Hook lấy thống kê tiến độ công việc
│   └── useCongViecForm.js              # [NEW] Hook xử lý logic form (validation, submit, reset)
├── components/
│   ├── BidPackageByProject.jsx
│   ├── BidPackageListTable.jsx
│   ├── BidPackageTableRow.jsx
│   ├── BidPackagesPage.jsx
│   ├── BidPackageDetailModal.jsx        # [NEW] Modal chi tiết dùng chung
│   └── CongViecGoiThau/                 # [NEW] Folder chứa các sub-components công việc
│       ├── CongViecGoiThauSection.jsx   # Container tab công việc
│       ├── CongViecStatsCards.jsx       # Component 3 thẻ thống kê
│       ├── CongViecTable.jsx            # Component bảng danh sách công việc
│       ├── CongViecTableRow.jsx         # Component từng hàng công việc
│       ├── CongViecStatusBadge.jsx      # Component badge tình trạng
│       └── CongViecFormModal.jsx        # Modal form thêm / sửa công việc
```

---

## 3. Phân tách Logic & UI (Separation of Concerns)

### A. Presentation Components (Chỉ nhận Props & Emit events)
- **`CongViecStatusBadge`**: Nhận `tinhTrang` ➔ Render Badge màu sắc (Emerald cho Đã hoàn thành/Đã ký, Blue cho Đang thực hiện, Amber/Gray cho Chưa thực hiện).
- **`CongViecStatsCards`**: Nhận `report` (`tongSoCongViec`, `soCongViecDaHoanThanh`, `soCongViecDangThucHien`) ➔ Render 3 thẻ stat count.
- **`CongViecTableRow`**: Nhận `item`, `stt`, `canEdit`, `onEdit`, `onDelete` ➔ Render 1 hàng dữ liệu + nút bấm thao tác nếu `canEdit = true`.
- **`CongViecTable`**: Nhận `items`, `loading`, `canEdit`, `onEdit`, `onDelete` ➔ Render bảng công việc hoặc empty state/loading skeleton.
- **`CongViecFormModal`**: Nhận `isOpen`, `onClose`, `onSubmit`, `initialValues`, `isSubmitting` ➔ Render Dialog Form thêm/sửa công việc.
- **`BidPackageDetailModal`**: Nhận `isOpen`, `onClose`, `packageId`, `canEdit` ➔ Render Modal container với Tabs (Thông tin chung & Danh sách công việc).

### B. Custom Hooks (Quản lý State, Data Fetching & Side-Effects)
- **`useCongViecGoiThau(goiThauId)`**:
  - Quản lý React Query / state lấy danh sách công việc theo `goiThauId`.
  - Quản lý các mutation: `createMutation`, `updateMutation`, `deleteMutation`.
  - Trả về: `{ tasks, isLoading, isError, report, refetch, createTask, updateTask, deleteTask }`.
- **`useCongViecForm(initialValues, onSubmit)`**:
  - Quản lý form state, validation, reset form khi đóng/mở modal.

---

## 4. Chi tiết API Integration

Endpoint backend: `/api/cong-viec-goi-thau`

1. **Get List**: `GET /api/cong-viec-goi-thau/by-goi-thau/{idGoiThau}`
2. **Get Report**: `GET /api/cong-viec-goi-thau/by-goi-thau/{idGoiThau}/report`
3. **Create Task**: `POST /api/cong-viec-goi-thau`
   Payload: `{ goiThauId, stt, tenTaiLieu, loaiVanBan, ngayKy, tinhTrang, ghiChu }`
4. **Update Task**: `PUT /api/cong-viec-goi-thau/{id}`
   Payload: `{ goiThauId, stt, tenTaiLieu, loaiVanBan, ngayKy, tinhTrang, ghiChu }`
5. **Delete Task**: `DELETE /api/cong-viec-goi-thau/{id}`

---

## 5. Phân quyền (Permission Matrix)

| Chức năng | Người chỉ xem (`canEdit = false`) | Người có quyền (`canEdit = true`) |
|---|---|---|
| Xem thống kê tiến độ | ✅ Có | ✅ Có |
| Xem bảng danh sách công việc | ✅ Có | ✅ Có |
| Nút `+ Thêm công việc` | ❌ Ẩn | ✅ Hiển thị |
| Nút `Chỉnh sửa` công việc | ❌ Ẩn | ✅ Hiển thị |
| Nút `Xóa` công việc | ❌ Ẩn | ✅ Hiển thị |

---

## 6. Kế hoạch Kiểm thử (Verification Plan)

1. **Kiểm tra UI/UX đồng nhất**:
   - Mở gói thầu từ trang Sidebar ➔ `BidPackageDetailModal` hiển thị chuẩn.
   - Mở gói thầu từ trang Dự án triển khai ➔ `BidPackageDetailModal` hiển thị giống hệt 100%.
2. **Kiểm tra Phân quyền**:
   - Với tài khoản Viewer (không có quyền edit): Đảm bảo các nút Thêm/Sửa/Xóa bị ẩn.
   - Với tài khoản Admin/Manager: Đảm bảo thực hiện đầy đủ CRUD công việc.
3. **Kiểm tra Thao tác dữ liệu**:
   - Thêm 1 công việc mới ➔ Bảng tự reload + Thống kê tự cập nhật.
   - Sửa 1 công việc ➔ Dữ liệu cập nhật đúng dòng.
   - Xóa 1 công việc ➔ Confirm Dialog hiển thị và xóa thành công.
