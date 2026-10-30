using InternshipManagement.Application.DTOs.Admin;
using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Application.Interfaces
{
    public interface IAdminService
    {
        Task<AdminStatsResponse> GetStatsAsync();

        Task<List<CompanyListResponse>> GetAllCompaniesAsync();
        Task<List<CompanySummaryResponse>> GetRecentCompaniesAsync(int count);

        Task<List<InternshipListResponse>> GetAllInternshipsAsync();

        Task<List<ReportListResponse>> GetAllReportsAsync();
        Task<List<ReportSummaryResponse>> GetRecentReportsAsync(int count);
        Task<bool> ResolveReportAsync(int reportId, string response, ReportStatus status, bool fraudUpheld, int adminUserId);

        Task<List<UserListResponse>> GetAllUsersAsync();
        Task<bool> SuspendUserAsync(int userId, bool suspend, int adminUserId);
    }
}
