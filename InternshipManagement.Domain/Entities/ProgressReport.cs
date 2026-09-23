using InternshipManagement.Domain.Common;
using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Domain.Entities
{
    public class ProgressReport : BaseEntity
    {
        public int PlacementId { get; set; }
        public virtual Placement Placement { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string WorkCompleted { get; set; }
        public string? SkillsLearned { get; set; }
        public string? Challenges { get; set; }
        public string? Achievements { get; set; }
        public string? Comments { get; set; }
        public string? SupportingDocuments { get; set; }
        public ProgressReportStatus Status { get; set; } = ProgressReportStatus.Submitted;
        public string? CompanyFeedback { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedByUserId { get; set; }
    }
}
