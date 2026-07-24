# Bảng Cấu Hình Máy Chủ Windows Server (Tối Giản - Không Dùng File Server Riêng)

Dưới đây là bảng yêu cầu cấu hình máy chủ đã được tối giản hóa bằng cách **loại bỏ máy chủ File Server chuyên dụng** và cấu hình dung lượng ổ cứng máy chủ ứng dụng chính (App Server) ở mức tối giản **50 GB** theo yêu cầu của bạn.

---

| STT | HostName *(KT2-NHHT điền)* | IP *(KT3-NHHT điền)* | Zone | Cấu hình server | OS | Function description *(Mô tả chức năng máy chủ, app/services chạy trên máy)* | Note |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **\*** | `COOPBANK-QLDA-DMZ` | *[IP WAN / IP LAN]* | **DMZ-ZONE** | 4 Cores / 8 GB RAM / HDD 100 GB | Windows Server 2022 | Cấu hình Reverse Proxy sử dụng **IIS ARR (Application Request Routing)** hoặc Nginx để tiếp nhận các yêu cầu HTTPS của người dùng và chuyển tiếp an toàn vào máy chủ IIS vùng APP. | Mở cổng 80/443 để tiếp nhận kết nối HTTPS từ Client. Cấu hình chứng chỉ SSL (`hopdong.coopbank.vn`). |
| **1** | `COOPBANK-QLDA-APP` | *[IP LAN]* | **APP-ZONE** | 8 Cores / 16 GB RAM / **HDD 50 GB** | Windows Server 2022 | Máy chủ ứng dụng chính (App Server) cài đặt **IIS**: <br>- Host ứng dụng **Backend API (.NET 8/9)** và **Frontend (React)**.<br>- **Lưu trữ trực tiếp** các tệp tin quét hợp đồng bản mềm (PDF, Docx) tải lên tại ổ đĩa cục bộ (ví dụ: `C:\qlda\uploads`). | Cài đặt **.NET ASP.NET Core Hosting Bundle** bản mới nhất. Mở cổng 80/443 nhận yêu cầu từ Proxy vùng DMZ. |
| **2** | `COOPBANK-QLDA-DB` | `10.225.11.201` | **DB-ZONE** | 8 Cores / 32 GB RAM / SSD Min 300 GB | Windows Server 2022 | Máy chủ cơ sở dữ liệu chuyên dụng chạy **PostgreSQL 15/16 cho Windows**: <br>- Lưu trữ dữ liệu nghiệp vụ quan hệ (Hợp đồng, Dự án, Đợt thanh toán...).<br>- Lưu trữ lịch sử hệ thống (Audit Logs), thông báo và hàng đợi Job Queue của Hangfire. | **Mở cổng 5432** (cổng mặc định của PostgreSQL). Chỉ cho phép kết nối từ IP của máy chủ App Server `COOPBANK-QLDA-APP` để đảm bảo bảo mật dữ liệu tối đa. |

---

## 2. Các Đặc Điểm Kỹ Thuật Khi Triển Khai Tối Giản

1. **Lưu Trữ File Cục Bộ (Local Storage)**:
   - Vì không sử dụng máy chủ File Server riêng, toàn bộ tệp hợp đồng tải lên sẽ được lưu trực tiếp trên phân vùng ổ đĩa của máy chủ `COOPBANK-QLDA-APP`.
   - Cấu hình thư mục lưu trữ trong file `appsettings.Production.json` trỏ về đường dẫn tuyệt đối trên Windows (ví dụ: `C:\qlda\uploads`).
2. **IIS Application Pool**:
   - Cấu hình IIS App Pool chạy dưới quyền tài khoản có đủ đặc quyền đọc/ghi trên thư mục lưu trữ cục bộ (`C:\qlda\uploads`).
   - Đặt `Start Mode = AlwaysRunning` và `Idle Time-out = 0` trên App Pool để Hangfire chạy ngầm hoạt động ổn định.
3. **Kết nối mạng & Tích hợp**:
   - App Server (`COOPBANK-QLDA-APP`) kết nối trực tiếp đến Database Server (`10.225.11.201` cổng `5432`).
   - Mở firewall đi ra (Outgoing) từ App Server tới RADIUS Server (`10.224.0.94` cổng `1812 UDP`) và SMTP Mail Server (`smtp.coopbank.vn` cổng `587 TCP`).
