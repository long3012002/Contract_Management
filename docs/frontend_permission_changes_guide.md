# Hướng Dẫn Cập Nhật Phân Quyền Phân Cấp (Hierarchical Permission) Cho Frontend

Tài liệu này tổng hợp chi tiết tất cả các thay đổi từ Backend (.NET 8) về cấu trúc dữ liệu, mã tính năng (Feature Code), API và hướng dẫn sửa đổi code trên **Frontend (React 19)**.

---

## 1. Thay Đổi Cấu Trúc API & Dữ Liệu Trao Đổi

Backend đã cập nhật các API quản lý tính năng và ma trận vai trò, bổ sung 2 trường dữ liệu mới:
- `parentCode` (`string | null`): Mã tính năng cha (`"BAO_CAO"`, hoặc `null` nếu là tính năng cấp cao nhất).
- `sortOrder` (`number`): Thứ tự sắp xếp hiển thị.

### Danh sách API bị ảnh hưởng:
1. `GET /api/HeThong/admin/features/catalog`
2. `GET /api/HeThong/admin/features`
3. `GET /api/HeThong/admin/roles/{roleId}/permissions`

### Cấu trúc Schema Feature / RolePermission mới:
```json
{
  "featureId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "featureCode": "BAO_CAO_TIEN_DO",
  "featureName": "Báo cáo 1: Tiến độ Dự án",
  "parentCode": "BAO_CAO",
  "sortOrder": 61,
  "canAccess": true,
  "permissions": "VIEW"
}
```

---

## 2. Danh Mục Mã Tính Năng (Feature Codes) Mới

### 2.1. Nhóm Danh Mục Dữ Liệu Độc Lập
| Feature Code | Feature Name | ParentCode | Ghi chú |
| :--- | :--- | :--- | :--- |
| `DANH_MUC` | Quản lý Danh mục dữ liệu | `null` | Phân quyền cho Loại dự án, Nguồn vốn, Loại hợp đồng, Nhóm dự án |

### 2.2. Nhóm Báo Cáo & Thống Kê (Cấu trúc Cây / Tree)
| Feature Code | Feature Name | ParentCode | Route Frontend Tương Ứng |
| :--- | :--- | :--- | :--- |
| `BAO_CAO` | **Báo cáo & Thống kê (Nhóm cha)** | `null` | Menu cha trên Sidebar |
| `BAO_CAO_TIEN_DO` | Báo cáo 1: Tiến độ Dự án | `BAO_CAO` | `/reports?tab=progress` |
| `BAO_CAO_VON` | Báo cáo 2: Phân bổ & Vốn | `BAO_CAO` | `/reports?tab=capital` |
| `BAO_CAO_DAU_THAU` | Báo cáo 3: Nhà thầu (LCNT) | `BAO_CAO` | `/reports?tab=bidding` |
| `BAO_CAO_HOP_DONG` | Báo cáo 4: Quản lý Hợp đồng | `BAO_CAO` | `/reports?tab=contracts` |
| `BAO_CAO_THANH_TOAN` | Báo cáo 5: Đợt thanh toán | `BAO_CAO` | `/reports?tab=payments` |
| `BAO_CAO_DU_AN_THAU` | Báo cáo 6: TT Dự án thầu | `BAO_CAO` | `/reports?tab=bidding-payments` |
| `BAO_CAO_DAU_TU` | Báo cáo Tổng hợp Đầu tư | `BAO_CAO` | `/reports/investment` |
| `BAO_CAO_PHE_DUYET` | Danh mục Dự án phê duyệt | `BAO_CAO` | `/reports/approved-projects` (hoặc Hạn License/SLA) |

> [!NOTE]
> **Cơ chế kế thừa từ Backend:** Nếu người dùng có quyền `BAO_CAO` (quyền cha), Backend sẽ tự động cấp quyền xem cho tất cả 8 báo cáo con. Ngược lại, Admin có thể tích chọn từng báo cáo con cụ thể.

---

## 3. Các Nhiệm Vụ Cụ Thể Cần Sửa Trên Frontend

### Task 1: Cập nhật Ma Trận Phân Quyền (`PermissionMatrix.jsx`)
- **Chuyển đổi dữ liệu dạng Cây (Tree Transformation):**
  - Nhóm các tính năng có `parentCode === "BAO_CAO"` lùi vào trong làm con của dòng `BAO_CAO`.
  - Nhóm `DANH_MUC` hiển thị thành 1 dòng riêng độc lập với 4 checkbox View, Create, Edit, Delete.
- **Giao diện Accordion / Mở sổ dòng (Collapsible Sub-tree):**
  - Dòng cha **Báo cáo & Thống kê** có nút mũi tên (Chevron) để mở rộng/thu gọn 8 báo cáo con.
  - Tích chọn nút xem trên từng báo cáo con để lưu quyền chi tiết.

### Task 2: Cập nhật Constants Phân Quyền (`permissions.js`)
Thêm các hằng số mã tính năng mới vào file constants:
```javascript
export const PERMISSIONS = {
  // ... các quyền hiện có
  CATEGORY: 'DANH_MUC',
  REPORT: 'BAO_CAO',
  REPORT_PROGRESS: 'BAO_CAO_TIEN_DO',
  REPORT_CAPITAL: 'BAO_CAO_VON',
  REPORT_BIDDING: 'BAO_CAO_DAU_THAU',
  REPORT_CONTRACT: 'BAO_CAO_HOP_DONG',
  REPORT_PAYMENT: 'BAO_CAO_THANH_TOAN',
  REPORT_BIDDING_PAYMENT: 'BAO_CAO_DU_AN_THAU',
  REPORT_INVESTMENT: 'BAO_CAO_DAU_TU',
  REPORT_APPROVED: 'BAO_CAO_PHE_DUYET',
};
```

### Task 3: Cập nhật Sidebar Navigation (`navigationConfig.js` & `navigationUtils.js`)
- Đổi cấu hình menu **Danh mục dữ liệu** sử dụng `feature: PERMISSIONS.CATEGORY`.
- Cập nhật hàm lọc menu `filterNavigationGroupsByPermission`:
  - Mục menu cha **Báo cáo** sẽ hiển thị nếu người dùng có quyền ở `BAO_CAO` **hoặc bất kỳ 1 trong 8 báo cáo con** (`BAO_CAO_*`).
  - Các mục menu con/tab trong trang Báo cáo chỉ hiển thị đúng những báo cáo mà tài khoản được cấp quyền.

### Task 4: Cập nhật Route Guards (`App.jsx`)
- Khóa các trang danh mục bằng `<PermissionGuard feature={PERMISSIONS.CATEGORY} />`:
  - Route `/project-types` (Loại dự án)
  - Route `/capital-sources` (Nguồn vốn)
  - Route `/contract-types` (Loại hợp đồng)
  - Route `/project-groups` (Nhóm dự án)
- Khóa các trang/tab báo cáo bằng mã tương ứng (`feature={PERMISSIONS.REPORT_INVESTMENT}`, `feature={PERMISSIONS.REPORT_PROGRESS}`, v.v.).

### Task 5: Cập nhật Bộ Tab Báo Cáo (`ReportsHubPage.jsx`)
- Trong `ReportsHubPage.jsx`, kiểm tra quyền người dùng đối với từng tab báo cáo (`BAO_CAO_TIEN_DO`, `BAO_CAO_VON`...).
- Tự động ẩn các Tab mà người dùng không được cấp quyền. Nếu truy cập đường dẫn trực tiếp tab không có quyền -> hiển thị thông báo hoặc điều hướng về Tab đầu tiên có quyền.

---

## 4. Tóm Tắt Quy Trình Kiểm Thử Cho FE (Test Cases)

1. **Test Admin:** Đăng nhập tài khoản Admin -> Xem đầy đủ menu Danh mục dữ liệu & tất cả các Báo cáo.
2. **Test Nhân viên thường (Không có quyền Danh mục):** Không nhìn thấy mục Danh mục dữ liệu trên Sidebar; truy cập URL `/project-types` bị đẩy về `/forbidden`.
3. **Test Phân rã Báo cáo con:**
   - Vào Ma trận phân quyền, chỉ tick chọn **Báo cáo 1: Tiến độ** và **Báo cáo 2: Vốn** cho 1 Role.
   - Đăng nhập tài khoản thuộc Role đó -> Trên Sidebar và trang `/reports` chỉ thấy 2 Tab Báo cáo 1 và Báo cáo 2, các tab báo cáo khác bị ẩn.
