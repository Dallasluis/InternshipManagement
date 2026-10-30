namespace InternshipManagement.Web.Models.Internship
{
    public class SubmitInternshipReportRequest
    {
        public int InternshipId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
