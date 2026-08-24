using InternshipManagement.Web.Models.Auth;

namespace InternshipManagement.Web.Services
{
    public interface IAuthApiClient
    {
        Task<AuthApiResult> RegisterAsync(RegisterViewModel model);
        Task<AuthApiResult> LoginAsync(LoginViewModel model);
    }
}