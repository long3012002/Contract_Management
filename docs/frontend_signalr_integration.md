# Hướng dẫn tích hợp SignalR Realtime Notifications (Dành cho Frontend Developer)

Tài liệu này hướng dẫn cách kết nối và nhận thông báo thời gian thực từ Backend sử dụng SignalR Hub.

---

## 1. Cài đặt thư viện
Cài đặt thư viện SignalR chính thức từ Microsoft:
```bash
npm install @microsoft/signalr
```

---

## 2. Thông tin Hub Endpoint
* **URL**: `/hub/notifications` (Ví dụ chạy local: `http://localhost:64950/hub/notifications` hoặc cổng backend đang lắng nghe).
* **Cơ chế xác thực (Authentication)**: Yêu cầu JWT token. Do giao thức WebSocket của trình duyệt không hỗ trợ gửi Custom Headers khi bắt đầu kết nối (handshake), token phải được truyền qua tham số truy vấn (Query String) với tên là `access_token`.

---

## 3. Sự kiện kết nối (Events)
Khi kết nối thành công, Hub sẽ đẩy thông báo trực tiếp xuống client qua sự kiện:
* **Event Name**: `ReceiveNotification`
* **Dữ liệu nhận được (Payload)**: Một Object JSON có cấu trúc như sau:
  ```json
  {
    "id": "guid-thong-bao",
    "title": "Cảnh báo: Hợp đồng sắp hết hạn",
    "content": "Hợp đồng 'Hợp đồng A' (Mã: HD01) sẽ hết hạn vào ngày 15/07/2026 (còn lại 2 ngày).",
    "link": "/contracts/guid-hop-dong",
    "isRead": false,
    "createdAt": "2026-07-13T09:30:00Z"
  }
  ```

---

## 4. Code mẫu tích hợp trong React (Custom Hook / Context)

Bạn có thể viết một Context hoặc Hook để quản lý vòng đời kết nối SignalR xuyên suốt ứng dụng.

```javascript
import React, { createContext, useContext, useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { toast } from 'react-hot-toast'; // Hoặc bất kỳ thư viện hiển thị Toast nào bạn dùng

const NotificationContext = createContext(null);

export const useNotifications = () => useContext(NotificationContext);

export const NotificationProvider = ({ children }) => {
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [connection, setConnection] = useState(null);

  // Hàm lấy danh sách thông báo ban đầu qua API
  const fetchInitialNotifications = async () => {
    try {
      const token = localStorage.getItem('token');
      const response = await fetch('http://localhost:64950/api/notification', {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (response.ok) {
        const data = await response.json();
        setNotifications(data);
        setUnreadCount(data.filter(n => !n.isRead).length);
      }
    } catch (error) {
      console.error('Lỗi khi fetch thông báo:', error);
    }
  };

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (!token) return;

    // 1. Khởi tạo danh sách thông báo ban đầu
    fetchInitialNotifications();

    // 2. Khởi tạo kết nối SignalR Hub
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:64950/hub/notifications', {
        // Truyền access_token cho quá trình xác thực JWT
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect() // Tự động kết nối lại khi mất mạng
      .configureLogging(signalR.LogLevel.Information)
      .build();

    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (!connection) return;

    // 3. Khởi động kết nối
    connection.start()
      .then(() => {
        console.log('Đã kết nối thành công tới SignalR Notification Hub!');

        // 4. Đăng ký lắng nghe sự kiện từ Server đẩy xuống
        connection.on('ReceiveNotification', (newNotification) => {
          console.log('Nhận thông báo realtime:', newNotification);

          // Cập nhật State danh sách thông báo
          setNotifications(prev => [newNotification, ...prev]);
          setUnreadCount(prev => prev + 1);

          // Hiển thị Popup Toast nhanh cho người dùng trên màn hình
          toast.success(newNotification.content, {
            duration: 5000,
            position: 'top-right'
          });
        });
      })
      .catch(error => console.error('Lỗi kết nối SignalR Hub:', error));

    // Cleanup khi component unmount hoặc connection thay đổi
    return () => {
      if (connection) {
        connection.off('ReceiveNotification');
        connection.stop();
      }
    };
  }, [connection]);

  return (
    <NotificationContext.Provider value={{ notifications, unreadCount, setNotifications, setUnreadCount }}>
      {children}
    </NotificationContext.Provider>
  );
};
```

---

## 5. Các API phụ trợ
Lập trình viên Frontend có thể sử dụng các HTTP API HTTP sau để thao tác với thông báo:
1. **Lấy danh sách thông báo của User đang đăng nhập**:
   * `GET /api/notification`
2. **Đánh dấu 1 thông báo đã đọc**:
   * `PUT /api/notification/{id}/read`
3. **Đánh dấu tất cả thông báo đã đọc**:
   * `PUT /api/notification/read-all`
