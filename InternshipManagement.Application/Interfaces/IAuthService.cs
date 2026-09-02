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
        Task<IdentityResult> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task<IdentityResult> ChangeEmailAsync(int userId, ChangeEmailRequest request);
        Task<AccountPreferencesDto> GetAccountPreferencesAsync(int userId);
        Task<IdentityResult> UpdateAccountPreferencesAsync(int userId, UpdateAccountPreferencesRequest request);
        Task<IdentityResult> DeactivateAccountAsync(int userId);
    }
}