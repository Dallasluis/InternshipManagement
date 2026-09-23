using InternshipManagement.Domain.Common;
using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Domain.Entities
{
    public class Placement : BaseEntity
    {
        public int StudentProfileId { get; set; }
        public virtual StudentProfile StudentProfile { get; set; }

        public int CompanyProfileId { get; set; }
        public virtual CompanyProfile CompanyProfile { get; set; }

        public int InternshipId { get; set; }
        public virtual Internship Internship { get; set; }

        public int InternshipApplicationId { get; set; }
        public virtual InternshipApplication InternshipApplication { get; set; }

        public PlacementStatus Status { get; set; } = PlacementStatus.Pending;
        public DateTime OriginalStartDate { get; set; }
        public DateTime? OriginalEndDate { get; set; }
        public DateTime CurrentStartDate { get; set; }
        public DateTime? CurrentEndDate { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? TerminatedAt { get; set; }
        public string? TerminationReason { get; set; }

        public virtual ICollection<ProgressReport> ProgressReports { get; set; } = new List<ProgressReport>();
        public virtual ICollection<InternshipExtensionRequest> ExtensionRequests { get; set; } = new List<InternshipExtensionRequest>();
        public virtual ICollection<InternshipWithdrawalRequest> WithdrawalRequests { get; set; } = new List<InternshipWithdrawalRequest>();
        public virtual ICollection<InternshipEvaluation> Evaluations { get; set; } = new List<InternshipEvaluation>();
    }
}
