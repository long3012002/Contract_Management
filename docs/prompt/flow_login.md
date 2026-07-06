Thiết kế đầy đủ UI/UX cho luồng đăng nhập của một hệ thống web nội bộ doanh nghiệp có xác thực đa lớp (MFA/2FA) bằng OTP theo chuẩn TOTP, tương thích với Google Authenticator.

## 1. Bối cảnh dự án

Đây là hệ thống web nội bộ dùng để quản lý nghiệp vụ trong doanh nghiệp/cơ quan.

Nhóm người dùng gồm:

* Nhân viên
* Quản lý / Lãnh đạo
* Quản trị hệ thống

Mục tiêu:

* Đảm bảo bảo mật cao
* Trải nghiệm người dùng đơn giản, dễ hiểu
* Giảm thao tác rườm rà khi đăng nhập

Người dùng sẽ:

1. Đăng nhập bằng tài khoản nội bộ (username + password)
2. Sau khi xác thực thành công, nhập mã OTP từ ứng dụng Google Authenticator (hoặc ứng dụng xác thực khác)
3. Nếu là lần đầu đăng nhập thì phải thiết lập 2FA trước

---

## 2. Luồng nghiệp vụ tổng thể

Luồng đăng nhập:

* Người dùng mở trang login
* Nhập tài khoản và mật khẩu
* Hệ thống xác thực thông tin đăng nhập

Sau đó kiểm tra trạng thái MFA:

Nếu người dùng chưa kích hoạt MFA:
→ Chuyển sang luồng thiết lập 2FA lần đầu

Nếu người dùng đã kích hoạt MFA:
→ Chuyển sang màn hình nhập OTP

Sau khi OTP hợp lệ:
→ Điều hướng vào Dashboard

Hãy thể hiện rõ flow diagram cho toàn bộ luồng này.

---

## 3. Màn hình đăng nhập (Login Screen)

Thiết kế giao diện login hiện đại cho web desktop.

Thành phần bắt buộc:

* Logo công ty
* Input Username / Email
* Input Password
* Icon hiện/ẩn mật khẩu
* Checkbox Remember me
* Link Quên mật khẩu
* Nút Đăng nhập

State cần có:

* Normal state
* Loading state
* Error validation state
* Disabled state

Yêu cầu UX:

* Giao diện sạch sẽ
* Chuyên nghiệp
* Phù hợp hệ thống nội bộ doanh nghiệp
* Thông báo lỗi rõ ràng
*dựa vào system design frontend hiện tại

Ví dụ lỗi:

* Sai tài khoản hoặc mật khẩu
* Tài khoản bị khóa
* Không có quyền truy cập

---

## 4. Luồng thiết lập MFA lần đầu (First Login Setup)

Thiết kế dạng wizard nhiều bước.

### Bước 1: Giới thiệu bảo mật

Hiển thị màn hình giới thiệu:

“Nâng cao bảo mật cho tài khoản của bạn bằng xác thực 2 lớp (2FA).”

Nội dung:

* Giải thích MFA là gì
* Lợi ích bảo mật
* Vì sao bắt buộc bật 2FA

Có:

* Illustration
* Nút Tiếp tục

---

### Bước 2: Cài ứng dụng Authenticator

Hiển thị các ứng dụng được khuyến nghị:

* Google Authenticator
* Microsoft Authenticator
* Authy

Bao gồm:

* Icon ứng dụng
* Mô tả ngắn
* Hướng dẫn tải app

Có option:

* Tôi đã cài ứng dụng

---

### Bước 3: Quét QR Code & Thêm tài khoản

Thiết kế màn hình setup chứa:

* QR Code lớn ở trung tâm
* Secret key dự phòng (định dạng chia nhóm để dễ đọc, ví dụ: `XXXX XXXX XXXX XXXX`)
* Nút Copy secret key
* Tooltip hướng dẫn chi tiết cách thêm vào Google Authenticator

Hướng dẫn chi tiết tích hợp Google Authenticator:
1. Mở ứng dụng **Google Authenticator** trên thiết bị di động của bạn.
2. Nhấn vào biểu tượng dấu cộng **`+`** ở góc dưới cùng bên phải màn hình.
3. Chọn **Quét mã QR** (Scan a QR code) và hướng camera của điện thoại vào mã QR đang hiển thị trên màn hình máy tính.
4. *Trường hợp camera không hoạt động:* Chọn **Nhập khóa thiết lập** (Enter a setup key), nhập tên tài khoản (ví dụ: email/username của bạn) và dán (paste) chuỗi **Secret key dự phòng** vào phần "Khóa của bạn" (Your key), chọn loại khóa là "Dựa trên thời gian" (Time-based), sau đó nhấn **Thêm** (Add).

Yêu cầu UI:
* QR code hiển thị rõ ràng, nổi bật ở trung tâm.
* Hiển thị Secret key rõ ràng cùng nút "Sao chép" (Copy) tiện lợi.
* Hướng dẫn trực quan bằng các step nhỏ (hoặc hình vẽ mô phỏng nút `+` trong ứng dụng).

---

### Bước 4: Xác minh OTP

Người dùng nhập mã OTP 6 số từ app.

Thành phần:

* OTP input 6 số
* Auto focus
* Hỗ trợ paste
* Countdown timer 30 giây
* Nút Xác minh

State:

* OTP sai
* OTP hết hạn
* Loading verification

---

### Bước 5: Thiết lập thành công

Hiển thị màn hình thành công.

Bao gồm:

* Success illustration
* Thông báo bật 2FA thành công
* Nút vào Dashboard

Optional:

* Checkbox “Tin cậy thiết bị này trong 30 ngày”

---

## 5. Màn hình OTP cho các lần đăng nhập sau

Sau khi login thành công bằng password:

Hiển thị màn hình nhập OTP.

Thành phần:

* Account identifier (username ẩn bớt ký tự hoặc email nội bộ)
* OTP input 6 số
* Countdown timer
* Nút xác minh
* Link quay lại login

Ví dụ:
u***rname

State lỗi:

* OTP không đúng
* OTP hết hạn
* Quá số lần thử

---

## 6. Hỗ trợ sự cố (Mất điện thoại / Sự cố OTP)

* Cung cấp thông tin liên hệ/nút **"Liên hệ Quản trị viên hệ thống"** hoặc bộ phận IT Support ngay trên màn hình OTP nếu người dùng không thể lấy được mã OTP (mất điện thoại, cài lại máy).
* Không sử dụng mã khôi phục tự động (Recovery Code) để đảm bảo chính sách bảo mật nội bộ nghiêm ngặt, bắt buộc phải xác minh qua Quản trị viên để reset 2FA.

---

## 7. Security UX Requirements

Hệ thống cần hỗ trợ:

* Khóa tạm sau 5 lần nhập OTP sai
* Chống brute force
* Hiển thị cảnh báo bảo mật
* Trusted device (ghi nhớ thiết bị)
* Session timeout
* Thông báo khi đăng nhập bất thường

---

## 8. Design System

Thiết kế theo phong cách enterprise SaaS hiện đại.

Yêu cầu:

* Desktop-first
* Responsive
* Clean UI
* Minimal
* Professional

Sử dụng:

* Grid 8px
* Typography scale rõ ràng
* Contrast tốt
* Accessible design

Component cần có:

* Button
* Input
* OTP Input
* Card
* Alert
* Toast
* Stepper
* Modal

---

## 9. Deliverables

Hãy tạo đầy đủ:

1. User flow diagram
2. Wireframe
3. High-fidelity mockup
4. Component design system
5. Interaction notes
6. Edge case handling
7. UX rationale cho từng màn hình

Phong cách tham khảo:
Modern enterprise dashboard, tối giản, chuyên nghiệp, phù hợp hệ thống quản lý nội bộ quy mô lớn.
