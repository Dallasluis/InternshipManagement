using InternshipManagement.Web.Models.Auth;

namespace InternshipManagement.Web.Services
{
    public interface IAuthApiClient
    {
        Task<AuthApiResult> RegisterAsync(RegisterViewModel model);
        Task<AuthApiResult> LoginAsync(LoginViewModel model);
        Task<AuthApiResult> VerifyTwoFactorLoginAsync(string challengeToken, string code);
        Task<AuthApiResult> ResendTwoFactorLoginCodeAsync(string challengeToken);
        Task<AuthApiResult> CancelTwoFactorLoginAsync(string challengeToken);
        Task<AuthApiResult> ChangePasswordAsync(string token, ChangePasswordViewModel model);
        Task<AuthApiResult> ChangeEmailAsync(string token, ChangeEmailViewModel model);
        Task<AccountPreferencesViewModel?> GetPreferencesAsync(string token);
        Task<AuthApiResult> UpdatePreferencesAsync(string token, AccountPreferencesViewModel model);
        Task<AuthApiResult> DeactivateAsync(string token);
        Task<AuthApiResult> UpdateProfilePictureAsync(string token, string profilePictureUrl);
    }
}
