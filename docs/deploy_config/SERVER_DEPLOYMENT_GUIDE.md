# Tài Liệu Hướng Dẫn Cấu Hình & Triển Khai Server (CoopBank)

Tài liệu này tổng hợp toàn bộ các cấu hình, tệp thiết lập và quy trình triển khai ứng dụng **Quản lý Dự án & Hợp đồng CNTT** (Quy mô ~10 người sử dụng nội bộ) trên máy chủ Production của Ngân hàng Hợp tác xã Việt Nam (CoopBank).

---

## 1. Danh Mục Các Tệp Cấu Hình Server Đã Chuẩn Bị

Tất cả các file cấu hình server được lưu trữ tại thư mục `docs/deploy_config/`:

| Tệp Cấu Hình | Đường Dẫn | Mô Tả |
| :--- | :--- | :--- |
| **Nginx Reverse Proxy** | [`nginx.conf`](file:///c:/Users/mdqa7/Documents/CoopBank_Project/Contract_Management/docs/deploy_config/nginx.conf) | Cấu hình Nginx Proxy, SSL, WebSockets SignalR, SPA Fallback, Gzip |
| **Docker Compose Prod** | [`docker-compose.prod.yml`](file:///c:/Users/mdqa7/Documents/CoopBank_Project/Contract_Management/docs/deploy_config/docker-compose.prod.yml) | File điều phối Container Production (Backend API + Nginx + Mount logs) |
| **AppSettings Prod** | [`appsettings.Production.json`](file:///c:/Users/mdqa7/Documents/CoopBank_Project/Contract_Management/docs/deploy_config/appsettings.Production.json) | File cấu hình ASP.NET Core Production (Tắt AutoMigrate, ConnString, JWT) |
| **Script Cài Đặt Server** | [`setup_server.sh`](file:///c:/Users/mdqa7/Documents/CoopBank_Project/Contract_Management/docs/deploy_config/setup_server.sh) | Script bash tự động khởi tạo Server, Firewall UFW, Docker & Thư mục |

---

## 2. Thông Số Hạ Tầng Tối Ưu (Quy Mô ~10 Người Dùng Nội Bộ)

### 2.1. Phân Vùng Máy Chủ
- **Web / Application Server**:
  - **OS**: Ubuntu Server 22.04 LTS / RHEL 9 hoặc Windows Server
  - **Cấu hình phần cứng tối ưu**: **2 vCPU, 4 GB - 8 GB RAM, 40 GB SSD**
  - **Role**: Host Docker Container (Nginx Reverse Proxy + ASP.NET Core Backend API)
- **Database Server**:
  - **IP**: `10.225.11.201` (Port `5432`)
  - **Engine**: PostgreSQL 15/16
  - **Cấu hình phần cứng tối ưu**: **2 - 4 vCPU, 4 GB - 8 GB RAM, 50 GB SSD**
- **Tích hợp Hạ tầng Ngân hàng**:
  - **RADIUS Active Directory**: `10.224.0.94` (Port UDP `1812`)
  - **SMTP Email Server**: `smtp.coopbank.vn` (Port TCP `587`)

### 2.2. Ma Trận Firewall (UFW / Network Access Control)
Chỉ cho phép mở các cổng kết nối cần thiết theo quy định An toàn thông tin:
- `TCP 443`: Tiếp nhận kết nối HTTPS từ Client Browser.
- `TCP 5432`: Kết nối từ App Server đến Database Server (`10.225.11.201`).
- `UDP 1812`: Kết nối từ App Server đến RADIUS Server (`10.224.0.94`).
- `TCP 587`: Kết nối từ App Server đến Mail Server (`smtp.coopbank.vn`).

---

## 3. Các Bước Triển Khai Chi Tiết Trên Server

### Bước 1: Khởi tạo Máy chủ Web (App Server)
Chạy script cài đặt môi trường trên server Linux vừa khởi tạo:
```bash
chmod +x docs/deploy_config/setup_server.sh
./docs/deploy_config/setup_server.sh
```

### Bước 2: Đặt Chứng chỉ SSL & Tệp Build Frontend
1. Copy tệp SSL Certificate của CoopBank vào server:
   - Certificate: `/etc/ssl/coopbank/hopdong.coopbank.vn.crt`
   - Private Key: `/etc/ssl/coopbank/hopdong.coopbank.vn.key`
2. Build Frontend React SPA và copy toàn bộ thư mục `dist` vào vị trí quy định.

### Bước 3: Cấu hình Biến Môi trường & Khởi chạy Container
1. Kiểm tra lại thông số trong [`appsettings.Production.json`](file:///c:/Users/mdqa7/Documents/CoopBank_Project/Contract_Management/docs/deploy_config/appsettings.Production.json) và [`docker-compose.prod.yml`](file:///c:/Users/mdqa7/Documents/CoopBank_Project/Contract_Management/docs/deploy_config/docker-compose.prod.yml).
2. Khởi chạy ứng dụng:
   ```bash
   cd docs/deploy_config
   docker compose -f docker-compose.prod.yml up -d --build
   ```

### Bước 4: Kiểm Tra Trạng Thái & Log Hệ Thống
- Kiểm tra container đang chạy: `docker ps`
- Xem log Backend real-time: `docker logs -f qlda-backend-container`
- Xem log Nginx access/error: `tail -f /var/log/nginx/access_log.log`
