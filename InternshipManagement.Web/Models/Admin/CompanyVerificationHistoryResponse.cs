namespace InternshipManagement.Web.Models.Admin
{
    public sealed class CompanyVerificationHistoryResponse
    {
        public string Status { get; set; } = string.Empty;
        public string? ReasonCode { get; set; }
        public string? Notes { get; set; }
        public string? CriteriaAssessmentJson { get; set; }
        public int? ChangedByUserId { get; set; }
        public bool IsAppeal { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
