namespace InternshipManagement.Web.Models.Admin
{
    public class CompanySummaryResponse
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string VerificationStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}