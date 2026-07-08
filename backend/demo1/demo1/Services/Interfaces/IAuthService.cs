using System;
using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(LoginRequest request);
        Task<AuthResult> RefreshAsync(RefreshRequest request);
        Task<AuthResult> Enable2FaAsync(Verify2FARequest request, string authHeader);
        Task<AuthResult> Verify2FaAsync(Verify2FARequest request, string authHeader);
        Task<AuthResult> LogoutAsync(string username);
    }
}
