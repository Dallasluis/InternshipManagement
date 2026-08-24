namespace InternshipManagement.Web.Models.Admin
{
    public class ReportListResponse
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string InternshipTitle { get; set; } = string.Empty;
        public string ReporterName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? AdminResponse { get; set; }
    }
}