using InternshipManagement.Application.DTOs.Auth;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task<bool> LogoutAsync(int userId);
        Task<bool> VerifyEmailAsync(int userId, string token);
        Task<bool> RequestPasswordResetAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    }
}