# Hướng dẫn tích hợp chức năng Hợp đồng (HopDong - DoiTac - DotThanhToan)

Tài liệu này hướng dẫn đội ngũ phát triển Frontend cách thức tích hợp với API của chức năng Hợp đồng đã được thiết kế lại ở Backend.

Toàn bộ các bảng và API liên quan đến hợp đồng đã được chuyển sang tên tiếng Việt:
1. **`DoiTac` (Đối tác)**: Lưu thông tin Chủ đầu tư và Nhà thầu.
2. **`HopDong` (Hợp đồng)**: Quản lý thông tin hợp đồng liên kết với gói thầu.
3. **`DotThanhToan` (Đợt thanh toán)**: Lưu thông tin các đợt thanh toán của hợp đồng.

---

## 1. API Đối tác (DoiTac)

API này quản lý danh sách các đối tác, bao gồm cả Chủ đầu tư và Nhà thầu.

* **Base URL**: `/api/doi-tac`
* **Quyền yêu cầu (Feature Code)**: `PARTNER`

### Cấu trúc dữ liệu Đối tác (`DoiTacDto`)

```json
{
  "id": "guid",
  "code": "MST-001", // Mã đối tác
  "name": "Tên đối tác / Công ty",
  "description": "Mô tả thêm",
  "taxCode": "0106008888", // Mã số thuế
  "phone": "02439744181", // Số điện thoại
  "email": "contact@coopbank.coop.vn",
  "address": "Địa chỉ đối tác",
  "account": "111000222333", // Số tài khoản ngân hàng
  "representative": "Nguyễn Văn A", // Người đại diện pháp luật
  "position": "Tổng Giám Đốc", // Chức vụ người đại diện
  "isActive": true,
  "createdAt": "2026-07-10T09:00:00Z",
  "updatedAt": null
}
```

---

## 2. API Hợp đồng (HopDong)

Mỗi Gói thầu (`GoiThau`) chỉ được liên kết với tối đa **1 Hợp đồng** (Quan hệ 1-1). Nếu cố gắng tạo hợp đồng thứ hai cho cùng một gói thầu, hệ thống sẽ trả về lỗi `400 Bad Request`.

* **Base URL**: `/api/hop-dong`
* **Quyền yêu cầu (Feature Code)**: `CONTRACT`

### Cấu trúc dữ liệu Hợp đồng (`HopDongDto`)

```json
{
  "id": "guid",
  "code": "CTR-2026-001", // Số ký hiệu hợp đồng
  "name": "Tên hợp đồng",
  "description": "Mô tả hợp đồng",
  "goiThauId": "guid", // ID Gói thầu liên kết
  "goiThauName": "Tên gói thầu tương ứng",
  "chuDauTuId": "guid", // ID Đối tác đóng vai trò Chủ đầu tư
  "chuDauTu": { ...DoiTacDto... },
  "nhaThauId": "guid", // ID Đối tác đóng vai trò Nhà thầu
  "nhaThau": { ...DoiTacDto... },
  "loaiHopDong": 1, // Loại hợp đồng (int)
  "thoiHanThucHien": "12 tháng", // Thời hạn thực hiện (string)
  "diaDiemThucHien": "Hà Nội", // Địa điểm thực hiện (string)
  "giaTriHopDong": 280000000.00, // Giá trị hợp đồng (decimal)
  "hinhThucThanhToan": 2, // Hình thức thanh toán: 1. Tiền mặt / 2. Chuyển khoản
  "ngayHieuLuc": "2026-07-05T00:00:00Z", // Ngày hiệu lực
  "expiredDate": "2027-07-05T00:00:00Z", // Ngày hết hạn (dành cho cảnh báo)
  "dotThanhToans": [
    {
      "id": "guid",
      "hopDongId": "guid",
      "tenDot": "Tạm ứng đợt 1",
      "tyLeThanhToan": 30.00, // % thanh toán
      "giaTriThanhToan": 84000000.00, // Giá trị thanh toán (tự động tính dựa trên % và Giá trị hợp đồng)
      "createdAt": "2026-07-10T09:00:00Z"
    },
    {
      "id": "guid",
      "hopDongId": "guid",
      "tenDot": "Thanh toán đợt 2",
      "tyLeThanhToan": 70.00,
      "giaTriThanhToan": 196000000.00,
      "createdAt": "2026-07-10T09:00:00Z"
    }
  ],
  "isActive": true
}
```

---

## 3. Luồng Nghiệp vụ & Các ràng buộc quan trọng (Validation Rules)

### Quy tắc khi Tạo/Cập nhật Hợp đồng
Khi gửi yêu cầu `POST /api/hop-dong` hoặc `PUT /api/hop-dong/{id}`, Frontend cần lưu ý các ràng buộc sau:

1. **Tổng tỷ lệ thanh toán của các đợt phải <= 100%**:
   * Tổng trường `tyLeThanhToan` của tất cả đối tượng trong mảng `dotThanhToans` phải nhỏ hơn hoặc bằng `100.00`.
   * Nếu tổng tỷ lệ vượt quá `100%`, API sẽ trả về lỗi: `400 Bad Request` với thông điệp: `"Tổng tỷ lệ các đợt thanh toán (110%) không được vượt quá 100%."`

2. **Tự động tính toán giá trị thanh toán**:
   * Hệ thống Backend sẽ tự động ghi đè hoặc tính toán trường `giaTriThanhToan` dựa trên công thức:
     $$\text{giaTriThanhToan} = \frac{\text{tyLeThanhToan} \times \text{giaTriHopDong}}{100}$$
   * Frontend chỉ cần gửi `tenDot` và `tyLeThanhToan` khi gửi payload, Backend sẽ tự động xử lý phần còn lại.

3. **Ràng buộc duy nhất gói thầu (Unique GoiThau)**:
   * Mỗi Gói thầu chỉ được phép gán tối đa cho 1 hợp đồng.
   * Nếu `goiThauId` gửi lên đã được liên kết với một hợp đồng khác đang có trên hệ thống, API sẽ báo lỗi `"Gói thầu này đã được liên kết với một hợp đồng khác."`

---

## 4. Ví dụ Payload để Frontend Gọi API

### Tạo mới Hợp đồng (`POST /api/hop-dong`)

**Request Payload**:
```json
{
  "code": "HD-2026-002",
  "name": "Hợp đồng thiết kế hệ thống mạng CoopBank",
  "description": "Cung cấp vật tư và thi công lắp đặt thiết bị mạng",
  "goiThauId": "22222222-2222-2222-2222-222222222222",
  "chuDauTuId": "33333333-3333-3333-3333-333333333333",
  "nhaThauId": "66666666-6666-6666-6666-666666666666",
  "loaiHopDong": 2,
  "thoiHanThucHien": "6 tháng",
  "diaDiemThucHien": "Tòa nhà CoopBank, Cầu Giấy, Hà Nội",
  "giaTriHopDong": 150000000.00,
  "hinhThucThanhToan": 2,
  "ngayHieuLuc": "2026-08-01T00:00:00Z",
  "expiredDate": "2027-02-01T00:00:00Z",
  "dotThanhToans": [
    {
      "tenDot": "Tạm ứng đợt 1",
      "tyLeThanhToan": 40.00
    },
    {
      "tenDot": "Nghiệm thu thanh toán đợt 2",
      "tyLeThanhToan": 60.00
    }
  ]
}
```

**Response (201 Created)**:
Hệ thống sẽ trả về đầy đủ đối tượng `HopDongDto` đã được tính toán đầy đủ `giaTriThanhToan` cho từng đợt (`60,000,000` và `90,000,000` tương ứng).
