# Tài liệu Đặc tả Phân Quyền (RolePermission & PhongBanPermission)

Tài liệu này đặc tả chi tiết cơ chế hoạt động của các trường **`CanAccess`** và **`Permissions`** trong bảng `RolePermission` và `PhongBanPermission`, phục vụ việc thiết kế và lập trình giao diện người dùng (Frontend UI/UX).

---

## 1. Bản chất & Sự khác biệt giữa 2 Bảng Phân Quyền

Hệ thống quản lý phân quyền theo 2 trục chính:

| Bảng | Đối tượng tác động | Ý nghĩa nghiệp vụ |
| :--- | :--- | :--- |
| **`RolePermission`** | **Vai trò (Roles)** <br>*(ví dụ: Admin, Manager, Staff)* | Phân quyền dựa trên **chức danh nghiệp vụ**. Khi một người dùng được gán một vai trò, họ sẽ thừa hưởng các quyền này. |
| **`PhongBanPermission`** | **Phòng ban (PhongBan)** <br>*(ví dụ: Phòng CNTT, Phòng Kế toán)* | Phân quyền dựa trên **đơn vị công tác**. Các nhân viên thuộc phòng ban đó sẽ có các đặc quyền riêng liên quan đến phạm vi công việc của phòng ban. |

Cả hai bảng này đều có cấu trúc trường tương đương khi cấu hình quyền cho một **Chức năng (Feature/Module)**:
*   `RoleId` / `PhongBanId`: Khóa ngoại xác định đối tượng được phân quyền.
*   `FeatureId`: Khóa ngoại xác định Chức năng (ví dụ: `Dự án`, `Hợp đồng`, `Gói thầu`).
*   `CanAccess` (Boolean): Quyền truy cập/xem cơ bản.
*   `Permissions` (String): Quyền thao tác chi tiết (ngăn cách bởi dấu chấm phẩy `;`).

---

## 2. Chi Tiết Các Trường Quyền Hạn (Backend & Frontend Mapping)

### 2.1. Trường `CanAccess` (Kiểu dữ liệu: `Boolean`)

*   **Định nghĩa**: Quyền hạn cho phép người dùng nhìn thấy và truy cập vào trang/module chức năng đó ở mức tối thiểu (Read-Only / View).
*   **Ý tưởng xử lý Frontend**:
    *   **Hiển thị Menu/Sidebar Navigation**: Nếu `CanAccess === true`, hiển thị menu của Chức năng đó trên Sidebar. Nếu `CanAccess === false`, ẩn hoàn toàn menu.
    *   **Quản lý Route (Routing Guard)**: Chặn không cho người dùng truy cập trực tiếp bằng URL (ví dụ: gõ `/projects` trên trình duyệt) bằng cách kiểm tra quyền `CanAccess` của module tương ứng. Nếu không có quyền, chuyển hướng về trang `403 Forbidden`.
    *   **Trạng thái hoạt động**: Đây là điều kiện tiên quyết (Prerequisite). Nếu `CanAccess` chưa được kích hoạt (`false`), tất cả các hành động chi tiết khác trong mục `Permissions` mặc định sẽ vô hiệu hóa (disabled).

---

### 2.2. Trường `Permissions` (Kiểu dữ liệu: `String` - Định dạng: `"Action1;Action2;Action3"`)

*   **Định nghĩa**: Chuỗi chứa các hành động cụ thể mà người dùng được phép thực hiện trên chức năng đó. Các hành động cách nhau bởi dấu chấm phẩy `;`.
*   **Ví dụ chuỗi**: `"Create;Update;Delete"`, `"Create;Update;Approve;Export"`, hoặc rỗng `""`.
*   **Các hành động chuẩn hệ thống (Actions)**:
    1.  `Create`: Thêm mới dữ liệu.
    2.  `Update`: Sửa đổi dữ liệu hiện có.
    3.  `Delete`: Xóa dữ liệu.
    4.  `Approve`: Phê duyệt (áp dụng cho Hợp đồng, Nghị quyết).
    5.  `Export`: Xuất file báo cáo (Excel, PDF).

*   **Ý tưởng xử lý Frontend**:
    *   **Hiện/Ẩn nút bấm (Button Control)**:
        *   If chuỗi `Permissions` chứa từ khóa `"Create"`, hiển thị nút `[Thêm mới]`. Ngược lại, ẩn nút này.
        *   Tương tự cho các nút `[Sửa]`, `[Xóa]`, `[Phê duyệt]`, `[Xuất báo cáo]`.
    *   **Phân tích chuỗi (Parsing) khi hiển thị**: Khi tải trang cấu hình phân quyền, frontend cần chuyển đổi chuỗi này thành một mảng để bật/tắt các Checkbox tương ứng.
    *   **Ghép chuỗi (Stringify) khi lưu**: Khi Admin thay đổi các checkbox và nhấn lưu, frontend cần gom các giá trị checkbox được chọn, nối lại bằng dấu `;` và gửi lên API.

---

## 3. Ý Tưởng Thiết Kế Giao Diện Người Dùng (UI/UX Design Intent)

Giao diện quản lý phân quyền tối ưu nhất là cấu trúc **Ma Trận Quyền (Permission Matrix Table)**. 

### 3.1. Cấu trúc Bảng Ma Trận Phân Quyền

Mỗi dòng đại diện cho một **Chức năng (Feature)**. Các cột đại diện cho các quyền **Xem (`CanAccess`)** và các **Hành động (`Permissions`)**.

| Chức Năng (Feature) | Xem / Truy Cập (`CanAccess`) | Thêm mới (`Create`) | Chỉnh sửa (`Update`) | Xóa (`Delete`) | Phê duyệt (`Approve`) | Xuất file (`Export`) |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **Dự án** | [x] *Switch/Toggle* | [x] *Checkbox* | [ ] *Checkbox* | [ ] *Checkbox* | - | [x] *Checkbox* |
| **Gói thầu** | [x] *Switch/Toggle* | [x] *Checkbox* | [x] *Checkbox* | [ ] *Checkbox* | - | [ ] *Checkbox* |
| **Hợp đồng** | [ ] *Switch/Toggle* | [ ] *(Disabled)* | [ ] *(Disabled)* | [ ] *(Disabled)* | [ ] *(Disabled)* | [ ] *(Disabled)* |

---

### 3.2. Quy tắc Tương Tác trên UI (Interactive UX Rules)

1.  **Mối quan hệ phụ thuộc**:
    *   Cột **Xem / Truy Cập (CanAccess)** sử dụng nút gạt **Switch (Toggle Button)** hoặc Checkbox lớn để bật/tắt toàn bộ dòng.
    *   Nếu **Xem / Truy Cập** được **TẮT** (`CanAccess = false`):
        *   Tất cả checkbox hành động còn lại trên dòng đó tự động **bỏ chọn (uncheck)** và **bị vô hiệu hóa (disabled)**.
    *   Nếu **Xem / Truy Cập** được **BẬT** (`CanAccess = true`):
        *   Mở khóa các checkbox hành động tương ứng để người dùng tích chọn.
2.  **Logic ẩn/hiện cột theo Chức năng**:
    *   Không phải chức năng nào cũng có đầy đủ các quyền. Ví dụ: Chức năng `Đối tác` có thể không có quyền `Phê duyệt` (`Approve`).
    *   Trên UI, những ô không thuộc phạm vi của chức năng đó nên được hiển thị dấu gạch ngang `-` hoặc ẩn hẳn checkbox đó đi để tránh gây bối rối cho người vận hành.

---

## 4. Hướng dẫn Code Mẫu Phía Frontend (React.js / Vue.js)

Dưới đây là gợi ý hàm xử lý dữ liệu để Frontend giao tiếp với Backend dễ dàng.

### 4.1. Phân tích dữ liệu từ Backend để hiển thị lên Checkbox (Parse API response)

```javascript
// Dữ liệu giả lập nhận về từ API cho một dòng Feature
const rawPermission = {
  featureId: "feat-123-uuid",
  featureName: "Quản lý Hợp đồng",
  canAccess: true,
  permissions: "Create;Update;Export" // Chuỗi ngăn cách bởi dấu chấm phẩy
};

// Chuyển đổi thành dạng Object thân thiện với Frontend UI
const parsePermission = (raw) => {
  const activeActions = raw.permissions ? raw.permissions.split(';') : [];
  
  return {
    featureId: raw.featureId,
    featureName: raw.featureName,
    canAccess: raw.canAccess,
    // Trả về dạng object boolean để dễ dàng bind vào thuộc tính checked của checkbox
    actions: {
      Create: activeActions.includes('Create'),
      Update: activeActions.includes('Update'),
      Delete: activeActions.includes('Delete'),
      Approve: activeActions.includes('Approve'),
      Export: activeActions.includes('Export'),
    }
  };
};

console.log(parsePermission(rawPermission));
/*
Kết quả đầu ra:
{
  featureId: "feat-123-uuid",
  featureName: "Quản lý Hợp đồng",
  canAccess: true,
  actions: {
    Create: true,
    Update: true,
    Delete: false,
    Approve: false,
    Export: true
  }
}
*/
```

### 4.2. Chuyển đổi trạng thái từ UI thành chuỗi để gửi lưu (Format to request body)

```javascript
// Trạng thái hiện tại lưu trong State của React/Vue sau khi Admin bấm tích chọn
const uiState = {
  featureId: "feat-123-uuid",
  canAccess: true,
  actions: {
    Create: true,
    Update: true,
    Delete: false,
    Approve: false,
    Export: true
  }
};

// Ghép lại thành chuỗi gửi lên Backend API
const formatToSave = (state) => {
  // Nếu canAccess = false thì xóa hết permissions
  if (!state.canAccess) {
    return {
      featureId: state.featureId,
      canAccess: false,
      permissions: ""
    };
  }

  // Lọc ra các hành động có giá trị true
  const selectedActions = Object.keys(state.actions).filter(
    (actionName) => state.actions[actionName] === true
  );

  return {
    featureId: state.featureId,
    canAccess: true,
    permissions: selectedActions.join(';') // Nối lại bằng dấu ';'
  };
};

console.log(formatToSave(uiState));
/*
Kết quả đầu ra gửi lên API:
{
  featureId: "feat-123-uuid",
  canAccess: true,
  permissions: "Create;Update;Export"
}
*/
```

---

## 5. Danh Sách Gợi Ý Các Chức Năng (Features) Cần Phân Quyền

Dựa trên cấu trúc cơ sở dữ liệu hiện tại, dưới đây là danh sách các module mà frontend cần dựng ma trận phân quyền:

1.  **Dự án (`PROJECT`)**
    *   *CanAccess*: Xem danh sách & chi tiết dự án.
    *   *Permissions*: `Create`, `Update`, `Delete`, `Export`.
2.  **Gói thầu (`BIDPACKAGE`)**
    *   *CanAccess*: Xem danh sách & chi tiết gói thầu.
    *   *Permissions*: `Create`, `Update`, `Delete`, `Export`.
3.  **Hợp đồng (`CONTRACT`)**
    *   *CanAccess*: Xem danh sách & chi tiết hợp đồng.
    *   *Permissions*: `Create`, `Update`, `Delete`, `Approve`, `Export`.
4.  **Đối tác (`PARTNER`)**
    *   *CanAccess*: Xem danh sách & thông tin đối tác.
    *   *Permissions*: `Create`, `Update`, `Delete`.
5.  **Nghị quyết / Quyết định (`RESOLUTION`)**
    *   *CanAccess*: Xem/tải file quyết định.
    *   *Permissions*: `Create`, `Update`, `Delete`.
