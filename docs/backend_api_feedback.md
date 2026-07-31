# Đề Xuất Tối Ưu API (Frontend -> Backend)

Để tối ưu hóa hiệu năng, giảm dung lượng dữ liệu truyền tải (băng thông) và tránh tình trạng giật lag ở phía Client khi dữ liệu phình to, Frontend đề xuất Backend hỗ trợ điều chỉnh và bổ sung một số tính năng cho các API như sau:

---

## 1. API Danh Sách Dự Án (`GET /api/DuAn`)

*   **Vấn đề hiện tại:** Frontend đang phải gọi `pageSize: 1000` để lấy toàn bộ dự án về, sau đó tự dùng filter lọc lấy các dự án có `loaiDuAn === 2` (Dự án triển khai).
*   **Đề xuất:**
    *   Hỗ trợ thêm filter `loaiDuAn` (hoặc `type`) trực tiếp trong query params của API.
    *   **Ví dụ URL mong muốn:** `GET /api/DuAn?loaiDuAn=2&page=1&pageSize=20`

---

## 2. API Danh Sách Gói Thầu (`GET /api/GoiThau`) và Chi Tiết Gói Thầu (`GET /api/GoiThau/{id}`)

*   **Vấn đề hiện tại:** Mỗi khi hiển thị danh sách gói thầu hoặc vào chi tiết 1 gói thầu, Frontend đang phải tải **toàn bộ danh sách hợp đồng** (`GET /api/HopDong?pageSize=1000`) về client chỉ để lọc ra các hợp đồng thuộc gói thầu đó nhằm tính toán lũy kế **Tổng giá trị hợp đồng** (`totalContractValue`).
*   **Đề xuất (Chọn 1 trong 2 phương án):**
    *   **Phương án 1 (Khuyên dùng):** Backend tính toán sẵn trường này và trả về trực tiếp trong response của API Gói thầu.
        *   API danh sách: Trả thêm trường `tongGiaTriHopDong` cho từng item gói thầu.
        *   API chi tiết: Trả thêm trường `tongGiaTriHopDong` trong object chi tiết gói thầu.
    *   **Phương án 2:** Nếu không thể gộp, Backend hỗ trợ filter theo gói thầu hoặc dự án cho API hợp đồng:
        *   `GET /api/HopDong?goiThauId={goiThauId}` (chỉ lấy các hợp đồng của gói thầu cụ thể đó).
        *   `GET /api/HopDong?duAnId={duAnId}` (chỉ lấy các hợp đồng thuộc dự án cụ thể đó).

---

## 3. API Công Việc Theo Gói Thầu (`GET /api/CongViecGoiThau/...`)

*   **Vấn đề hiện tại:** Khi vào chi tiết gói thầu, hệ thống đang gọi song song 2 API:
    1.  `GET /api/CongViecGoiThau/by-goi-thau/{idGoiThau}` (để lấy danh sách công việc).
    2.  `GET /api/CongViecGoiThau/report/{idGoiThau}` (để lấy báo cáo số liệu hoàn thành/đang thực hiện).
*   **Đề xuất:**
    *   Nếu API `report` đã trả về danh sách công việc (`congViecs`), Frontend có thể chỉ cần gọi API `report` là đủ. Nhờ Backend xác nhận xem dữ liệu danh sách công việc ở 2 API này có đồng nhất không để thống nhất gộp lại hoặc tối giản luồng gọi từ Frontend.
