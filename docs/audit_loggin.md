# Yêu cầu xây dựng Audit Logging

## 1. Mục tiêu

Xây dựng chức năng Audit Logging cho hệ thống nhằm theo dõi và truy vết các hoạt động quan trọng của người dùng trên hệ thống.

Audit Log cần trả lời được các câu hỏi:

- Ai thực hiện?
- Thực hiện thao tác gì?
- Thực hiện trên đối tượng nào?
- Thời điểm nào?
- Dữ liệu nào đã thay đổi?
- Giá trị trước và sau thay đổi là gì?
- Có thể truy vết được request phát sinh thao tác hay không?

Backend tự quyết định kiến trúc, cách triển khai, database schema và cơ chế ghi log phù hợp với hệ thống hiện tại.

---

## 2. Phạm vi Audit

Audit các thao tác nghiệp vụ quan trọng, bao gồm tối thiểu:

### Dữ liệu nghiệp vụ

- Nghị quyết
- Dự án
- Gói thầu
- Hợp đồng
- Đối tác
- Thanh toán
- Nguồn vốn
- Điều chỉnh
- Tài liệu
- Các nghiệp vụ phát sinh sau này

### Thao tác

Tối thiểu cần theo dõi:

- Tạo mới
- Cập nhật
- Xóa
- Phê duyệt
- Từ chối
- Trình duyệt
- Hủy
- Khôi phục
- Upload
- Download
- Export
- Thay đổi phân quyền
- Các thao tác nghiệp vụ quan trọng khác

Backend tự xác định những action nào thực sự cần Audit dựa trên nghiệp vụ.

---

## 3. Thông tin cần Audit

Mỗi Audit Event cần xác định được:

### Người thực hiện

- User ID
- Họ tên
- Chức vụ
- Phòng/ban hoặc đơn vị

### Đối tượng

- Loại đối tượng
- ID đối tượng
- Tên/mã đối tượng để hiển thị

Ví dụ:

> Hợp đồng HD-2026-0012

### Thao tác

Ví dụ:

> Cập nhật / Phê duyệt / Xóa

### Thời gian

Ghi nhận chính xác thời điểm thao tác.

### Thông tin thay đổi

Đối với các thao tác làm thay đổi dữ liệu, cần xác định:

- Trường nào thay đổi
- Giá trị trước khi thay đổi
- Giá trị sau khi thay đổi

Ví dụ:

> Giá trị hợp đồng: 5 tỷ → 5,5 tỷ

Không yêu cầu frontend phải tự so sánh dữ liệu.

---

## 4. Truy vết request

Audit cần có khả năng liên kết ngược về request/API phát sinh thao tác để phục vụ việc điều tra và debug.

Tối thiểu cần có khả năng truy vết:

- Request ID / Correlation ID
- Thời gian
- API/endpoint
- HTTP method
- User
- IP hoặc thông tin client phù hợp

Backend tự quyết định thông tin kỹ thuật nào cần lưu thêm.

---

## 5. Tính chính xác

Audit phải phản ánh đúng trạng thái thực tế của hệ thống.

Ví dụ:

Nếu cập nhật hợp đồng thất bại hoặc transaction rollback thì không được tạo Audit Event thể hiện rằng cập nhật đã thành công.

Đối với các nghiệp vụ quan trọng, Audit và Business Operation phải đảm bảo tính nhất quán.

---

## 6. Dữ liệu nhạy cảm

Không được lưu các thông tin nhạy cảm hoặc bí mật vào Audit Log.

Ví dụ:

- Password
- Token
- Refresh Token
- OTP
- Secret
- Credential
- Các thông tin bảo mật khác

Backend cần chủ động xác định và xử lý các trường nhạy cảm.

---

## 7. Tính bất biến

Audit Log phải được xem là dữ liệu phục vụ truy vết.

Người dùng thông thường không được:

- Chỉnh sửa Audit Log
- Xóa Audit Log
- Làm thay đổi nội dung Audit Log

Nếu hệ thống có nhu cầu retention, archive hoặc cleanup thì phải có cơ chế quản trị riêng và không làm mất khả năng truy vết ngoài policy được phê duyệt.

---

## 8. API phục vụ Frontend

Backend cung cấp API để Frontend có thể:

### Xem danh sách Audit

Hỗ trợ:

- Pagination
- Tìm kiếm
- Lọc theo người thực hiện
- Lọc theo loại đối tượng
- Lọc theo thao tác
- Lọc theo khoảng thời gian
- Lọc theo đối tượng cụ thể

### Xem chi tiết Audit

Khi người dùng chọn một Audit Event, Frontend có thể xem:

- Người thực hiện
- Thao tác
- Đối tượng
- Thời gian
- Nội dung thay đổi
- Giá trị trước/sau
- Thông tin truy vết cần thiết

### Xem Audit của một đối tượng

Frontend cần có khả năng lấy lịch sử hoạt động của một đối tượng cụ thể.

Ví dụ:

> Lịch sử hoạt động của Hợp đồng HD-2026-0012.

Chức năng này sẽ được sử dụng trong màn hình chi tiết của:

- Dự án
- Gói thầu
- Hợp đồng
- Thanh toán
- Các entity phù hợp khác

---

## 9. Phân quyền

Audit Log phải được bảo vệ bằng authorization.

Người dùng chỉ được xem Audit Log phù hợp với quyền truy cập của mình.

Các thông tin kỹ thuật hoặc Audit toàn hệ thống có thể được giới hạn cho Admin hoặc nhóm người dùng có quyền phù hợp.

Backend tự thiết kế cơ chế permission/authorization phù hợp với hệ thống hiện tại.

---

## 10. Hiệu năng

Audit Logging không được làm ảnh hưởng đáng kể đến hiệu năng của các API nghiệp vụ.

Cần đảm bảo:

- Không tạo truy vấn không cần thiết.
- Có khả năng xử lý số lượng Audit Log lớn.
- API truy vấn Audit có pagination.
- Không trả về lượng dữ liệu quá lớn trong một request.
- Có khả năng mở rộng khi hệ thống phát sinh nhiều log.

Backend tự lựa chọn giải pháp tối ưu về database, indexing, asynchronous processing, queue/event... nếu cần.

---

## 11. Khả năng mở rộng

Audit Logging phải có tính generic và có thể áp dụng cho các module mới mà không phải xây dựng lại toàn bộ cơ chế.

Khi thêm một nghiệp vụ mới, việc tích hợp Audit nên đơn giản và thống nhất.

Không nên thiết kế Audit chỉ dành riêng cho Contract hoặc Project.

---

## 12. Acceptance Criteria

Chức năng được xem là đạt khi:

- [ ] Có thể xác định người thực hiện mỗi thao tác.
- [ ] Có thể xác định thao tác được thực hiện.
- [ ] Có thể xác định đối tượng bị tác động.
- [ ] Có timestamp chính xác.
- [ ] Có thể xem lịch sử thay đổi của dữ liệu.
- [ ] Với dữ liệu bị thay đổi, có thể biết giá trị trước và sau.
- [ ] Có thể truy vết request phát sinh thao tác.
- [ ] Không ghi dữ liệu nhạy cảm.
- [ ] Audit không bị ghi sai khi business transaction thất bại.
- [ ] Có API danh sách Audit.
- [ ] Có pagination và filter.
- [ ] Có API xem chi tiết Audit.
- [ ] Có API lấy Audit theo entity.
- [ ] Có authorization.
- [ ] Người dùng không thể tùy ý sửa/xóa Audit.
- [ ] Đáp ứng tốt khi số lượng Audit Log tăng lớn.
- [ ] Có thể mở rộng cho các module/nghiệp vụ mới.
- [ ] Có test cho các trường hợp Audit quan trọng.

---

## 13. Yêu cầu về thiết kế kỹ thuật

Không bắt buộc một implementation cụ thể.

Backend tự lựa chọn giải pháp phù hợp nhất với kiến trúc hiện tại, bao gồm nhưng không giới hạn:

- Database design
- Entity/model
- Service
- Middleware
- Interceptor
- Filter
- Attribute
- Domain/Application Event
- Queue
- Background processing
- Cơ chế transaction
- Index
- Partitioning
- Retention
- Caching

Mục tiêu cuối cùng là đảm bảo:

**Đúng dữ liệu → Truy vết được → An toàn → Hiệu năng tốt → Dễ mở rộng → Dễ bảo trì.**

Backend cần đề xuất phương án kỹ thuật trước khi triển khai nếu có nhiều lựa chọn kiến trúc khác nhau.