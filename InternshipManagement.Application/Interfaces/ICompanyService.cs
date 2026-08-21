using InternshipManagement.Application.DTOs.Company;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    public interface ICompanyService
    {
        Task<CompanyProfileResponse> GetCompanyProfileAsync(int userId);
        Task<CompanyProfileResponse> UpdateCompanyProfileAsync(int userId, UpdateCompanyProfileRequest request);
        Task<bool> SubmitVerificationAsync(int userId, SubmitVerificationRequest request);
        Task<bool> ReviewVerificationAsync(int companyProfileId, ReviewVerificationRequest request);
        Task<bool> UpdateSubscriptionStatusAsync(int companyProfileId, bool isSubscribed);
    }
}