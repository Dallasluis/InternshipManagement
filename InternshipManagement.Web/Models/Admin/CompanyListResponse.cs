namespace InternshipManagement.Web.Models.Admin
{
    public class CompanyListResponse
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string VerificationStatus { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public int InternshipCount { get; set; }
    }
}