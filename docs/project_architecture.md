# Kiến Trúc Hệ Thống & Cơ Sở Dữ Liệu (Project Architecture & Database Schema)

Tài liệu này mô tả chi tiết về cấu trúc các bảng cơ sở dữ liệu, mối quan hệ giữa chúng, và nguyên lý hoạt động của các luồng nghiệp vụ chính trong dự án **Quản Lý Dự Án và Hợp Đồng CNTT**.

---

## 1. Danh Sách Các Bảng & Thực Thể (Database Schema)

Các thực thể nghiệp vụ cốt lõi (Project, BidPackage, Contract, Partner, Resolution) đều kế thừa từ lớp trừu tượng `BaseEntity` để đồng nhất các thuộc tính cơ bản:
*   `Id` (Guid): Khóa chính tự sinh.
*   `Code` (String): Mã định danh nghiệp vụ (ví dụ: Mã dự án, Mã hợp đồng).
*   `Name` (String): Tên hiển thị.
*   `Description` (String): Mô tả chi tiết.
*   `IsActive` (Boolean): Trạng thái hoạt động.
*   `CreatedAt` (DateTime): Thời gian tạo.
*   `UpdatedAt` (DateTime): Thời gian cập nhật gần nhất.

### 1.1. Nhóm Quản Lý Nghiệp Vụ Chính

#### Dự Án (`Projects`)
Lưu trữ thông tin các dự án CNTT của tổ chức.
*   `TotalBudget` (Decimal): Tổng ngân sách dự toán của dự án.
*   `Status` (String): Trạng thái dự án (ví dụ: `Planning`, `In Progress`, `Completed`).

#### Gói Thầu (`BidPackages`)
Lưu trữ thông tin các gói thầu thuộc một dự án.
*   `ProjectId` (Guid?): Khóa ngoại liên kết tới bảng `Projects`.
*   `EstimatedValue` (Decimal): Giá trị dự toán gói thầu.
*   `WarningThresholdPercent` (Decimal): Ngưỡng cảnh báo chi tiêu (mặc định 100%).

#### Hợp Đồng (`Contracts`)
Lưu trữ các hợp đồng được ký kết cho gói thầu/dự án.
*   `ProjectId` (Guid?): Khóa ngoại liên kết tới bảng `Projects`.
*   `BidPackageId` (Guid?): Khóa ngoại liên kết tới bảng `BidPackages`.
*   `ContractValue` (Decimal): Giá trị hợp đồng.
*   `SignedDate` (DateTime?): Ngày ký kết.
*   `EffectiveDate` (DateTime?): Ngày hiệu lực.
*   `ExpiredDate` (DateTime?): Ngày hết hạn.
*   `RenewalReminderDate` (DateTime?): Ngày cảnh báo gia hạn hợp đồng.
*   `IsRenewalRequired` (Boolean): Đánh dấu hợp đồng có cần gia hạn hay không.
*   `Status` (String): Trạng thái hợp đồng (ví dụ: `Draft`, `Active`, `Expired`).

#### Đối Tác (`Partners`)
Lưu trữ thông tin nhà thầu, nhà cung cấp, đối tác liên kết.
*   `TaxCode` (String): Mã số thuế.
*   `Phone` (String): Số điện thoại liên hệ.
*   `Email` (String): Email liên hệ.
*   `Address` (String): Địa chỉ.

#### Nghị Quyết / Văn Bản (`Resolutions`)
Lưu trữ các nghị quyết, quyết định phê duyệt dự án/gói thầu.
*   `IssuedDate` (DateTime?): Ngày ban hành.
*   `EffectiveDate` (DateTime?): Ngày hiệu lực.
*   `FileUrl` (String): Đường dẫn lưu file văn bản đính kèm.

---

### 1.2. Nhóm Phân Quyền & Người Dùng (RBAC)

#### Người Dùng (`Users`)
Lưu trữ tài khoản người dùng truy cập hệ thống.
*   `Username` (String): Tên đăng nhập.
*   `PasswordHash` (String): Mật khẩu đã mã hóa.
*   `FullName` (String): Họ và tên đầy đủ.
*   `Email` / `Phone` (String): Thông tin liên hệ.
*   `IsActive` (Boolean): Trạng thái hoạt động của tài khoản.
*   `IsSystemAdmin` (Boolean): Quyền Quản trị viên tối cao (Bỏ qua mọi bước kiểm tra phân quyền RBAC).
*   `IsTwoFactorEnabled` / `TwoFactorSecret` (Boolean/String): Cấu hình xác thực 2 lớp (MFA/TOTP).
*   `RefreshTokenHash` / `RefreshTokenExpiryTime` (String/DateTime): Thông tin Refresh Token hỗ trợ duy trì đăng nhập JWT.

#### Vai Trò (`Roles`)
Lưu trữ các nhóm vai trò trong hệ thống (ví dụ: `Admin`, `Manager`, `Staff`).
*   `Name` (String): Tên vai trò.
*   `Description` (String): Mô tả nhiệm vụ vai trò.
*   `IsActive` (Boolean): Trạng thái hoạt động.

#### Chức Năng (`Features`)
Danh mục các module chức năng cần phân quyền (ví dụ: `PROJECT`, `CONTRACT`, `PARTNER`).
*   `Code` (String): Mã chức năng dùng để kiểm tra trong code (ví dụ: `"PROJECT"`).
*   `Name` (String): Tên chức năng.

---

### 1.3. Các Bảng Trung Gian (Join Tables)

#### Phân Vai Trò Người Dùng (`UserRoles`)
Mối quan hệ Nhiều-Nhiều giữa `Users` và `Roles`.
*   `UserId` (Guid) & `RoleId` (Guid): Tạo thành khóa chính liên hợp.

#### Chi Tiết Phân Quyền Vai Trò (`RolePermissions`)
Mối quan hệ Nhiều-Nhiều giữa `Roles` và `Features`, đi kèm phân quyền chi tiết.
*   `RoleId` (Guid) & `FeatureId` (Guid): Khóa chính liên hợp.
*   `CanAccess` (Boolean): Quyền xem/truy cập chức năng.
*   `Permissions` (String): Danh sách các quyền hành động chi tiết, ngăn cách bởi dấu chấm phẩy `;` (ví dụ: `"Create;Update;Delete"`). Đây là thiết kế động giúp dễ dàng mở rộng thêm các quyền mới (như `"Approve"`, `"Export"`,...).

#### Đối Tác Hợp Đồng (`ContractPartners`)
Mối quan hệ Nhiều-Nhiều giữa `Contracts` và `Partners`.
*   `ContractId` (Guid) & `PartnerId` (Guid): Khóa chính liên hợp.
*   `Role` (String): Vai trò của đối tác trong hợp đồng (ví dụ: `Primary` - Nhà thầu chính, `Sub` - Nhà thầu phụ).

---

## 2. Sơ Đồ Mối Quan Hệ Giữa Các Bảng (Entity Relationships)

```mermaid
erDiagram
    Projects ||--o{ BidPackages : "chứa"
    Projects ||--o{ Contracts : "thuộc về"
    BidPackages ||--o{ Contracts : "có"
    
    Contracts ||--o{ ContractPartners : "liên kết"
    Partners ||--o{ ContractPartners : "liên kết"
    
    Users ||--o{ UserRoles : "gán"
    Roles ||--o{ UserRoles : "gán"
    
    Roles ||--o{ RolePermissions : "có"
    Features ||--o{ RolePermissions : "có"
    
    BaseEntity <|-- Projects : "kế thừa"
    BaseEntity <|-- BidPackages : "kế thừa"
    BaseEntity <|-- Contracts : "kế thừa"
    BaseEntity <|-- Partners : "kế thừa"
    BaseEntity <|-- Resolutions : "kế thừa"
```

---

## 3. Phương Hướng & Nguyên Lý Hoạt Động (System Workflows)

### 3.1. Luồng Xác Thực & Bảo Mật (Authentication & Security)
1.  **Đăng nhập 2 lớp (MFA)**:
    *   Người dùng đăng nhập bằng `Username` & `Password`.
    *   Nếu tài khoản đã kích hoạt MFA (`IsTwoFactorEnabled = true`), hệ thống yêu cầu cung cấp mã OTP 6 số được sinh từ ứng dụng Authenticator (dựa trên thuật toán TOTP lưu ở `TwoFactorSecret`).
    *   Xác thực thành công sẽ trả về **Access Token** (JWT ngắn hạn) và **Refresh Token** (lưu ở database để cấp lại Access Token mới khi hết hạn).
2.  **Đăng nhập Radius/Active Directory**:
    *   Hệ thống hỗ trợ tích hợp xác thực tài khoản tập trung thông qua dịch vụ Radius (`RadiusClient`).

### 3.2. Luồng Kiểm Tra Phân Quyền (Authorization Flow)
Phân quyền hệ thống hoạt động thông qua một Bộ lọc tùy biến `[FeatureAuthorize("FEATURE_CODE")]` đặt trên các API:
1.  Khi Client gửi request lên API (ví dụ: `POST /api/projects`), bộ lọc `FeatureAuthorizeFilter` sẽ bắt lấy thông tin người dùng từ JWT.
2.  Nếu người dùng có `IsSystemAdmin = true`, hệ thống bỏ qua kiểm tra và cho phép thực thi ngay lập tức.
3.  Nếu không phải Admin hệ thống, bộ lọc sẽ tìm tất cả các `Role` đang hoạt động được gán cho người dùng này thông qua bảng `UserRoles`.
4.  Truy vấn các bản ghi `RolePermissions` tương ứng với các vai trò đó cho chức năng được yêu cầu (ví dụ: `FeatureCode = "PROJECT"`).
5.  Kiểm tra quyền tương ứng dựa vào phương thức HTTP:
    *   **GET**: Yêu cầu `CanAccess = true`.
    *   **POST**: Yêu cầu trường `Permissions` chứa từ khóa `"Create"`.
    *   **PUT / PATCH**: Yêu cầu trường `Permissions` chứa từ khóa `"Update"`.
    *   **DELETE**: Yêu cầu trường `Permissions` chứa từ khóa `"Delete"`.
6.  Nếu thỏa mãn bất kỳ vai trò nào có quyền, yêu cầu được tiếp tục xử lý. Ngược lại, hệ thống trả về mã lỗi `403 Forbidden`.

### 3.3. Luồng Quản Lý Vòng Đời Dự Án & Hợp Đồng (Lifecycle Management)
1.  **Lập kế hoạch**: Khởi tạo `Project` với tổng ngân sách dự kiến.
2.  **Chuẩn bị thầu**: Chia nhỏ dự án thành các gói thầu (`BidPackages`), đảm bảo tổng giá trị dự toán (`EstimatedValue`) của các gói thầu không vượt quá tổng ngân sách dự án.
3.  **Ký kết hợp đồng**: Ký hợp đồng (`Contracts`) liên kết với gói thầu và các đối tác (`Partners`) thực hiện. Giá trị hợp đồng cộng dồn không được vượt quá giá trị dự toán của gói thầu (nếu vượt quá ngưỡng `WarningThresholdPercent` sẽ kích hoạt cảnh báo chi tiêu vượt hạn mức).
4.  **Theo dõi hiệu lực & Cảnh báo hết hạn**:
    *   Hệ thống tự động tính toán ngày hết hiệu lực của hợp đồng (`ExpiredDate`).
    *   Trước ngày hết hạn một khoảng thời gian (dựa trên cấu hình `RenewalReminderDate`), hệ thống sẽ phát tín hiệu cảnh báo hoặc gửi thông báo nhắc nhở gia hạn hợp đồng đối với các bản ghi có `IsRenewalRequired = true`.
