using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Message { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool Require2FASetup { get; set; }
        public bool Require2FAVerification { get; set; }
        public string? TwoFactorSecret { get; set; }
        public string? QrCodeUrl { get; set; }
    }

    public class Verify2FARequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }

    public class RefreshRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
