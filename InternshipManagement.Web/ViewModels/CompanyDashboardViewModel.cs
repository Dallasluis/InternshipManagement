using InternshipManagement.Web.Models.Company;
using InternshipManagement.Web.Models.Internship;
using InternshipManagement.Web.Models.Application;

namespace InternshipManagement.Web.ViewModels
{
    public class CompanyDashboardViewModel
    {
        public CompanyProfileResponse? Profile { get; set; }
        public List<InternshipResponse> RecentInternships { get; set; } = new();
        public List<ApplicationResponse> RecentApplications { get; set; } = new();
        public int TotalInternships { get; set; }
        public int ActiveInternships { get; set; }
        public int TotalApplications { get; set; }
        public int ShortlistedCount { get; set; }
        public bool IsVerified { get; set; }
        public string SubscriptionStatus { get; set; } = string.Empty;
    }
}