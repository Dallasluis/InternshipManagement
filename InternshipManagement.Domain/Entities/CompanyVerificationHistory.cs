using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Domain.Entities
{
    public class CompanyVerificationHistory
    {
        public long Id { get; set; }
        public int CompanyProfileId { get; set; }
        public CompanyVerificationStatus Status { get; set; }
        public string? ReasonCode { get; set; }
        public string? Notes { get; set; }
        public string? CriteriaAssessmentJson { get; set; }
        public string? EvidenceDocumentIdsJson { get; set; }
        public int? ChangedByUserId { get; set; }
        public bool IsAppeal { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
