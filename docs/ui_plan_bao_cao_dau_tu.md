# Kế hoạch Thiết kế và Phát triển Giao diện (UI Plan) - Báo cáo Đầu tư Dự án (Biểu số 02.A)

Tài liệu này phác thảo kế hoạch xây dựng giao diện báo cáo tình hình đầu tư dự án dựa trên chuẩn thiết kế đơn sắc (monochrome) kết hợp với màu thương hiệu (Co-opBank Blue), sử dụng **Tailwind CSS v4** và các thành phần **shadcn/ui**.

---

## 1. Cấu trúc Thư mục và Component (Component Structure)

Để tuân thủ nguyên tắc thiết kế **Feature-based Structure** và **Separation of Concerns**, mã nguồn phần UI báo cáo sẽ được tổ chức như sau:

```
frontend/src/
├── pages/
│   └── Reports.jsx                    # Container Page chính (Quản lý Route /reports/investment)
├── features/
│   └── finance/
│       ├── components/
│       │   ├── ReportFilters.jsx      # Presentational: Bộ lọc năm & kỳ báo cáo
│       │   ├── ReportTable.jsx        # Presentational: Bảng dữ liệu báo cáo 02.A đa tầng
│       │   └── ExportActions.jsx      # Presentational: Cụm nút bấm Xuất Excel/PDF
│       ├── hooks/
│       │   └── useInvestmentReport.js # Custom hook fetching dữ liệu từ API qua TanStack Query
│       └── utils/
│           └── reportHelpers.js       # Các hàm helper định dạng số, xuất file
```

---

## 2. Giao diện Tổng quan & Layout

Trang báo cáo sẽ sử dụng bố cục responsive tiêu chuẩn của hệ thống:

```
+-----------------------------------------------------------------------------------+
|  [PageHeader] Báo cáo tình hình đầu tư & huy động vốn (Biểu số 02.A)              |
+-----------------------------------------------------------------------------------+
|  [Filters & Actions]                                                              |
|  Năm: [ 2026 v ]  Kỳ báo cáo: [ 6T đầu năm | Cả năm ]   [ Xuất Excel ] [ Xuất PDF ]|
+-----------------------------------------------------------------------------------+
|  [Bảng số liệu - Bảng cuộn ngang]                                                  |
|  - Cột cố định (Tên dự án, quyết định)                                            |
|  - Các cột số liệu (Tổng mức đầu tư, Khối lượng thực hiện, Giải ngân, Bàn giao)   |
+-----------------------------------------------------------------------------------+
```

---

## 3. Thiết kế Chi tiết Thành phần (Component Design)

### 3.1. Bộ lọc và Hành động (`ReportFilters.jsx` & `ExportActions.jsx`)
*   **Thành phần sử dụng**:
    *   `Select` (shadcn/ui) cho việc chọn năm.
    *   `Tabs` (shadcn/ui) dạng Segmented Control cho việc chọn Kỳ báo cáo (6T / Cả năm) để mang lại cảm giác mượt mà và trực quan.
*   **Màu sắc & Style**:
    *   Sử dụng màu viền tối giản (`border-zinc-200`).
    *   Nút bấm Xuất dữ liệu sử dụng màu thương hiệu làm điểm nhấn (`bg-primary` cho nút chính và `variant="outline"` cho nút phụ).

### 3.2. Bảng Dữ liệu Đa tầng (`ReportTable.jsx`)
Đây là phần quan trọng nhất của UI báo cáo do bảng có lượng cột rất lớn (11 cột dữ liệu gốc chia nhỏ thành 13 cột hiển thị).

#### A. Cấu trúc Tiêu đề Bảng (Multi-level Table Header)
*   **Tầng 1 (Header Group)**:
    *   `Thông tin dự án` (TT, Tên dự án, Quyết định) - `rowSpan={2}`
    *   `Tổng mức vốn đầu tư` (Cột 4, 5) - `colSpan={2}`
    *   `Giá trị khối lượng thực hiện` (Cột 13, 14, 15) - `colSpan={3}`
    *   `Giải ngân` (Cột 16, 17, 18) - `colSpan={3}`
    *   `Giá trị tài sản bàn giao` (Cột 19) - `rowSpan={2}`
*   **Tầng 2 (Sub Header)**:
    *   Tổng mức vốn: `Tổng` | `Vốn chủ sở hữu`
    *   Khối lượng thực hiện: `Kỳ trước chuyển sang` | `Thực hiện trong kỳ` | `Lũy kế`
    *   Giải ngân: `Kỳ trước chuyển sang` | `Thực hiện trong kỳ` | `Lũy kế`

#### B. Phân cấp Dòng (Row Styling)
Để người dùng không bị rối mắt khi xem bảng số liệu dài:
*   **Dòng Nhóm lớn (Group Header)**:
    *   *Ví dụ*: `B. Các dự án nhóm B`, `C. Các dự án khác`
    *   *Style*: Chữ in đậm, nền xám nhạt (`bg-zinc-100/90`), viền dưới rõ ràng (`border-b border-zinc-300`). Có nút icon góc trái để Thu nhỏ/Mở rộng nhóm (Collapse/Expand).
*   **Dòng Nhóm con (Subgroup Header)**:
    *   *Ví dụ*: `I. Dự án đầu tư xây dựng`, `II. Dự án công nghệ thông tin`
    *   *Style*: Chữ in nghiêng, nền xám siêu nhạt (`bg-zinc-50/50`), lùi lề đầu dòng (`pl-6`).
*   **Dòng Dự án cụ thể (Project Row)**:
    *   *Style*: Chữ thường, nền trắng, hover đổi màu nhẹ (`hover:bg-zinc-50`). Tên dự án là liên kết màu xanh dương nhạt để bấm chuyển tiếp sang trang chi tiết của dự án.
*   **Dòng Tổng cộng (Grand Total Row)**:
    *   *Style*: Cố định ở dưới cùng (Sticky bottom), nền xám đậm (`bg-zinc-200/80`), chữ viết hoa in đậm.

#### C. Quy tắc Hiển thị Số liệu
*   Đơn vị tính hiển thị ở góc trên bên phải bảng: **Đơn vị tính: Triệu đồng**.
*   Các ô số liệu được căn lề phải (`text-right`) và sử dụng font chữ monospace để các con số thẳng hàng nhau theo chiều dọc, giúp người dùng dễ dàng so sánh độ lớn của số liệu:
    ```html
    className="font-mono text-right"
    ```
*   Giá trị bằng `0` hoặc null hiển thị dấu gạch ngang (`-`) để bảng thông thoáng hơn.

---

## 4. Quản lý Trạng thái & Tải dữ liệu (State & Data Fetching)

Tuân thủ hướng dẫn sử dụng **TanStack Query** và **Zustand**:

1.  **State cục bộ (React useState)**:
    *   `year`: lưu năm đang chọn (mặc định: năm hiện tại).
    *   `period`: lưu kỳ đang chọn (`1` hoặc `2`).
2.  **State toàn cầu (Zustand)**:
    *   Lưu trữ cấu hình thu gọn/mở rộng mặc định của các nhóm báo cáo nếu người dùng muốn lưu trạng thái qua các trang khác nhau.
3.  **Data Fetching (TanStack Query)**:
    *   Gọi hook `useInvestmentReport(year, period)`.
    *   Trong quá trình tải dữ liệu (`isLoading = true`), hiển thị bộ xương dữ liệu giả lập (Skeleton Table Loading) thay vì màn hình trắng để tạo cảm giác phản hồi nhanh.

---

## 5. Kế hoạch Verification (Kiểm thử UI)

*   **Responsive check**: Kiểm tra thanh cuộn ngang (`overflow-x-auto`) của bảng hoạt động mượt mà trên iPad và các màn hình laptop nhỏ mà không làm vỡ bố cục chung của trang Web.
*   **Data integrity**: Đối chiếu số liệu hiển thị trên bảng UI khớp 100% với file ảnh báo cáo gốc khi nhập cùng tham số đầu vào (Năm: 2026, Kỳ: 6T đầu năm).
*   **Interactive flow**: Kiểm tra nút gập/mở nhóm dự án hoạt động trơn tru; bấm vào dự án chuyển hướng đúng sang trang chi tiết dự án.
