# Báo cáo Theo dõi Tiến độ Thanh toán các Dự án Thầu

Tài liệu kỹ thuật và đặc tả chi tiết về **Báo cáo Theo dõi Tiến độ Thanh toán các Dự án Thầu** (khớp chuẩn 100% với mẫu Excel `Mẫu_Báo_cáo_theo_dõi_tiến_độ_thanh_toán_các_dự_án_thầu.xlsx`).

---

## 1. Tổng Quan Nghiệp Vụ

Báo cáo tổng hợp tình hình thực hiện hợp đồng, gói thầu và các đợt giải ngân thanh toán của các dự án thầu tại **Ngân hàng Hợp tác xã Việt Nam (CoopBank)**. 

Báo cáo kết nối dữ liệu từ 6 thực thể chính trong CSDL:
- **Dự án (`DuAn`)**: Tên dự án, nguồn vốn hình thành, số quyết định phê duyệt dự toán.
- **Gói thầu (`GoiThau`)**: Tên gói thầu, số quyết định phê duyệt kết quả lựa chọn nhà thầu (KQLCNT).
- **Nhà thầu / Đối tác (`DoiTac`)**: Tên công ty trúng thầu, mã số thuế, địa chỉ.
- **Hợp đồng (`HopDong`)**: Số hợp đồng, ngày ký, thời gian thực hiện, giá trị hợp đồng.
- **Đợt thanh toán (`DotThanhToan`)**: Số tiền tạm ứng, số tiền thanh toán các lần (Lần 1, Lần 2, Lần 3...).
- **Nguồn vốn (`NguonVon`)**: Nguồn kinh phí (Chi phí, Vốn điều lệ, Quỹ đầu tư phát triển...).

---

## 2. Cấu Trúc Các Cột Trong Báo Cáo (18 Cột Chuẩn)

| STT | Cột Báo Cáo | Loại Dữ Liệu | Nguồn Dữ Liệu CSDL | Mô Tả Chi Tiết |
| :---: | :--- | :---: | :--- | :--- |
| **1** | **STT** | Text | Tự động sinh (`1.1`, `1.2`, `2.1`...) | Đánh số thứ tự theo từng Dự án và Gói thầu thuộc dự án. |
| **2** | **Nguồn vốn hình thành** | Text | `DuAn.DanhSachNguonVon` ➔ `NguonVon.Name` | Tên các nguồn vốn (Chi phí, Vốn điều lệ, Quỹ ĐTPT...). |
| **3** | **Dự án** | Text | `DuAn.Name` | Tên Dự án nguồn hoặc Dự án triển khai. |
| **4** | **Quyết định phê duyệt dự toán** | Text | `DuAn.SoQuyetDinhPheDuyetDuToan` / `SoQuyetDinh` | Số quyết định phê duyệt dự toán của Dự án/Gói thầu. |
| **5** | **Tên gói thầu** | Text | `GoiThau.Name` / `HopDong.Name` | Tên gói thầu triển khai. |
| **6** | **Quyết định phê duyệt KQLCNT** | Text | `GoiThau.SoQuyetDinhKQLCNT` | Số quyết định phê duyệt kết quả lựa chọn nhà thầu. |
| **7** | **Công ty** | Text | `DoiTac.Name` (`HopDong.NhaThau`) | Tên Công ty / Nhà thầu trúng thầu ký HĐ. |
| **8** | **Mã số thuế** | Text | `DoiTac.TaxCode` | Mã số thuế của nhà thầu. |
| **9** | **Địa chỉ** | Text | `DoiTac.Address` | Địa chỉ đăng ký kinh doanh/trụ sở nhà thầu. |
| **10** | **Số HĐ** | Text | `HopDong.Code` | Số / Ký hiệu hợp đồng. |
| **11** | **Ngày ký** | Date | `HopDong.NgayKy` / `NgayHieuLuc` | Ngày ký kết hợp đồng (Định dạng: `dd/MM/yyyy`). |
| **12** | **Thời gian thực hiện HĐ** | Text | `HopDong.ThoiHanThucHien` | Thời gian thực hiện hợp đồng (vd: "90 ngày", "12 tháng"). |
| **13** | **Giá trị HĐ** | Decimal | `HopDong.GiaTriHopDong` | Giá trị hợp đồng (VNĐ). |
| **14** | **Tạm ứng** | Decimal | `DotThanhToan` (Tên đợt có chứa "tạm ứng") | Tổng giá trị đợt tạm ứng hợp đồng. |
| **15+** | **Lần 1, Lần 2, Lần 3...** | Decimal | `DotThanhToan` (Các đợt thanh toán tiếp theo) | Số tiền thanh toán theo từng đợt thực tế/kế hoạch. Cột sinh động theo số đợt tối đa. |
| **End** | **Ghi chú** | Text | `HopDong.Description` | Ghi chú quyết toán (vd: "Đã quyết toán 322/QĐ-NHHT ngày 24/07/2025"). |

---

## 3. Các Trường Dữ Liệu Nâng Cấp CSDL

Hệ thống đã nâng cấp các Entity & DTOs tương ứng để lưu trữ đủ 100% dữ liệu báo cáo:

1. **`GoiThau` / `GoiThauDto` / `CreateGoiThauDto` / `UpdateGoiThauDto`**:
   - `SoQuyetDinhKQLCNT` (`string?`): Số quyết định phê duyệt KQLCNT.
   - `NgayPheDuyetKQLCNT` (`DateTime?`): Ngày phê duyệt KQLCNT.
2. **`HopDong` / `HopDongDto` / `CreateHopDongDto` / `UpdateHopDongDto`**:
   - `NgayKy` (`DateTime?`): Ngày ký kết hợp đồng.
3. **`DuAn` / `DuAnDto` / `CreateDuAnDto` / `UpdateDuAnDto`**:
   - `SoQuyetDinhPheDuyetDuToan` (`string?`): Số quyết định phê duyệt dự toán.

---

## 4. Chi Tiết Danh Sách APIs

### 4.1. Lấy Dữ Liệu Báo Cáo dạng JSON
- **URL**: `GET /api/NghiepVu/reports/tien-do-thanh-toan-du-an-thau`
- **Aliases**: `/api/NghiepVu/report/tien-do-thanh-toan-du-an-thau`
- **Headers**: `Authorization: Bearer <access_token>`
- **Query Parameters**:
  - `year` (int, optional): Lọc theo năm ký/hiệu lực HĐ.
  - `duAnId` (guid, optional): Lọc theo ID dự án cụ thể.
  - `search` (string, optional): Tìm kiếm theo từ khóa (Dự án, Gói thầu, Nhà thầu, Hợp đồng).
  - `donViTinh` (string, optional): Đơn vị tính (mặc định: `Đồng`).

#### Response JSON Sample (`200 OK`):
```json
{
  "title": "BÁO CÁO THEO DÕI TIẾN ĐỘ THANH TOÁN CÁC DỰ ÁN THẦU",
  "unit": "Đồng",
  "maxDotThanhToanCount": 3,
  "summary": {
    "tongSoHopDong": 5,
    "tongGiaTriHopDong": 15000000000.0,
    "tongTamUng": 3000000000.0,
    "tongDaThanhToan": 9000000000.0
  },
  "rows": [
    {
      "stt": 1,
      "sttDisplay": "1.1",
      "nguonVon": "Chi phí",
      "tenDuAn": "Gia hạn bản quyền trung tâm SOC",
      "soQuyetDinhPheDuyetDuToan": "322/QĐ-NHHT",
      "tenGoiThau": "Tư vấn thẩm định giá",
      "soQuyetDinhKQLCNT": "105/QĐ-NHHT",
      "tenNhaThau": "Công ty Cổ phần Dịch vụ tư vấn và Thẩm định giá Việt Nam",
      "maSoThue": "0102030405",
      "diaChi": "Hà Nội",
      "soHopDong": "0804/2025/VCVS-HĐTĐG",
      "ngayKy": "2025-04-08T00:00:00",
      "thoiGianThucHien": "30 ngày",
      "giaTriHopDong": 50000000.0,
      "tamUng": 10000000.0,
      "cacLanThanhToan": [40000000.0],
      "ghiChu": "Đã quyết toán 322/QĐ-NHHT ngày 24/07/2025",
      "hopDongId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "goiThauId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
      "duAnId": "3fa85f64-5717-4562-b3fc-2c963f66afa8"
    }
  ]
}
```

---

### 4.2. Xuất File Báo Cáo (Excel / CSV / HTML / Base64)
- **URL**: `GET /api/NghiepVu/reports/tien-do-thanh-toan-du-an-thau/export`
- **Aliases**: `/api/NghiepVu/report/tien-do-thanh-toan-du-an-thau/export`
- **Query Parameters**:
  - `year`, `duAnId`, `search`, `donViTinh` (Tương tự API trên).
  - `format` (string, default: `"xlsx"`): Định dạng xuất (`xlsx`, `csv`, `html`).
  - `base64` (boolean, default: `false`): Nếu `true`, trả về chuỗi JSON chứa dữ liệu mã hóa Base64 thay vì tải file về.

---

## 5. Kiểm Thử & Xác Nhận

- **Biên dịch**: Đã biên dịch toàn bộ mã nguồn Backend thành công với `0 Error(s)`.
- **Đã kiểm định:**
  - File Excel xuất ra tự động định dạng header màu xám xanh (`#D9E1F2`), canh lề chuẩn, căn lề phải cho cột tiền tệ và định dạng số `#,#0`.
  - Tự động sinh dòng tổng cộng ở cuối bảng báo cáo.
