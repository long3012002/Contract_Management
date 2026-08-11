using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs
{
    /// <summary>
    /// Yêu cầu đăng nhập tài khoản.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Tên đăng nhập (Username)
        /// </summary>
        [Required]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Mật khẩu người dùng
        /// </summary>
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kết quả đăng nhập và thông tin Token xác thực.
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// Thông điệp kết quả
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Chuỗi JWT Access Token dùng cho các API có bảo mật Bearer
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// Chuỗi Refresh Token để cấp lại Access Token khi hết hạn
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Tên đăng nhập của tài khoản
        /// </summary>
        public string Username { get; set; } = string.Empty;

        public Guid? UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public bool IsSystemAdmin { get; set; }

        /// <summary>
        /// Yêu cầu người dùng cài đặt 2FA lần đầu
        /// </summary>
        public bool Require2FASetup { get; set; }

        /// <summary>
        /// Yêu cầu nhập mã xác thực OTP 2FA
        /// </summary>
        public bool Require2FAVerification { get; set; }

        /// <summary>
        /// Mã Secret Key cài đặt 2FA (nếu có)
        /// </summary>
        public string? TwoFactorSecret { get; set; }

        /// <summary>
        /// Đường dẫn ảnh QR Code để quét cài đặt Google Authenticator
        /// </summary>
        public string? QrCodeUrl { get; set; }
    }

    /// <summary>
    /// Yêu cầu xác thực mã OTP 2FA.
    /// </summary>
    public class Verify2FARequest
    {
        /// <summary>
        /// Tên đăng nhập
        /// </summary>
        [Required]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Mã OTP 6 chữ số từ ứng dụng Authenticator
        /// </summary>
        [Required]
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    /// Yêu cầu làm mới Access Token.
    /// </summary>
    public class RefreshRequest
    {
        /// <summary>
        /// Mã Refresh Token hợp lệ
        /// </summary>
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
