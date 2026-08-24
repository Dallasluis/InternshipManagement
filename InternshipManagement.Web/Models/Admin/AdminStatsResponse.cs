namespace InternshipManagement.Web.Models.Admin
{
    public class AdminStatsResponse
    {
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalCompanies { get; set; }
        public int TotalInternships { get; set; }
        public int ActiveInternships { get; set; }
        public int TotalApplications { get; set; }
        public int PendingReviews { get; set; }
        public int PendingVerifications { get; set; }
        public int PendingReports { get; set; }
        public List<MonthlyStatDto>? MonthlyTrend { get; set; }
    }

    public class MonthlyStatDto
    {
        public string Month { get; set; } = string.Empty;
        public int Internships { get; set; }
        public int Applications { get; set; }
        public int Users { get; set; }
    }
}