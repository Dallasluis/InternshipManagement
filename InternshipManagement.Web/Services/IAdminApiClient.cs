using InternshipManagement.Web.Models.Admin;

namespace InternshipManagement.Web.Services
{
    public interface IAdminApiClient
    {
        Task<AdminStatsResponse?> GetStatsAsync(string token);
        Task<List<CompanyListResponse>> GetAllCompaniesAsync(string token);
        Task<List<CompanySummaryResponse>> GetRecentCompaniesAsync(string token, int count);
        Task<bool> ReviewVerificationAsync(string token, int companyId, bool approved, string? notes);
        Task<List<InternshipListResponse>> GetAllInternshipsAsync(string token);
        Task<bool> ModerateInternshipAsync(string token, int id, string status, string? notes);
        Task<List<ReportListResponse>> GetAllReportsAsync(string token);
        Task<List<ReportSummaryResponse>> GetRecentReportsAsync(string token, int count);
        Task<bool> ResolveReportAsync(string token, int id, string response, bool resolved);
        Task<List<UserListResponse>> GetAllUsersAsync(string token);
        Task<bool> SuspendUserAsync(string token, int id, bool suspend);
    }
}