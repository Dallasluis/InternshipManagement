namespace InternshipManagement.Web.Models.Admin
{
    public class InternshipListResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ModerationStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ApplicationsCount { get; set; }
    }
}