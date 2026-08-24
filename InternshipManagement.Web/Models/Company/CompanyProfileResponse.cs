namespace InternshipManagement.Web.Models.Company
{
    public class CompanyProfileResponse
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Industry { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? LogoUrl { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PhoneNumber { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;
        public bool IsSubscribed { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public string SubscriptionStatus { get; set; } = string.Empty;  
        public int ActiveInternships { get; set; }
        public int TotalInternships { get; set; }
        public int TotalApplications { get; set; }
    }
}