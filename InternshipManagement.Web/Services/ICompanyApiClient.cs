using InternshipManagement.Web.Models.Company;

namespace InternshipManagement.Web.Services
{
    public interface ICompanyApiClient
    {
        Task<CompanyProfileResponse?> GetProfileAsync(string token, int userId);
        Task<CompanyProfileResponse?> UpdateProfileAsync(string token, int userId, UpdateCompanyProfileRequest request);
        Task<bool> SubmitVerificationAsync(string token, int userId, SubmitVerificationRequest request);
    }
}