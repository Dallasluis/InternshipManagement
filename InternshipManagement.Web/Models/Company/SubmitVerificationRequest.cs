namespace InternshipManagement.Web.Models.Company
{
    public class SubmitVerificationRequest
    {
        public List<Guid> EvidenceDocumentIds { get; set; } = new();
        public string? Notes { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public List<IFormFile> EvidenceFiles { get; set; } = new();
    }
}