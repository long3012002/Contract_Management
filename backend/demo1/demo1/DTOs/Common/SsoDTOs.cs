using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Chuỗi state ngẫu nhiên truyền từ Frontend nhằm đối soát chống tấn công CSRF
        /// </summary>
        [Required]
        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;
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

    /// <summary>
    /// Dữ liệu UserInfo Endpoint trả về theo Phụ lục A.5 tài liệu SSO
    /// </summary>
    public class SsoUserInfoResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("sub")]
        public string? Sub { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("preferred_username")]
        public string? PreferredUsername { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("attributes")]
        public SsoUserAttributes? Attributes { get; set; }

        [JsonPropertyName("clientResources")]
        public List<SsoClientResourceGroup>? ClientResources { get; set; }

        [JsonPropertyName("clientScopes")]
        public List<SsoClientScopeGroup>? ClientScopes { get; set; }

        [JsonPropertyName("roleCodes")]
        public List<string>? RoleCodes { get; set; }
    }

    public class SsoUserAttributes
    {
        [JsonPropertyName("countLoginFailed")]
        public int? CountLoginFailed { get; set; }

        [JsonPropertyName("temporaryLock")]
        public bool? TemporaryLock { get; set; }

        [JsonPropertyName("lastChangePassword")]
        public string? LastChangePassword { get; set; }

        [JsonPropertyName("lastLogin")]
        public string? LastLogin { get; set; }

        [JsonPropertyName("gender")]
        public int? Gender { get; set; }

        [JsonPropertyName("emailAddress")]
        public string? EmailAddress { get; set; }
    }

    public class SsoClientResourceGroup
    {
        [JsonPropertyName("clientId")]
        public string? ClientId { get; set; }

        [JsonPropertyName("resources")]
        public List<SsoResourceItem>? Resources { get; set; }
    }

    public class SsoResourceItem
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("iconUri")]
        public string? IconUri { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("uris")]
        public List<string>? Uris { get; set; }

        [JsonPropertyName("children")]
        public List<SsoResourceItem>? Children { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
    }

    public class SsoClientScopeGroup
    {
        [JsonPropertyName("clientId")]
        public string? ClientId { get; set; }

        [JsonPropertyName("scopes")]
        public List<string>? Scopes { get; set; }
    }
}
