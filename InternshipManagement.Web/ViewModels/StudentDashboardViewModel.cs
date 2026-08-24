using InternshipManagement.Web.Models.Student;
using InternshipManagement.Web.Models.Application;

namespace InternshipManagement.Web.ViewModels
{
    public class StudentDashboardViewModel
    {
        public StudentProfileResponse? Profile { get; set; }
        public List<ApplicationResponse> RecentApplications { get; set; } = new();
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int ShortlistedCount { get; set; }
        public int RejectedCount { get; set; }
        public int ProfileCompletion { get; set; }
    }
}