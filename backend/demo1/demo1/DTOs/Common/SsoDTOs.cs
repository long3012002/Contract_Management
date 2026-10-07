using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace demo1.DTOs
{
    /// <summary>
    /// Dữ liệu gửi lên từ Frontend sau khi nhận Authorization Code từ SSO
    /// </summary>
    public class SsoCallbackRequest
    {
        /// <summary>
        /// Mã Authorization Code từ SSO Server
        /// </summary>
        [Required]
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Chuỗi code_verifier đã sinh tại Frontend (chuẩn PKCE S256)
        /// </summary>
        [Required]
        [JsonPropertyName("code_verifier")]
        public string CodeVerifier { get; set; } = string.Empty;

        /// <summary>
        /// Redirect URI đã đăng ký với SSO (phải khớp chính xác với lúc khởi tạo auth)
        /// </summary>
        [Required]
        [JsonPropertyName("redirect_uri")]
        public string RedirectUri { get; set; } = string.Empty;
    }

    /// <summary>
    /// Phản hồi nhận được từ Token Endpoint của SSO Server
    /// </summary>
    public class SsoTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }
}
