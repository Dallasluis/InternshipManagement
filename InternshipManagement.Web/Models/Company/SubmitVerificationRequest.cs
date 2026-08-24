namespace InternshipManagement.Web.Models.Company
{
    public class SubmitVerificationRequest
    {
        public string VerificationDocuments { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}