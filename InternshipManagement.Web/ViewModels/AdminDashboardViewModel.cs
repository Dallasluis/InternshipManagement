using InternshipManagement.Web.Models.Admin;

namespace InternshipManagement.Web.ViewModels
{
    public class AdminDashboardViewModel
    {
        public AdminStatsResponse? Stats { get; set; }
        public List<CompanySummaryResponse> RecentCompanies { get; set; } = new();
        public List<ReportSummaryResponse> RecentReports { get; set; } = new();
    }
}