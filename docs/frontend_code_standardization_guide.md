# Hướng dẫn Cập nhật Frontend: Quy chuẩn Mã Hệ thống Mới & Tích hợp API

Tài liệu này tổng hợp chi tiết quy chuẩn mã hóa mới và hướng dẫn đội ngũ Frontend (FE) cập nhật giao diện, form nhập liệu và các tích hợp API Backend tương ứng.

---

## 1. Tổng quan Quy chuẩn Mã Mới

Tất cả các mã trong hệ thống Quản lý Hợp đồng & Dự án CNTT (CoopBank) đã được chuẩn hóa theo quy tắc dưới đây. Backend tự động sinh mã mặc định, đồng thời người dùng vẫn được phép **chỉnh sửa thủ công** nếu có trường hợp ngoại lệ (input không bị khóa read-only).

| STT | Đối tượng | Cấu trúc chuẩn | Ví dụ mẫu | Quy tắc nghiệp vụ |
| --- | --- | --- | --- | --- |
| 1 | **Dự án Nguồn** | `{STT_3_CHỮ_SỐ}/{NĂM}/DAN` | `001/2026/DAN` | STT tự động tăng và reset về `001` vào đầu mỗi năm mới |
| 2 | **Dự án Triển khai** | `{STT_3_CHỮ_SỐ}/{NĂM}/DATK` | `002/2026/DATK` | STT tự động tăng và reset về `001` vào đầu mỗi năm mới |
| 3 | **Gói thầu** | `{STT_3_CHỮ_SỐ}/{NĂM}/GT` | `002/2026/GT` | STT tự động tăng và reset về `001` vào đầu mỗi năm mới |
| 4 | **Số/Mã Hợp đồng** | `{STT_3_CHỮ_SỐ}/{NĂM}/HĐ` | `017/2026/HĐ` | STT tự động tăng và reset về `001` vào đầu mỗi năm mới |
| 5 | **Mã Đợt thanh toán** | `{ĐỢT_2_CHỮ_SỐ}/{NĂM}/TT-HD{SỐ_HĐ_3_CHỮ_SỐ}` | `01/2026/TT-HD017` | Số đợt thanh toán tăng dần theo từng Hợp đồng |
| 6 | **Nghị quyết / Quyết định** | Nhập tự do text | `QĐ-123/2026/QĐ-NH` | Cho phép nhập tự do văn bản pháp lý |
| 7 | **Mã Đối tác / Nhà thầu** | `NT-{TÊN_NGẮN_HOA}` | `NT-CMC` | Tiền tố `NT-` + Tên ngắn gọn hoa không dấu |
| 8 | **Mã Nguồn vốn** | `NV-{TÊN_VIẾT_TẮT_HOA}` | `NV-NSNN` | Tiền tố `NV-` + Tên viết tắt hoa không dấu |
| 9 | **Mã Loại Dự án** | `PL-{TÊN_VIẾT_TẮT_HOA}` | `PL-HTPH` | Tiền tố `PL-` + Tên viết tắt hoa không dấu |
| 10 | **Mã Loại Hợp đồng** | `PLHD-{TÊN_VIẾT_TẮT_HOA}` | `PLHD-BT` | Tiền tố `PLHD-` + Tên viết tắt hoa không dấu |

---

## 2. Chi tiết API Backend Sinh Mã Tự động

Backend cung cấp Controller `SystemCodeController` tại đường dẫn `/api/SystemCode` với các endpoint sau để FE gọi nạp gợi ý mã khi mở Form tạo mới:

### 2.1. API Sinh mã Dự án (Nguồn / Triển khai)
* **Endpoint**: `GET /api/SystemCode/generate/du-an`
* **Query Params**:
  * `loaiDuAn` (number): `1` (Dự án Nguồn) hoặc `2` (Dự án Triển khai) - Mặc định `1`.
  * `nam` (number, optional): Năm (Mặc định lấy năm hiện tại, e.g. `2026`).
* **Response**:
  ```json
  {
    "code": "001/2026/DAN"
  }
  ```
* **Hướng dẫn FE**: Khi người dùng mở Modal/Page **Tạo Dự án**, hoặc thay đổi dropdown chọn `LoaiDuAn` (1 hoặc 2), FE gọi API này và điền giá trị `code` trả về vào ô input Mã dự án.

### 2.2. API Sinh mã Gói thầu
* **Endpoint**: `GET /api/SystemCode/generate/goi-thau`
* **Query Params**: `nam` (number, optional)
* **Response**:
  ```json
  {
    "code": "002/2026/GT"
  }
  ```
* **Hướng dẫn FE**: Tự động điền giá trị gợi ý khi mở form tạo mới Gói thầu.

### 2.3. API Sinh mã Hợp đồng
* **Endpoint**: `GET /api/SystemCode/generate/hop-dong`
* **Query Params**: `nam` (number, optional)
* **Response**:
  ```json
  {
    "code": "017/2026/HĐ"
  }
  ```
* **Hướng dẫn FE**: Tự động điền giá trị gợi ý khi mở form tạo mới Hợp đồng.

### 2.4. API Sinh mã Đợt thanh toán
* **Endpoint**: `GET /api/SystemCode/generate/dot-thanh-toan`
* **Query Params**:
  * `hopDongId` (string, required): Guid ID của hợp đồng tương ứng.
  * `nam` (number, optional)
* **Response**:
  ```json
  {
    "code": "01/2026/TT-HD017"
  }
  ```
* **Hướng dẫn FE**: Gọi API khi người dùng bấm "Thêm đợt thanh toán" mới trong danh sách đợt thanh toán của Hợp đồng.

### 2.5. API Sinh mã Danh mục (Đối tác, Nguồn vốn, Loại dự án, Loại hợp đồng)
* `GET /api/SystemCode/generate/doi-tac?name=CMC` -> `{ "code": "NT-CMC" }`
* `GET /api/SystemCode/generate/nguon-von?name=NSNN` -> `{ "code": "NV-NSNN" }`
* `GET /api/SystemCode/generate/phan-loai-du-an?name=HTPH` -> `{ "code": "PL-HTPH" }`
* `GET /api/SystemCode/generate/loai-hop-dong?name=BT` -> `{ "code": "PLHD-BT" }`

### 2.6. API Chuyển đổi Mã Lịch sử (Dành cho Admin)
* **Endpoint**: `POST /api/SystemCode/migrate-legacy-codes`
* **Response**:
  ```json
  {
    "message": "Migrated legacy codes successfully",
    "migratedRecords": 45
  }
  ```

---

## 3. Các thay đổi DTO Đợt thanh toán (`DotThanhToan`)

Trong các DTO liên quan đến đợt thanh toán (`DotThanhToanDto`, `CreateDotThanhToanDto`, `DotThanhToanReportDto`), Backend đã bổ sung thêm thuộc tính **`code`** (string):

```typescript
export interface DotThanhToanDto {
  id: string;
  hopDongId: string;
  tenDot: string;
  code: string; // <--- Thuộc tính mới
  tyLeThanhToan: number;
  giaTriThanhToan: number;
  ngayThanhToan?: string;
  ngayThanhToanThucTe?: string;
  dieuKienThanhToan?: string;
  ghiChuThanhToan?: string;
  isPaid: boolean;
  createdAt: string;
  updatedAt?: string;
}
```

---

## 4. Hướng dẫn Cập nhật Giao diện UI/UX ở Frontend

1. **Form Input Code (Mã đối tượng)**:
   - Giữ nguyên ô input `Code` dạng cho phép chỉnh sửa (không khóa read-only / disabled).
   - Thêm nút icon / button "Tạo mã tự động" kế bên ô Input để người dùng bấm lại khi cần reset lấy mã chuẩn.
   - Cập nhật Placeholder gợi ý mẫu chuẩn (Ví dụ: `Ví dụ: 001/2026/DAN`, `Ví dụ: 017/2026/HĐ`).

2. **Màn hình Đợt thanh toán**:
   - Thêm cột `Mã đợt thanh toán` (`code`) trong Data Table / danh sách hiển thị các đợt thanh toán của Hợp đồng.

3. **Màn hình Nghị quyết / Quyết định**:
   - Sử dụng ô nhập văn bản tự do (Text input) cho phép người dùng gõ số hiệu quyết định (Ví dụ: `QĐ-123/2026/QĐ-NH`).
