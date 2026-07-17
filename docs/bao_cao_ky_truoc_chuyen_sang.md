# Tài liệu giải thích và hướng dẫn xử lý "Kỳ trước chuyển sang" trên Báo cáo Biểu số 02.A

Tài liệu này giải thích chi tiết về mặt nghiệp vụ kế toán xây dựng cơ bản (theo Thông tư 200/2015/TT-BTC) và đề xuất giải pháp lập trình hệ thống cho các cột dữ liệu liên quan đến **"Kỳ trước chuyển sang"**.

---

## 1. Bản chất nghiệp vụ: Khi nào có giá trị "Kỳ trước chuyển sang"?

Cột **"Kỳ trước chuyển sang"** (gồm Cột 13 - *Khối lượng thực hiện* và Cột 16 - *Giải ngân*) hiển thị giá trị tích lũy (lũy kế) của dự án **trước thời điểm bắt đầu của kỳ báo cáo hiện tại**.

### Ví dụ thực tế từ Báo cáo 6T đầu năm 2026:
*   **Kỳ báo cáo hiện tại**: 6T đầu năm 2026 (từ `01/01/2026` đến `30/06/2026`).
*   **Kỳ trước**: Toàn bộ khoảng thời gian từ khi dự án bắt đầu cho tới hết ngày `31/12/2025`.

| Dự án | Ngày phê duyệt | Kỳ trước chuyển sang | Giải thích |
| :--- | :--- | :--- | :--- |
| **DLP** (Triển khai DLP) | 07/04/2026 | **0** (hoặc trống) | Dự án được phê duyệt trong kỳ báo cáo (năm 2026), không có phát sinh trước 2026. |
| **HSM** (Mua sắm máy chủ HSM) | 06/05/2026 | **0** (hoặc trống) | Tương tự, dự án mới phê duyệt và thực hiện hoàn toàn trong năm 2026. |
| **FTP** (Phần mềm định giá FTP) | 19/03/2026 | **0** (hoặc trống) | Dự án bắt đầu trong năm 2026. |
| **Dự án A** *(Giả định)* | 15/05/2024 | **Lũy kế phát sinh trước 2026** | Đã nghiệm thu/thanh toán trong năm 2024, 2025. Giá trị này chuyển tiếp sang làm số dư đầu kỳ năm 2026. |

---

## 2. Giải pháp xử lý trên Hệ thống (C# / .NET & Database)

Để tính toán chính xác dữ liệu động theo năm và kỳ được chọn, hệ thống cần xử lý như sau:

### Bước 2.1: Xác định các mốc thời gian biên (Time Boundaries)
Dựa vào tham số đầu vào `year` (Năm) và `period` (Kỳ báo cáo: `1` cho 6 tháng đầu năm, `2` cho cả năm):

*   `startOfPeriod`: Ngày bắt đầu kỳ báo cáo hiện tại.
    *   Mặc định: `01/01/{year}` (ví dụ: `01/01/2026 00:00:00`)
*   `endOfPeriod`: Ngày kết thúc kỳ báo cáo hiện tại.
    *   Nếu `period == 1` (6T): `30/06/{year}` (ví dụ: `30/06/2026 23:59:59`)
    *   Nếu `period == 2` (1N): `31/12/{year}` (ví dụ: `31/12/2026 23:59:59`)

---

### Bước 2.2: Công thức truy vấn và tính toán số liệu

Mỗi dự án (`DuAn`) liên kết với nhiều Hợp đồng (`HopDong`), mỗi hợp đồng có nhiều Đợt thanh toán/nghiệm thu (`DotThanhToan`).

#### A. Nhóm cột "Giá trị khối lượng thực hiện" (Cột 13, 14, 15)
Thể hiện giá trị công việc đã nghiệm thu hoàn thành (chưa quan tâm đã thanh toán tiền hay chưa):

1.  **Kỳ trước chuyển sang (Cột 13)**:
    Tổng giá trị nghiệm thu trước thời điểm bắt đầu kỳ hiện tại.
    ```sql
    SELECT SUM(GiaTriThanhToan) FROM DotThanhToan 
    WHERE DuAnId = @DuAnId AND NgayThanhToan < @startOfPeriod
    ```
2.  **Thực hiện trong kỳ (Cột 14)**:
    Tổng giá trị nghiệm thu phát sinh trong kỳ hiện tại.
    ```sql
    SELECT SUM(GiaTriThanhToan) FROM DotThanhToan 
    WHERE DuAnId = @DuAnId AND NgayThanhToan >= @startOfPeriod AND NgayThanhToan <= @endOfPeriod
    ```
3.  **Lũy kế thực hiện (Cột 15)**:
    $$\text{Cột 15} = \text{Cột 13} + \text{Cột 14}$$

#### B. Nhóm cột "Giải ngân" (Cột 16, 17, 18)
Thể hiện số tiền thực tế chủ đầu tư đã chi trả/chuyển khoản cho nhà thầu:

1.  **Kỳ trước chuyển sang (Cột 16)**:
    Tổng số tiền đã giải ngân thành công trước kỳ hiện tại.
    ```sql
    SELECT SUM(GiaTriThanhToan) FROM DotThanhToan 
    WHERE DuAnId = @DuAnId AND IsPaid = true AND NgayThanhToan < @startOfPeriod
    ```
2.  **Thực hiện trong kỳ (Cột 17)**:
    Tổng số tiền đã giải ngân thành công phát sinh trong kỳ hiện tại.
    ```sql
    SELECT SUM(GiaTriThanhToan) FROM DotThanhToan 
    WHERE DuAnId = @DuAnId AND IsPaid = true AND NgayThanhToan >= @startOfPeriod AND NgayThanhToan <= @endOfPeriod
    ```
3.  **Lũy kế giải ngân (Cột 18)**:
    $$\text{Cột 18} = \text{Cột 16} + \text{Cột 17}$$

---

## 3. Hiện trạng trong mã nguồn & Khuyến nghị tối ưu

### Hiện trạng trong `ReportService.cs`:
Hệ thống hiện tại đang tính toán báo cáo trong file [ReportService.cs](file:///Users/ducanhle/Desktop/Quanly_Duan/backend/demo1/demo1/Services/Implements/ReportService.cs):
*   Sử dụng thuộc tính `milestone.CreatedAt` (ngày tạo bản ghi) thay vì `NgayThanhToan` (ngày giao dịch thực tế) để so khớp mốc thời gian kỳ báo cáo.
*   Mặc định gán cột Giải ngân bằng Khối lượng thực hiện (`gNganKyTruoc = kLuongKyTruoc` và `gNganTrongKy = kLuongTrongKy`), chưa tách biệt giữa việc nghiệm thu công việc và việc thực chi dòng tiền.

### Đề xuất cải tiến code:
Để chuẩn hóa báo cáo theo chuẩn tài chính, mã nguồn tính toán nên được cập nhật như sau:

```csharp
// Tính toán Khối lượng thực hiện & Giải ngân dựa trên ngày thanh toán thực tế và trạng thái thanh toán
foreach (var contract in projectContracts)
{
    if (contract.DotThanhToans != null)
    {
        foreach (var milestone in contract.DotThanhToans)
        {
            // Xác định ngày áp dụng (Ưu tiên ngày thực tế thanh toán/nghiệm thu)
            DateTime targetDate = milestone.NgayThanhToan ?? milestone.CreatedAt;

            // 1. Tính toán Khối lượng thực hiện (Nghiệm thu)
            if (targetDate < startOfPeriod)
            {
                performedKyTruocVnd += milestone.GiaTriThanhToan;
            }
            else if (targetDate <= endOfPeriod)
            {
                performedTrongKyVnd += milestone.GiaTriThanhToan;
            }

            // 2. Tính toán Giải ngân thực tế (Chỉ tính khi đợt thanh toán đã hoàn thành thực chi)
            if (milestone.IsPaid)
            {
                if (targetDate < startOfPeriod)
                {
                    disbursedKyTruocVnd += milestone.GiaTriThanhToan;
                }
                else if (targetDate <= endOfPeriod)
                {
                    disbursedTrongKyVnd += milestone.GiaTriThanhToan;
                }
            }
        }
    }
}
```
