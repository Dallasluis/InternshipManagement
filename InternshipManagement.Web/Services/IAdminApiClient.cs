using InternshipManagement.Web.Models.Admin;

namespace InternshipManagement.Web.Services
{
    public interface IAdminApiClient
    {
        Task<AdminStatsResponse?> GetStatsAsync(string token);
        Task<List<CompanyListResponse>> GetAllCompaniesAsync(string token);
        Task<List<CompanySummaryResponse>> GetRecentCompaniesAsync(string token, int count);
        Task<bool> ReviewVerificationAsync(
            string token,
            int companyId,
            string status,
            string reasonCode,
            string notes,
            Dictionary<string, bool> criteriaAssessment,
            bool isAppeal = false);
        Task<List<CompanyVerificationHistoryResponse>> GetVerificationHistoryAsync(string token, int companyId);
        Task<List<InternshipManagement.Web.Models.Documents.PrivateDocumentResponse>> GetVerificationEvidenceAsync(string token, int companyId);
        Task<List<InternshipListResponse>> GetAllInternshipsAsync(string token);
        Task<bool> ModerateInternshipAsync(string token, int id, string status, string? notes);
        Task<List<ReportListResponse>> GetAllReportsAsync(string token);
        Task<List<ReportSummaryResponse>> GetRecentReportsAsync(string token, int count);
        Task<bool> ResolveReportAsync(
            string token,
            int id,
            string response,
            bool resolved,
            string? status = null,
            bool fraudUpheld = false);
        Task<List<UserListResponse>> GetAllUsersAsync(string token);
        Task<bool> SuspendUserAsync(string token, int id, bool suspend);
    }
}