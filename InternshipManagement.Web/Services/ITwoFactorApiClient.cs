using InternshipManagement.Web.Models.Auth;

namespace InternshipManagement.Web.Services
{
    public interface ITwoFactorApiClient
    {
        Task<TwoFactorSetupViewModel?> GetSetupAsync(string token);
        Task<AuthApiResult> EnableAsync(string token, string code);
    }
}
