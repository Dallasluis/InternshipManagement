using InternshipManagement.Application.DTOs.Company;
using InternshipManagement.Application.DTOs.Documents;
using InternshipManagement.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    public interface ICompanyService
    {
        Task<CompanyProfileResponse> GetCompanyProfileAsync(int userId);
        Task<CompanyProfileResponse> UpdateCompanyProfileAsync(int userId, UpdateCompanyProfileRequest request);
        Task<bool> SubmitVerificationAsync(int userId, SubmitVerificationRequest request);
        Task<bool> ReviewVerificationAsync(int companyProfileId, int reviewerUserId, ReviewVerificationRequest request);
        Task<bool> SubmitVerificationAppealAsync(int userId, SubmitVerificationAppealRequest request);
        Task<bool> ReviewVerificationAppealAsync(int companyProfileId, int reviewerUserId, ReviewVerificationAppealRequest request);
        Task<IReadOnlyList<CompanyVerificationHistory>> GetVerificationHistoryAsync(int companyProfileId);
        Task<IReadOnlyList<PrivateDocumentResponse>> GetVerificationEvidenceAsync(int companyProfileId);
        Task<VerificationSlaResponse> GetVerificationSlaAsync();
        Task<int> UpdateAgedVerificationsAsync();
        Task<bool> UpdateSubscriptionStatusAsync(int companyProfileId, bool isSubscribed);
    }
}