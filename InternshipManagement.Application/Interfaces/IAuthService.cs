using InternshipManagement.Application.DTOs.Auth;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> CompleteTwoFactorLoginAsync(int userId, string challengeId, string code, bool rememberMe, string? ipAddress);
        Task<AuthResponse> ResendTwoFactorLoginCodeAsync(int userId, string challengeId);
        Task<bool> CancelTwoFactorLoginAsync(int userId, string challengeId);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task<bool> LogoutAsync(int userId);
        Task<bool> VerifyEmailAsync(int userId, string token);
        Task<(bool Success, string? Error)> ResendVerificationAsync(int userId, string? ipAddress);
        Task<bool> ConfirmEmailChangeAsync(int userId, string token, string? ipAddress);
        Task<bool> CancelEmailChangeAsync(int userId, string token, string? ipAddress);
        Task RunEmailVerificationHousekeepingAsync(CancellationToken cancellationToken);
        Task<AuthResponse> SendSecurityCodeAsync(int userId);
        Task<bool> RequestPasswordResetAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
        Task<IdentityResult> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task<IdentityResult> ChangeEmailAsync(int userId, ChangeEmailRequest request);
        Task<IdentityResult> UpdateProfilePictureUrlAsync(int userId, string profilePictureUrl);
        Task<AccountPreferencesDto> GetAccountPreferencesAsync(int userId);
        Task<IdentityResult> UpdateAccountPreferencesAsync(int userId, UpdateAccountPreferencesRequest request);
        Task<IdentityResult> DeactivateAccountAsync(int userId);
    }
}
