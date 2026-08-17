# Tài liệu Kỹ thuật API & DTO (CoopBank Contract Management)

Tài liệu đặc tả toàn bộ danh sách APIs và DTOs (Data Transfer Objects) thuộc dự án **Hệ thống Quản lý Hợp đồng, Gói thầu & Dự án - Ngân hàng Hợp tác xã Việt Nam (CoopBank)**.

---

## 1. Quy chuẩn chung (API Conventions)

### 1.1 Base URL & Tiền tố Đường dẫn (Route Prefixes)
- **Hệ thống, Xác thực & Quản trị**: `http://localhost:5000/api/HeThong/...` (ví dụ: `/api/HeThong/auth`, `/api/HeThong/admin`, `/api/HeThong/user`, `/api/HeThong/warnings`, `/api/HeThong/notification`)
- **Nghiệp vụ Chức năng Hệ thống**: `http://localhost:5000/api/NghiepVu/...` (ví dụ: `/api/NghiepVu/du-an`, `/api/NghiepVu/goi-thau`, `/api/NghiepVu/hop-dong`, `/api/NghiepVu/report`)
- **Quản lý Danh mục Hệ thống**: `http://localhost:5000/api/DanhMuc/...`
- **Máy chủ & Health**: `http://localhost:5000/api/health`
- **Swagger UI**: `http://localhost:5000/swagger`

### 1.2 Authentication Header
Tất cả các API (ngoại trừ `/api/HeThong/auth/login`, `/api/HeThong/auth/refresh` và `/api/health`) đều yêu cầu xác thực bằng JWT Bearer Token trong Request Header:
```http
Authorization: Bearer <your_jwt_access_token>
```

### 1.3 Cấu trúc Response chuẩn

#### Danh sách Phân trang (`PagedResult<T>`)
```json
{
  "items": [ ... ],
  "totalItems": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8,
  "nextCursor": "string"
}
```

#### Xử lý Lỗi chuẩn (`ApiErrorResponse`)
```json
{
  "message": "Thông điệp lỗi chi tiết",
  "errors": {
    "FieldName": ["Mô tả lỗi của trường data"]
  }
}
```

### 1.4 HTTP Status Codes
| Mã Lỗi | Ý nghĩa | Mô tả |
| :--- | :--- | :--- |
| **200 OK** | Thành công | Yêu cầu đã xử lý thành công, trả về data |
| **201 Created** | Tạo mới thành công | Tạo đối tượng mới thành công |
| **204 No Content** | Thành công (Không content) | Cập nhật hoặc Xóa thành công |
| **400 Bad Request** | Dữ liệu không hợp lệ | Vi phạm validation DTO hoặc tham số |
| **401 Unauthorized** | Chưa xác thực | Không truyền Token hoặc Token hết hạn |
| **403 Forbidden** | Không có quyền truy cập | Tài khoản không có tính năng/quyền tương ứng |
| **404 Not Found** | Không tìm thấy | Không tồn tại tài nguyên theo ID |
| **500 Internal Error** | Lỗi máy chủ | Lỗi nội bộ trong quá trình xử lý |

---

## 2. Danh sách Endpoints APIs (API Specification)

### 2.1 Quản trị Hệ thống, Xác thực & Tài khoản (`/api/HeThong/auth`, `/api/HeThong/admin`, `/api/HeThong/user`)
- **`POST /api/HeThong/auth/login`**: Đăng nhập tài khoản bằng Username & Password.
- **`POST /api/HeThong/auth/refresh`**: Cấp lại Access Token mới khi hết hạn.
- **`POST /api/HeThong/auth/enable-2fa`**: Kích hoạt xác thực 2 yếu tố (2FA).
- **`POST /api/HeThong/auth/verify-2fa`**: Xác thực mã OTP 2FA hoàn tất đăng nhập.
- **`POST /api/HeThong/auth/logout`**: Đăng xuất tài khoản & hủy Refresh Token.
- **`GET /api/HeThong/user` / `GET /api/HeThong/user/list`**: Lấy danh sách người dùng kèm vai trò.
- **`POST /api/HeThong/user/bulk-create`**: Tạo/Cập nhật hàng loạt người dùng từ DTO.
- **`POST /api/HeThong/user/import-excel`**: Import danh sách người dùng từ file Excel (`.xlsx`).
- **`GET /api/HeThong/user/import-template`**: Tải file Excel mẫu để import người dùng.
- **`PUT /api/HeThong/user/{id}`**: Cập nhật thông tin chi tiết người dùng.
- **`DELETE /api/HeThong/user/bulk-delete`**: Xóa hàng loạt người dùng theo danh sách ID.
- **`GET /api/HeThong/admin/roles`**: Danh sách tất cả vai trò (Roles).
- **`POST /api/HeThong/admin/roles`**: Tạo mới vai trò.
- **`PUT /api/HeThong/admin/roles/{roleId}`**: Cập nhật vai trò.
- **`GET /api/HeThong/admin/features`**: Danh sách các tính năng ứng dụng.
- **`POST /api/HeThong/admin/features`**: Tạo mới tính năng.
- **`PUT /api/HeThong/admin/features/{featureId}`**: Cập nhật tính năng.
- **`DELETE /api/HeThong/admin/features/{featureId}`**: Xóa tính năng.
- **`GET /api/HeThong/admin/roles/{roleId}/permissions`**: Danh sách quyền của vai trò.
- **`PUT /api/HeThong/admin/roles/{roleId}/permissions`**: Cập nhật quyền cho vai trò.
- **`GET /api/HeThong/admin/users/{userId}/roles`**: Lấy danh sách ID vai trò của người dùng.
- **`PUT /api/HeThong/admin/users/{userId}/roles`**: Gán vai trò cho người dùng.
- **`GET /api/HeThong/admin/audit-logs`**: Tra cứu nhật ký hệ thống (Audit Logs).

### 2.2 Hệ thống Cảnh báo & Thông báo (`/api/HeThong/warnings`, `/api/HeThong/notification`)
- **`GET /api/HeThong/warnings/contracts-expiring-soon`**: Cảnh báo hợp đồng sắp hết hạn trong 30 ngày.
- **`GET /api/HeThong/warnings/expired-contracts`**: Cảnh báo hợp đồng đã quá hạn.
- **`GET /api/HeThong/warnings/over-budget-contracts`**: Cảnh báo hợp đồng vượt ngân sách.
- **`GET /api/HeThong/warnings/licenses-expiring-soon`**: Cảnh báo License phần mềm sắp hết hạn.
- **`GET /api/HeThong/warnings/expired-licenses`**: Cảnh báo License phần mềm đã hết hạn.
- **`GET /api/HeThong/notification`**: Lấy danh sách thông báo cá nhân (Phân trang, lọc chưa đọc/đã đọc).
- **`PUT /api/HeThong/notification/{id}/read`**: Đánh dấu 1 thông báo là đã đọc.
- **`PUT /api/HeThong/notification/read-all`**: Đánh dấu tất cả thông báo cá nhân là đã đọc.

### 2.3 Nghiệp vụ - Yêu cầu & Phân quyền Người dùng (`/api/NghiepVu/permission-requests`, `/api/NghiepVu/user-permissions`)
- **`POST /api/NghiepVu/permission-requests`**: Gửi yêu cầu xin cấp quyền bổ sung.
- **`GET /api/NghiepVu/permission-requests/my-requests`**: Xem danh sách yêu cầu cấp quyền của tôi.
- **`GET /api/NghiepVu/permission-requests/admin`**: Danh sách tất cả yêu cầu cấp quyền cho admin.
- **`POST /api/NghiepVu/permission-requests/{id}/review`**: Phê duyệt/Từ chối yêu cầu cấp quyền.
- **`GET /api/NghiepVu/user-permissions`**: Tra cứu danh sách quyền của người dùng.
- **`GET /api/NghiepVu/user-permissions/catalog`**: Danh mục catalog các loại quyền hệ thống.
- **`POST /api/NghiepVu/user-permissions`**: Cấp trực tiếp quyền đặc thù cho người dùng.
- **`GET /api/NghiepVu/user-permissions/du-an/{duAnId}`**: Kiểm tra quyền cá nhân trên một Dự án cụ thể.
- **`DELETE /api/NghiepVu/user-permissions/{id}`**: Thu hồi quyền người dùng.

### 2.4 Nghiệp vụ - Quản lý Dự án (`/api/NghiepVu/du-an`)
- **`GET /api/NghiepVu/du-an`**: Lấy danh sách dự án (Có phân trang, tìm kiếm).
- **`GET /api/NghiepVu/du-an/{id}`**: Chi tiết thông tin dự án theo GUID.
- **`POST /api/NghiepVu/du-an`**: Tạo mới dự án.
- **`PUT /api/NghiepVu/du-an/{id}`**: Cập nhật thông tin dự án.
- **`DELETE /api/NghiepVu/du-an/{id}`**: Xóa dự án theo GUID.
- **`POST /api/NghiepVu/du-an/{id}/dieu-chinh`**: Điều chỉnh ngân sách/tổng mức đầu tư dự án.
- **`GET /api/NghiepVu/du-an/{id}/dieu-chinh`**: Lịch sử các đợt điều chỉnh kinh phí.
- **`POST /api/NghiepVu/du-an/{id}/advance-status`**: Chuyển dự án sang giai đoạn tiếp theo.
- **`POST /api/NghiepVu/du-an/{id}/close`**: Quyết toán và đóng dự án.
- **`GET /api/NghiepVu/du-an/{id}/goi-thau`**: Danh sách gói thầu thuộc dự án.
- **`GET /api/NghiepVu/du-an/{id}/hop-dong`**: Danh sách hợp đồng thuộc dự án.
- **`GET /api/NghiepVu/du-an/{id}/audit-log`**: Lịch sử chỉnh sửa audit log của dự án.

### 2.5 Nghiệp vụ - Quản lý Gói thầu & Công việc (`/api/NghiepVu/goi-thau`, `/api/NghiepVu/cong-viec-goi-thau`, `/api/NghiepVu/comment-cong-viec`)
- **`GET /api/NghiepVu/goi-thau`**: Danh sách gói thầu (bộ lọc hình thức, trạng thái).
- **`GET /api/NghiepVu/goi-thau/{id}`**: Chi tiết gói thầu.
- **`POST /api/NghiepVu/goi-thau`**: Tạo mới gói thầu.
- **`PUT /api/NghiepVu/goi-thau/{id}`**: Cập nhật thông tin gói thầu.
- **`DELETE /api/NghiepVu/goi-thau/{id}`**: Xóa gói thầu.
- **`GET /api/NghiepVu/cong-viec-goi-thau/by-goi-thau/{idGoiThau}`**: Danh sách công việc thuộc gói thầu.
- **`GET /api/NghiepVu/cong-viec-goi-thau/by-goi-thau/{idGoiThau}/paged`**: Danh sách công việc thuộc gói thầu (phân trang).
- **`DELETE /api/NghiepVu/cong-viec-goi-thau/by-goi-thau/{idGoiThau}`**: Xóa tất cả công việc của gói thầu.
- **`POST /api/NghiepVu/cong-viec-goi-thau/{id}/xac-nhan`**: Xác nhận hoàn thành công việc.
- **`POST /api/NghiepVu/cong-viec-goi-thau/{id}/forward`**: Chuyển tiếp công việc cho người liên quan.
- **`GET /api/NghiepVu/cong-viec-goi-thau/{id}/forward-history`**: Lịch sử chuyển tiếp công việc.
- **`GET /api/NghiepVu/comment-cong-viec/by-cong-viec/{idCongViec}`**: Lấy bình luận của bước công việc.
- **`POST /api/NghiepVu/comment-cong-viec`**: Tạo mới bình luận.
- **`PUT /api/NghiepVu/comment-cong-viec/{id}`**: Cập nhật nội dung bình luận.
- **`DELETE /api/NghiepVu/comment-cong-viec/{id}`**: Xóa bình luận.
- **`GET /api/NghiepVu/comment-cong-viec/mention-suggestions`**: Gợi ý danh sách người dùng để Tag/Mention.

### 2.6 Nghiệp vụ - Quản lý Hợp đồng & Đợt thanh toán (`/api/NghiepVu/hop-dong`)
- **`GET /api/NghiepVu/hop-dong`**: Danh sách hợp đồng (lọc loại hợp đồng, trạng thái, giá trị, ngày hiệu lực).
- **`GET /api/NghiepVu/hop-dong/{id}`**: Chi tiết hợp đồng và danh sách đợt thanh toán.
- **`POST /api/NghiepVu/hop-dong`**: Tạo mới hợp đồng.
- **`PUT /api/NghiepVu/hop-dong/{id}`**: Cập nhật thông tin hợp đồng.
- **`DELETE /api/NghiepVu/hop-dong/{id}`**: Xóa hợp đồng.
- **`PUT /api/NghiepVu/hop-dong/dot-thanh-toan/{dotThanhToanId}/pay`**: Xác nhận thanh toán cho Đợt thanh toán.

### 2.7 Nghiệp vụ - Quản lý Hàng hóa, Dịch vụ & License (`/api/NghiepVu/hang-hoa`, `/api/NghiepVu/dich-vu`, `/api/NghiepVu/licenses`, `/api/NghiepVu/resolutions`)
- **`GET /api/NghiepVu/hang-hoa/by-parent/{idParent}`**: Lấy danh sách hàng hóa thuộc Hợp đồng/Gói thầu.
- **`POST /api/NghiepVu/hang-hoa/batch`**: Thêm mới danh sách hàng hóa hàng loạt.
- **`GET /api/NghiepVu/dich-vu/by-parent/{idParent}`**: Lấy danh sách dịch vụ thuộc Hợp đồng/Gói thầu.
- **`POST /api/NghiepVu/dich-vu/batch`**: Thêm mới danh sách dịch vụ hàng loạt.
- **`GET /api/NghiepVu/licenses`**: Danh sách License phần mềm.
- **`POST /api/NghiepVu/licenses/single`**: Tạo mới License phần mềm.
- **`GET /api/NghiepVu/licenses/by-duan/{duAnId}`**: Danh sách License theo Dự án.
- **`GET /api/NghiepVu/licenses/expiring`**: Danh sách License sắp hết hạn.
- **`GET /api/NghiepVu/licenses/summary`**: Thống kê tổng quan số lượng License.
- **`GET /api/NghiepVu/licenses/enums`**: Danh sách Enum loại và trạng thái License.
- **`GET /api/NghiepVu/resolutions`**: Quản lý Nghị quyết / Quyết định phê duyệt.

### 2.8 Nghiệp vụ - Báo cáo & Xuất dữ liệu (`/api/NghiepVu/report`)
- **`GET /api/NghiepVu/report/investment`**: Báo cáo tổng hợp tình hình đầu tư (năm, kỳ).
- **`GET /api/NghiepVu/report/investment/export`**: Xuất báo cáo đầu tư (xlsx, csv, html, base64).
- **`GET /api/NghiepVu/report/cong-viec-goi-thau/{idGoiThau}`**: Báo cáo trình tự thực hiện gói thầu.
- **`GET /api/NghiepVu/report/cong-viec-goi-thau/{idGoiThau}/export`**: Xuất báo cáo tiến độ gói thầu ra Excel.
- **`GET /api/NghiepVu/report/contract-payments`**: Báo cáo theo dõi giải ngân thanh toán hợp đồng.
- **`GET /api/NghiepVu/report/contract-payments/export`**: Xuất báo cáo thanh toán hợp đồng ra Excel/CSV.
- **`GET /api/NghiepVu/report/theo-doi-hop-dong`** (Alias: `/api/NghiepVu/reportTheoDoiHopDong`): Báo cáo theo dõi hợp đồng và đợt thanh toán (mẫu `Theo_dõi_HĐ.xlsx`).
- **`GET /api/NghiepVu/report/theo-doi-hop-dong/export`** (Alias: `/api/NghiepVu/reportTheoDoiHopDong/export`): Xuất báo cáo theo dõi hợp đồng chuẩn Excel `Theo_dõi_HĐ.xlsx`.


### 2.9 Danh mục Hệ thống (`/api/DanhMuc/...`)
- **`GET /api/DanhMuc/chuc-vu`**: Danh mục Chức vụ.
- **`GET /api/DanhMuc/don-vi`**: Danh mục Đơn vị.
- **`GET /api/DanhMuc/phong-ban`**: Danh mục Phòng ban.
- **`GET /api/DanhMuc/don-vi-tinh`**: Danh mục Đơn vị tính.
- **`GET /api/DanhMuc/hang-san-xuat`**: Danh mục Hãng sản xuất.
- **`GET /api/DanhMuc/nhom-du-an`**: Danh mục Nhóm dự án.
- **`GET /api/DanhMuc/phan-loai-du-an`**: Danh mục Phân loại dự án.
- **`GET /api/DanhMuc/xuat-xu`**: Danh mục Xuất xứ hàng hóa.
- **`GET /api/DanhMuc/doi-tac`**: Danh mục Đối tác / Nhà thầu.

### 2.10 Quản lý Tải lên & Tải xuống Tệp tin (`/api/HeThong/files`)
- **`POST /api/HeThong/files/upload`**: Tải lên tệp đính kèm (PDF, DOCX, XLSX, v.v.), lưu trữ ổ đĩa theo cấu trúc động `id/FeatureCode/id chức năng/filename` và lưu thông tin vào bảng CSDL `FileAttachments`.
- **`GET /api/HeThong/files/download/{id}`**: Tải xuống/Xem file đính kèm bằng mã định danh GUID (khuyên dùng vì độ bảo mật cao).
- **`GET /api/HeThong/files/download`**: Tải xuống/Xem file đính kèm bằng đường dẫn tương đối (Query `relativePath`), tự động phòng chống tấn công Directory Traversal.
- **`GET /api/HeThong/files/by-entity`**: Lấy danh sách tài liệu đính kèm của một bản ghi chức năng cụ thể (ví dụ: danh sách file của Hợp đồng X) để biết bản ghi đó có file hay không và hiển thị nút tải về.
- **`DELETE /api/HeThong/files/delete-multiple`**: Xóa hàng loạt tệp đính kèm theo danh sách ID (Guid) truyền dưới body dạng JSON Array. Thực hiện xóa cứng (xóa hoàn toàn bản ghi khỏi các bảng FileAttachments và FileVersions trong CSDL) và xóa tệp tin vật lý tương ứng trên đĩa.

---

## 3. Hướng dẫn Xem & Thử nghiệm trên Swagger UI

1. Khởi chạy Backend server .NET Core Web API (`dotnet run`).
2. Mở trình duyệt truy cập: **`http://localhost:5000/swagger`**
3. Giao diện Swagger sẽ tự động sắp xếp các API theo từng cụm tiền tố `/api/HeThong/`, `/api/NghiepVu/` và `/api/DanhMuc/` rõ ràng, ngăn nắp.
4. Chọn nút **Authorize** màu xanh ở góc trên bên phải, nhập Token: `Bearer <access_token>` và nhấn **Authorize**.
5. Bạn có thể chọn và thử nghiệm gọi trực tiếp bất kỳ API nào từ giao diện Swagger!
