# Tài Liệu Cập Nhật API (Tính Năng Mới)

Tài liệu này mô tả các thay đổi trong các đầu ra/đầu vào API liên quan tới 2 tính năng vừa được bổ sung:
1. **Thiết lập Chủ Dự Án (Project Owner)**.
2. **Thiết lập Quyền xem toàn bộ hợp đồng (CanViewHopDong)**.

---

## 1. Tính năng Chủ Dự Án (Project)

### 1.1. API Tạo Mới Dự Án
- **Endpoint**: `POST /api/DuAn` (và `POST /api/DuAn/bulk-create`)
- **Mô tả**: Bổ sung thêm trường `chuDuAnId` để admin có thể chỉ định người làm chủ dự án. Nếu admin không truyền hoặc người dùng bình thường tự tạo, hệ thống tự động lấy ID của người đang thao tác làm chủ dự án.
- **Payload Request Mới (Thêm thuộc tính)**:
```json
{
  "code": "string",
  "name": "string",
  ...
  "chuDuAnId": "3fa85f64-5717-4562-b3fc-2c963f66afa6" // Mới (Guid - Có thể null)
}
```

### 1.2. API Cập Nhật Dự Án
- **Endpoint**: `PUT /api/DuAn/{id}`
- **Mô tả**: Cho phép cập nhật/chuyển giao Chủ dự án sang người khác (Chỉ Admin mới có quyền đổi).
- **Payload Request Mới (Thêm thuộc tính)**:
```json
{
  "name": "string",
  ...
  "chuDuAnId": "3fa85f64-5717-4562-b3fc-2c963f66afa6" // Mới (Guid - Có thể null)
}
```

### 1.3. API Lấy Danh Sách & Chi Tiết Dự Án
- **Endpoint**: `GET /api/DuAn` và `GET /api/DuAn/{id}`
- **Mô tả**: Kết quả trả về giờ đây chứa thêm ID và Tên hiển thị của chủ dự án để thuận tiện cho Frontend hiển thị. Ngoài ra, danh sách trả về cho một User thường sẽ tự động bao gồm các dự án mà họ được gán làm `ChuDuAn`.
- **Response Model (Bổ sung thuộc tính)**:
```json
{
  "id": "...",
  "name": "...",
  ...
  "chuDuAnId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", // Mới (Guid)
  "chuDuAnName": "Nguyễn Văn A" // Mới (String)
}
```

---

## 2. Tính năng Cấu Hình Quyền "Xem Tất Cả Hợp Đồng" (User)

### 2.1. API Lấy Danh Sách User
- **Endpoint**: `GET /api/HeThong/user`
- **Mô tả**: Trả về thêm cấu hình `canViewHopDong` của mỗi user.
- **Response Model (Bổ sung thuộc tính)**:
```json
{
  "items": [
    {
      "id": "...",
      "username": "...",
      "fullName": "...",
      ...
      "canViewHopDong": true // Mới (Boolean)
    }
  ],
  "totalItems": 10
}
```

### 2.2. API Tạo / Import User
- **Endpoint**: `POST /api/HeThong/user/bulk-create`
- **Mô tả**: Cho phép cấu hình quyền xem tất cả hợp đồng ngay lúc tạo tài khoản mới.
- **Payload Request Mới (Thêm thuộc tính)**:
```json
[
  {
    "username": "user_a",
    "fullName": "Người dùng A",
    ...
    "canViewHopDong": true // Mới (Boolean)
  }
]
```

### 2.3. API Cập Nhật User
- **Endpoint**: `PUT /api/HeThong/user/{id}`
- **Mô tả**: Cho phép admin bật/tắt quyền xem toàn bộ hợp đồng của một user hiện có.
- **Payload Request Mới (Thêm thuộc tính)**:
```json
{
  "fullName": "Người dùng A cập nhật",
  ...
  "canViewHopDong": true // Mới (Boolean)
}
```

### 2.4 API Lấy Danh Sách Hợp Đồng (Ảnh Hưởng Phân Quyền)
- **Endpoint**: `GET /api/HopDong`
- **Mô tả**: Hành vi thay đổi. Nếu User đang đăng nhập có cờ `canViewHopDong` là `true`, dữ liệu trả về sẽ là **toàn bộ hợp đồng trên hệ thống**, bỏ qua các lớp filter giới hạn thuộc tính dự án hay quyền phân công trước đó. Quyền này cũng áp dụng cho `GET /api/HopDong/{id}` (xem chi tiết hợp đồng).

---

## 3. Tính năng Báo cáo Theo dõi Tiến độ Thanh toán các Dự án Thầu (Mẫu Excel)

### 3.1. Bổ sung Trường dữ liệu CSDL & DTOs
- **Gói thầu (`GoiThau` & `GoiThauDto`)**: Bổ sung `soQuyetDinhKQLCNT` (Số QĐ phê duyệt kết quả lựa chọn nhà thầu) và `ngayPheDuyetKQLCNT` (Ngày phê duyệt KQLCNT).
- **Hợp đồng (`HopDong` & `HopDongDto`)**: Bổ sung `ngayKy` (Ngày ký hợp đồng).
- **Dự án (`DuAn` & `DuAnDto`)**: Bổ sung `soQuyetDinhPheDuyetDuToan` (Số QĐ phê duyệt dự toán).

### 3.2. API Lấy Dữ liệu Báo cáo Tiến độ Thanh toán các Dự án Thầu
- **Endpoint**: `GET /api/NghiepVu/reports/tien-do-thanh-toan-du-an-thau` (Alias: `/api/NghiepVu/report/tien-do-thanh-toan-du-an-thau`)
- **Query Parameters**:
  - `year` (int?): Năm ký/hiệu lực hợp đồng
  - `duAnId` (Guid?): Mã Dự án cần lọc
  - `search` (string?): Từ khóa tìm kiếm (Dự án, Gói thầu, Nhà thầu, Hợp đồng)
  - `donViTinh` (string?): Đơn vị tính (Đồng, Triệu đồng...)
- **Response Model**: `TienDoThanhToanDuAnThauReportResponseDto` (trả về 18 cột thông tin chuẩn khớp file mẫu Excel `Mẫu_Báo_cáo_theo_dõi_tiến_độ_thanh_toán_các_dự_án_thầu.xlsx`).

### 3.3. API Xuất File Báo cáo Tiến độ Thanh toán các Dự án Thầu
- **Endpoint**: `GET /api/NghiepVu/reports/tien-do-thanh-toan-du-an-thau/export` (Alias: `/api/NghiepVu/report/tien-do-thanh-toan-du-an-thau/export`)
- **Query Parameters**:
  - `year` (int?), `duAnId` (Guid?), `search` (string?), `donViTinh` (string?)
  - `format` (string): Định dạng file xuất (`xlsx`, `csv`, `html` - mặc định: `xlsx`).
  - `base64` (bool): `true` để trả về JSON Base64 string, `false` để tải trực tiếp file binary.

