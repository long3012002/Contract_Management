using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces
{
    /// <summary>
    /// Service chuyên trách xử lý xác thực SSO OAuth2 / OIDC PKCE
    /// </summary>
    public interface ISsoService
    {
        /// <summary>
        /// Xử lý callback từ frontend: đổi code + code_verifier lấy token SSO,
        /// trích xuất thông tin người dùng (claim sub, email) và cấp phiên làm việc nội bộ.
        /// </summary>
        Task<AuthResult> HandleCallbackAsync(SsoCallbackRequest request);
    }
}
