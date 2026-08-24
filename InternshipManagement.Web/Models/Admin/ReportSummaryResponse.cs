namespace InternshipManagement.Web.Models.Admin
{
    public class ReportSummaryResponse
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string InternshipTitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}