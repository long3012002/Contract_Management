# Hướng Dẫn Cập Nhật Phân Quyền Phân Cấp (Hierarchical Permission) Cho Frontend

Tài liệu này tổng hợp chi tiết tất cả các nâng cấp Backend (.NET 8) vừa thực hiện để **giải quyết triệt để cả 5 bất cập** mà Frontend team đã phản hồi.

---

## 1. Giải Quyết 5 Bất Cập Của Frontend (Backend Enhancements)

| STT | Vấn đề FE phản hồi | Giải pháp Backend đã nâng cấp | Kết quả cho FE |
| :--- | :--- | :--- | :--- |
| **1** | **Bị thiếu Feature Code (chỉ có 7 mã cũ):** FE phải gánh tự merge mock catalog. | Backend đã sửa `CreateFakeData.cs` đồng bộ tự động `ProductionSeeder` mỗi khi khởi chạy. API trả về đủ 15 tính năng bao gồm `DANH_MUC` và 8 báo cáo con. | **FE không cần mock catalog hay gán mảng cứng nữa.** API trả về đúng 100% dữ liệu chuẩn. |
| **2** | **Phải tự suy đoán tiền tố & dựng cây:** FE phải viết logic `code.startsWith('BAO_CAO_')` và `buildTreeFeatures`. | Backend bổ sung API `GET /api/HeThong/admin/features/tree` và tham số `GET /api/HeThong/admin/features/catalog?tree=true` trả về sẵn danh mục phân cấp cấu trúc Cây (`children: []`). | **FE gọi API lấy luôn danh sách Tree Data sẵn**, không cần tự phân cấp lùi lề. |
| **3** | **Schema không đồng nhất:** Lúc trả `code`/`featureCode`, `id`/`featureId`, `name`/`featureName`. | Backend đã quy chuẩn 100% các DTOs trả về song song các thuộc tính chuẩn hóa camelCase: `featureId` (`id`), `featureCode` (`code`), `featureName` (`name`), `parentCode`, `sortOrder`, `children`. | **FE không lo bị undefined hay crash.** |
| **4** | **Phải tự parse/join chuỗi Ma trận quyền:** FE phải tự `split(';')` / `join(';')`. | Backend đã bổ sung sẵn 4 cờ boolean trực tiếp trong DTO: `canAccess`, `canView`, `canCreate`, `canEdit`, `canDelete`. | **FE binding trực tiếp vào Checkbox UI** mà không cần split/join chuỗi. API `PUT` chấp nhận cả cờ boolean lẫn chuỗi. |
| **5** | **Ràng buộc logic hành động:** FE phải tự hủy/tick Xem khi chọn Thêm/Sửa/Xóa. | Backend đã tự động validate và áp dụng ràng buộc logic phân quyền trực tiếp khi tiếp nhận payload `PUT`. | Nếu tick Thêm/Sửa/Xóa, BE tự động bật Xem. Ngược lại nếu tắt Xem, BE tự động bỏ Thêm/Sửa/Xóa. |

---

## 2. Chi Tiết API Nâng Cấp Chi Trực Tiếp Dành Cho Frontend

### 2.1. API Lấy Danh Mục Cây (Tree Data API)
- **Endpoint:** `GET /api/HeThong/admin/features/tree` hoặc `GET /api/HeThong/admin/features/catalog?tree=true`
- **Response sample:**
```json
[
  {
    "featureId": "11111111-1111-1111-1111-111111111111",
    "featureCode": "DANH_MUC",
    "featureName": "Quản lý Danh mục dữ liệu",
    "description": "Quản lý các loại dự án, nguồn vốn, loại hợp đồng, nhóm dự án",
    "parentCode": null,
    "sortOrder": 50,
    "children": []
  },
  {
    "featureId": "22222222-2222-2222-2222-222222222222",
    "featureCode": "BAO_CAO",
    "featureName": "Báo cáo & Thống kê",
    "description": "Nhóm chức năng báo cáo tổng hợp & chi tiết",
    "parentCode": null,
    "sortOrder": 60,
    "children": [
      {
        "featureId": "33333333-3333-3333-3333-333333333333",
        "featureCode": "BAO_CAO_TIEN_DO",
        "featureName": "Báo cáo 1: Tiến độ Dự án",
        "description": "Báo cáo trình tự thực hiện các công việc thuộc gói thầu và dự án",
        "parentCode": "BAO_CAO",
        "sortOrder": 61,
        "children": []
      }
    ]
  }
]
```

---

### 2.2. API Lấy Ma Trận Phân Quyền Vai Trò (Role Permissions)
- **Endpoint:** `GET /api/HeThong/admin/roles/{roleId}/permissions`
- **Response sample (Đã có sẵn 4 cờ Boolean):**
```json
[
  {
    "featureId": "33333333-3333-3333-3333-333333333333",
    "featureCode": "BAO_CAO_TIEN_DO",
    "featureName": "Báo cáo 1: Tiến độ Dự án",
    "parentCode": "BAO_CAO",
    "sortOrder": 61,
    "canAccess": true,
    "canView": true,
    "canCreate": false,
    "canEdit": false,
    "canDelete": false,
    "permissions": "VIEW"
  }
]
```

---

### 2.3. API Cập Nhật Ma Trận Phân Quyền (Update Role Permissions)
- **Endpoint:** `PUT /api/HeThong/admin/roles/{roleId}/permissions`
- **Payload FE gửi lên vô cùng đơn giản:**
```json
[
  {
    "featureId": "33333333-3333-3333-3333-333333333333",
    "canAccess": true,
    "canView": true,
    "canCreate": true,
    "canEdit": false,
    "canDelete": false
  }
]
```

---

## 3. Danh Mục Mã Feature Code Hoàn Chỉnh

| Feature Code | Feature Name | ParentCode | Route FE Tương Ứng |
| :--- | :--- | :--- | :--- |
| `DU_AN` | Quản lý dự án | `null` | `/projects` |
| `GOI_THAU` | Quản lý gói thầu | `null` | `/packages` |
| `QUAN_LY_HOP_DONG` | Quản lý hợp đồng | `null` | `/contracts` |
| `DOI_TAC` | Quản lý đối tác | `null` | `/partners` |
| `DANH_MUC` | Quản lý Danh mục dữ liệu | `null` | `/project-types`, `/capital-sources`, `/contract-types`, `/project-groups` |
| `BAO_CAO` | **Báo cáo & Thống kê (Cha)** | `null` | Menu cha trên Sidebar |
| `BAO_CAO_TIEN_DO` | Báo cáo 1: Tiến độ Dự án | `BAO_CAO` | `/reports?tab=progress` |
| `BAO_CAO_VON` | Báo cáo 2: Phân bổ & Vốn | `BAO_CAO` | `/reports?tab=capital` |
| `BAO_CAO_DAU_THAU` | Báo cáo 3: Nhà thầu (LCNT) | `BAO_CAO` | `/reports?tab=bidding` |
| `BAO_CAO_HOP_DONG` | Báo cáo 4: Quản lý Hợp đồng | `BAO_CAO` | `/reports?tab=contracts` |
| `BAO_CAO_THANH_TOAN` | Báo cáo 5: Đợt thanh toán | `BAO_CAO` | `/reports?tab=payments` |
| `BAO_CAO_DU_AN_THAU` | Báo cáo 6: TT Dự án thầu | `BAO_CAO` | `/reports?tab=bidding-payments` |
| `BAO_CAO_DAU_TU` | Báo cáo Tổng hợp Đầu tư | `BAO_CAO` | `/reports/investment` |
| `BAO_CAO_PHE_DUYET` | Danh mục Dự án phê duyệt | `BAO_CAO` | `/reports/approved-projects` |
