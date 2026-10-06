# HƯỚNG DẪN CẬP NHẬT FRONTEND: BỔ SUNG QUYẾT ĐỊNH THÀNH LẬP & BÁO CÁO BIỂU SỐ 02.A

Tài liệu này tổng hợp toàn bộ các thay đổi mới nhất từ Backend liên quan đến việc bổ sung trường **Quyết định thành lập dự án** và màn hình **Báo cáo Biểu số 02.A (Thông tư số 200/2015/TT-BTC)** dành cho đội ngũ phát triển Frontend (React / TypeScript).

---

## 1. Cập nhật Model & Form Quản lý Dự án

### 1.1. Bổ sung trường vào Interface `DuAn`
Trong các file định nghĩa kiểu dữ liệu dự án (ví dụ `src/types/duAn.ts` hoặc `src/features/projects/types/index.ts`):

```typescript
export interface DuAn {
  id: string;
  code: string;
  name: string;
  duToanPheDuyet: number;
  
  // 👉 2 TRƯỜNG MỚI BỔ SUNG:
  soQuyetDinhThanhLap?: string | null;      // Số QĐ thành lập dự án (vd: "452/QĐ-NHHT")
  ngayQuyetDinhThanhLap?: string | null;    // Ngày ban hành QĐ thành lập (ISO Date string: "2024-08-12T00:00:00Z")

  // Các trường quyết định hiện có:
  soQuyetDinh?: string | null;              // Số QĐ phê duyệt dự án
  soQuyetDinhPheDuyetDuToan?: string | null;// Số QĐ phê duyệt dự toán
  thoiGianThucHien?: string | null;         // Thời gian thực hiện (vd: "2025-2027", "14 tháng")
  ngayBatDau?: string | null;
  ngayKetThuc?: string | null;
  namBatDau?: number | null;
  namKetThuc?: number | null;
  // ... các trường khác giữ nguyên
}

export interface CreateDuAnDto {
  code: string;
  name: string;
  duToanPheDuyet: number;
  soQuyetDinhThanhLap?: string;
  ngayQuyetDinhThanhLap?: string | Date;
  soQuyetDinh?: string;
  soQuyetDinhPheDuyetDuToan?: string;
  // ...
}

export interface UpdateDuAnDto extends CreateDuAnDto {
  id: string;
}
```

### 1.2. Cập nhật Form Thêm mới / Cập nhật Dự án (`ProjectFormModal` / `ProjectDrawer`)
* **Thêm 2 ô nhập liệu** tại khu vực **Thông tin Pháp lý / Quyết định dự án**:
  1. **Số Quyết định thành lập**: `Input` text, placeholder: *"Nhập số QĐ thành lập (vd: 452/QĐ-NHHT)..."*.
  2. **Ngày Quyết định thành lập**: `DatePicker`, format hiển thị: `DD/MM/YYYY`.
* **Ghi chú nghiệp vụ trên UI**:
  > *Lưu ý: Dự án bắt buộc phải có Quyết định thành lập (Số QĐ hoặc Ngày QĐ) để được đưa vào Báo cáo Biểu số 02.A theo quy định.*

---

## 2. Cập nhật Phân quyền & Menu Điều hướng

### 2.1. Feature Code Mới: `BAO_CAO_02A`
* **Mã chức năng (Feature Code)**: `BAO_CAO_02A`
* **Aliases hỗ trợ**: `BAO_CAO_BIEU_02A`, `BIEU_02A`, `BIEU_MAU_02A`
* **Tên quyền**: `Báo cáo Biểu số 02.A`
* **Mô tả**: *Báo cáo tổng hợp tình hình thực hiện dự án đầu tư theo Thông tư 200 (Đơn vị tính: Tỷ đồng)*
* **Nhóm cha**: `BAO_CAO` *(Báo cáo & Thống kê)*

> [!IMPORTANT]
> **Không sử dụng chung quyền `BAO_CAO_DAU_TU`**. Feature `BAO_CAO_02A` là quyền độc lập dành riêng cho Biểu số 02.A.

### 2.2. Khai báo Route & Menu Điều hướng
* **Đường dẫn Route**: `/reports/bieu-mau-02a` (hoặc `/reports/bieu-02a`)
* **Component**: `BieuMau02AReportPage`
* **Vị trí Menu**: Sidebar > Nhóm **Báo cáo & Thống kê** > Mục **Báo cáo Biểu số 02.A (TT 200)**.

---

## 3. Màn hình Báo cáo Biểu số 02.A (`BieuMau02AReportPage`)

### 3.1. Danh sách Endpoints API

#### 🔸 1. Lấy dữ liệu Báo cáo (JSON):
* **Method**: `GET`
* **URL**: `/api/NghiepVu/reports/bieu-mau-02a` *(hoặc alias `/api/NghiepVu/reports/bieu-02a`)*
* **Headers**: `Authorization: Bearer <token>`
* **Query Parameters**:
  - `year` *(number, optional)*: Năm báo cáo (mặc định: năm hiện tại, vd: `2026`).
  - `period` *(number, optional)*: Kỳ báo cáo (mặc định: `1`):
    - `1`: Cả năm
    - `2`: 6 tháng đầu năm
    - `3`: 6 tháng cuối năm
    - `4, 5, 6, 7`: Quý 1, Quý 2, Quý 3, Quý 4
    - `11 .. 22`: Tháng 1 đến Tháng 12
    - `0`: Khoảng thời gian tùy chọn (dùng `fromDate` & `toDate`)
  - `fromDate` *(string, optional)*: Từ ngày dạng `YYYY-MM-DD`.
  - `toDate` *(string, optional)*: Đến ngày dạng `YYYY-MM-DD`.

#### 🔸 2. Xuất File Báo Cáo (Excel / CSV / HTML):
* **Method**: `GET`
* **URL**: `/api/NghiepVu/reports/bieu-mau-02a/export` *(hoặc alias `/api/NghiepVu/reports/bieu-02a/export`)*
* **Query Parameters**: Giống API 1 kèm:
  - `format` *(string, optional)*: `xlsx` *(mặc định)*, `csv`, hoặc `html`.
  - `base64` *(boolean, optional)*: `false` (mặc định tải file stream) hoặc `true` (nhận chuỗi base64).

---

### 3.2. Cấu trúc Response Data Model

```typescript
export interface ReportRowDto {
  stt: string;                           // "A", "B", "I", "II", "1", "2", "Cộng A", ""
  rowType: 'BlockHeader' | 'GroupHeader' | 'SubGroupHeader' | 'ProjectRow' | 'BlockFooter' | 'GrandTotal';
  projectName: string;                   // Tên khối / Tên nhóm / Tên dự án / Dòng tổng cộng
  approvalDecision: string;              // "452/QĐ-NHHT ngày 12/08/2024"
  thoiGianThucHien?: string;             // "2025-2027", "14 tháng", "270 ngày"
  
  // Các cột số liệu (Đơn vị tính ĐÃ ĐƯỢC CHUYỂN ĐỔI SẴN: TỶ ĐỒNG)
  tongMucDauTuTong: number;              // (4) Tổng mức đầu tư Tổng
  tongMucDauTuVCSH: number;              // (5) Vốn chủ sở hữu
  phanTramVCSH: number;                  // (6) % Vốn CSH
  tongMucDauTuVay: number;               // (7) & (10) Vốn vay
  phanTramVay: number;                   // (8) % Vốn vay
  thoiHanVay?: string;                   // (11) Thời hạn vay
  laiSuat?: number;                      // (12) Lãi suất (%)
  
  // Khối lượng thực hiện (= Giải ngân)
  khoiLuongKyTruoc: number;              // (13) Kỳ trước chuyển sang
  khoiLuongTrongKy: number;              // (14) Thực hiện trong kỳ
  khoiLuongLuyKe: number;                // (15) Lũy kế đến cuối kỳ
  
  // Giải ngân
  giaiNganKyTruoc: number;               // (16) Kỳ trước chuyển sang (= Cột 13)
  giaiNganTrongKy: number;               // (17) Thực hiện trong kỳ (= Cột 14)
  giaiNganLuyKe: number;                 // (18) Lũy kế đến cuối kỳ (= Cột 15)
  
  taiSanBanGiao: number;                 // (19) Giá trị tài sản hoàn thành đưa vào SD
}

export interface Bieu02AResponse {
  title: string;                         // "BÁO CÁO TỔNG HỢP TÌNH HÌNH THỰC HIỆN DỰ ÁN ĐẦU TƯ (BIỂU SỐ 02.A)"
  unit: string;                          // "Tỷ đồng"
  year: number;
  period: number;
  periodName: string;                    // "Cả năm", "Quý III", "6T đầu năm"...
  fromDate?: string;
  toDate?: string;
  rows: ReportRowDto[];
}
```

---

### 3.3. Quy tắc Hiển thị & Cấu trúc Bảng 19 Cột

Bảng báo cáo gồm **19 cột** chuẩn TT 200/2015/TT-BTC:
1. **(1) TT**
2. **(2) Tên dự án**
3. **(3) Quyết định phê duyệt / thành lập**
4. **(4) Tổng mức vốn đầu tư - Tổng**
5. **(5) Vốn CSH**
6. **(6) % Vốn CSH**
7. **(7) Vốn huy động / Vay**
8. **(8) % Vốn huy động**
9. **(9) Thời gian đầu tư theo KH**
10. **(10) Tổng số nguồn vốn vay**
11. **(11) Thời hạn vay**
12. **(12) Lãi suất (%)**
13. **(13) Khối lượng thực hiện - Kỳ trước chuyển sang**
14. **(14) Khối lượng thực hiện - Trong kỳ**
15. **(15) Khối lượng thực hiện - Lũy kế đến cuối kỳ**
16. **(16) Giải ngân - Kỳ trước chuyển sang**
17. **(17) Giải ngân - Trong kỳ**
18. **(18) Giải ngân - Lũy kế đến cuối kỳ**
19. **(19) Giá trị tài sản hoàn thành đưa vào SD**

#### Thứ tự phân khối và styling theo `rowType`:
1. **`BlockHeader` (Dòng tiêu đề Khối A và B)**:
   - `PHẦN A: CÁC DỰ ÁN CÓ QUYẾT ĐỊNH THÀNH LẬP TRƯỚC KỲ BÁO CÁO`
   - `PHẦN B: CÁC DỰ ÁN CÓ QUYẾT ĐỊNH THÀNH LẬP TRONG KỲ BÁO CÁO`
   - *Styling*: Nền xanh dương nhạt (`bg-blue-100`), chữ in hoa đậm (`font-bold text-blue-900`).
2. **`GroupHeader` (Dòng Nhóm quy mô)**:
   - `I. Nhóm A (> 800 tỷ)`, `II. Nhóm B (45 - 800 tỷ)`, `III. Nhóm C (< 45 tỷ)`.
   - *Styling*: Nền xám nhạt (`bg-slate-100 font-bold`).
3. **`SubGroupHeader` (Dòng Lĩnh vực con)**:
   - `1. Dự án XDCB`, `2. Dự án CNTT`, `3. Dự án Khác`.
   - *Styling*: In nghiêng, thụt đầu dòng (`italic font-semibold pl-6`).
4. **`ProjectRow` (Dòng dự án chi tiết)**:
   - Hiển thị đầy đủ 19 cột.
   - Định dạng số: `#,##0.00` (đơn vị: Tỷ đồng). Nếu giá trị bằng 0 hiển thị `"-"`. Cột `%` hiển thị `0.0%`.
5. **`BlockFooter` (Dòng Cộng Phần A / Cộng Phần B)**:
   - `Cộng PHẦN A`, `Cộng PHẦN B`.
   - *Styling*: Nền tím nhạt (`bg-indigo-50 font-bold text-indigo-950`).
6. **`GrandTotal` (Dòng Tổng cộng toàn bộ)**:
   - `TỔNG CỘNG TOÀN BỘ (PHẦN A + PHẦN B)`.
   - *Styling*: Nền vàng nhạt nổi bật (`bg-amber-100 font-black text-amber-950 border-t-2 border-amber-400`).

---

### 3.4. Code Mẫu React Component (`BieuMau02AReportTable.tsx`)

```tsx
import React from 'react';
import { ReportRowDto } from '@/types/reports';

interface Props {
  rows: ReportRowDto[];
  loading?: boolean;
}

const formatNumber = (val?: number | null, isPercent = false): string => {
  if (val === null || val === undefined || val === 0) return '-';
  if (isPercent) return `${val.toFixed(1)}%`;
  return new Intl.NumberFormat('vi-VN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(val);
};

export const BieuMau02AReportTable: React.FC<Props> = ({ rows, loading }) => {
  if (loading) {
    return <div className="py-12 text-center text-slate-500">Đang tải dữ liệu báo cáo...</div>;
  }

  return (
    <div className="overflow-x-auto border rounded-lg shadow-sm">
      <table className="w-full text-xs text-left border-collapse">
        <thead className="bg-[#1E3A8A] text-white text-center font-bold text-xs sticky top-0 z-10">
          <tr>
            <th rowSpan={2} className="border border-blue-900 p-2 w-12">TT</th>
            <th rowSpan={2} className="border border-blue-900 p-2 min-w-[240px]">Tên dự án</th>
            <th rowSpan={2} className="border border-blue-900 p-2 min-w-[180px]">Quyết định phê duyệt</th>
            <th colSpan={5} className="border border-blue-900 p-2">Tổng mức vốn đầu tư</th>
            <th rowSpan={2} className="border border-blue-900 p-2 min-w-[110px]">Thời gian ĐT</th>
            <th colSpan={3} className="border border-blue-900 p-2">Nguồn vốn huy động</th>
            <th colSpan={3} className="border border-blue-900 p-2">Giá trị khối lượng thực hiện</th>
            <th colSpan={3} className="border border-blue-900 p-2">Giải ngân thực tế</th>
            <th rowSpan={2} className="border border-blue-900 p-2 min-w-[120px]">Tài sản bàn giao</th>
          </tr>
          <tr>
            <th className="border border-blue-900 p-1.5 min-w-[90px]">Tổng</th>
            <th className="border border-blue-900 p-1.5 min-w-[80px]">Vốn CSH</th>
            <th className="border border-blue-900 p-1.5 w-14">% CSH</th>
            <th className="border border-blue-900 p-1.5 min-w-[80px]">Vốn vay</th>
            <th className="border border-blue-900 p-1.5 w-14">% Vay</th>
            <th className="border border-blue-900 p-1.5 min-w-[80px]">Tổng số</th>
            <th className="border border-blue-900 p-1.5 min-w-[70px]">Thời hạn</th>
            <th className="border border-blue-900 p-1.5 w-14">Lãi suất</th>
            <th className="border border-blue-900 p-1.5 min-w-[85px]">Trước kỳ</th>
            <th className="border border-blue-900 p-1.5 min-w-[85px]">Trong kỳ</th>
            <th className="border border-blue-900 p-1.5 min-w-[90px]">Lũy kế</th>
            <th className="border border-blue-900 p-1.5 min-w-[85px]">Trước kỳ</th>
            <th className="border border-blue-900 p-1.5 min-w-[85px]">Trong kỳ</th>
            <th className="border border-blue-900 p-1.5 min-w-[90px]">Lũy kế</th>
          </tr>
          <tr className="bg-slate-200 text-slate-700 italic text-[11px] font-normal">
            {Array.from({ length: 19 }, (_, i) => (
              <th key={i} className="border border-slate-300 p-1">({i + 1})</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((r, idx) => {
            if (r.rowType === 'BlockHeader') {
              return (
                <tr key={idx} className="bg-blue-100 text-blue-900 font-bold">
                  <td className="text-center p-2 border">{r.stt}</td>
                  <td colSpan={18} className="p-2 border text-left uppercase tracking-wide">{r.projectName}</td>
                </tr>
              );
            }
            if (r.rowType === 'GroupHeader') {
              return (
                <tr key={idx} className="bg-slate-100 text-slate-800 font-bold">
                  <td className="text-center p-1.5 border">{r.stt}</td>
                  <td colSpan={18} className="p-1.5 border text-left">{r.projectName}</td>
                </tr>
              );
            }
            if (r.rowType === 'SubGroupHeader') {
              return (
                <tr key={idx} className="bg-slate-50 text-slate-700 font-semibold italic">
                  <td className="border"></td>
                  <td colSpan={18} className="p-1.5 border pl-6">{r.projectName}</td>
                </tr>
              );
            }
            if (r.rowType === 'BlockFooter') {
              return (
                <tr key={idx} className="bg-indigo-50 text-indigo-950 font-bold border-t-2 border-indigo-200">
                  <td className="text-center p-2 border">{r.stt}</td>
                  <td className="p-2 border font-bold">{r.projectName}</td>
                  <td className="border"></td>
                  <td className="p-2 border text-right">{formatNumber(r.tongMucDauTuTong)}</td>
                  <td className="p-2 border text-right">{formatNumber(r.tongMucDauTuVCSH)}</td>
                  <td className="p-2 border text-right">{formatNumber(r.phanTramVCSH, true)}</td>
                  <td className="p-2 border text-right">{formatNumber(r.tongMucDauTuVay)}</td>
                  <td className="p-2 border text-right">{formatNumber(r.phanTramVay, true)}</td>
                  <td className="border"></td>
                  <td className="p-2 border text-right">{formatNumber(r.tongMucDauTuVay)}</td>
                  <td className="border"></td>
                  <td className="border"></td>
                  <td className="p-2 border text-right">{formatNumber(r.khoiLuongKyTruoc)}</td>
                  <td className="p-2 border text-right">{formatNumber(r.khoiLuongTrongKy)}</td>
                  <td className="p-2 border text-right">{formatNumber(r.khoiLuongLuyKe)}</td>
                  <td className="p-2 border text-right">{formatNumber(r.giaiNganKyTruoc)}</td>
                  <td className="p-2 border text-right">{formatNumber(r.giaiNganTrongKy)}</td>
                  <td className="p-2 border text-right">{formatNumber(r.giaiNganLuyKe)}</td>
                  <td className="p-2 border text-right">{formatNumber(r.taiSanBanGiao)}</td>
                </tr>
              );
            }
            if (r.rowType === 'GrandTotal') {
              return (
                <tr key={idx} className="bg-amber-100 text-amber-950 font-black border-t-2 border-amber-400">
                  <td className="text-center p-2.5 border">{r.stt}</td>
                  <td className="p-2.5 border uppercase">{r.projectName}</td>
                  <td className="border"></td>
                  <td className="p-2.5 border text-right">{formatNumber(r.tongMucDauTuTong)}</td>
                  <td className="p-2.5 border text-right">{formatNumber(r.tongMucDauTuVCSH)}</td>
                  <td className="p-2.5 border text-right">{formatNumber(r.phanTramVCSH, true)}</td>
                  <td className="p-2.5 border text-right">{formatNumber(r.tongMucDauTuVay)}</td>
                  <td className="p-2.5 border text-right">{formatNumber(r.phanTramVay, true)}</td>
                  <td className="border"></td>
                  <td className="p-2.5 border text-right">{formatNumber(r.tongMucDauTuVay)}</td>
                  <td className="border"></td>
                  <td className="border"></td>
                  <td className="p-2.5 border text-right">{formatNumber(r.khoiLuongKyTruoc)}</td>
                  <td className="p-2.5 border text-right">{formatNumber(r.khoiLuongTrongKy)}</td>
                  <td className="p-2.5 border text-right">{formatNumber(r.khoiLuongLuyKe)}</td>
                  <td className="p-2.5 border text-right">{formatNumber(r.giaiNganKyTruoc)}</td>
                  <td className="p-2.5 border text-right">{formatNumber(r.giaiNganTrongKy)}</td>
                  <td className="p-2.5 border text-right">{formatNumber(r.giaiNganLuyKe)}</td>
                  <td className="p-2.5 border text-right">{formatNumber(r.taiSanBanGiao)}</td>
                </tr>
              );
            }

            // ProjectRow
            return (
              <tr key={idx} className="hover:bg-slate-50 transition-colors">
                <td className="text-center p-1.5 border">{r.stt}</td>
                <td className="p-1.5 border pl-6 font-medium text-slate-900">{r.projectName}</td>
                <td className="p-1.5 border text-slate-600">{r.approvalDecision || '-'}</td>
                <td className="p-1.5 border text-right font-medium">{formatNumber(r.tongMucDauTuTong)}</td>
                <td className="p-1.5 border text-right">{formatNumber(r.tongMucDauTuVCSH)}</td>
                <td className="p-1.5 border text-right">{formatNumber(r.phanTramVCSH, true)}</td>
                <td className="p-1.5 border text-right">{formatNumber(r.tongMucDauTuVay)}</td>
                <td className="p-1.5 border text-right">{formatNumber(r.phanTramVay, true)}</td>
                <td className="p-1.5 border text-center">{r.thoiGianThucHien || '-'}</td>
                <td className="p-1.5 border text-right">{formatNumber(r.tongMucDauTuVay)}</td>
                <td className="p-1.5 border text-center">{r.thoiHanVay || '-'}</td>
                <td className="p-1.5 border text-center">{formatNumber(r.laiSuat, true)}</td>
                <td className="p-1.5 border text-right">{formatNumber(r.khoiLuongKyTruoc)}</td>
                <td className="p-1.5 border text-right">{formatNumber(r.khoiLuongTrongKy)}</td>
                <td className="p-1.5 border text-right font-medium">{formatNumber(r.khoiLuongLuyKe)}</td>
                <td className="p-1.5 border text-right">{formatNumber(r.giaiNganKyTruoc)}</td>
                <td className="p-1.5 border text-right">{formatNumber(r.giaiNganTrongKy)}</td>
                <td className="p-1.5 border text-right font-medium">{formatNumber(r.giaiNganLuyKe)}</td>
                <td className="p-1.5 border text-right">{formatNumber(r.taiSanBanGiao)}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
};
```
