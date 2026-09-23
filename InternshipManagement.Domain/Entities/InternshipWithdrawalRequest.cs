using InternshipManagement.Domain.Common;
using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Domain.Entities
{
    public class InternshipWithdrawalRequest : BaseEntity
    {
        public int PlacementId { get; set; }
        public virtual Placement Placement { get; set; }
        public string Reason { get; set; }
        public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Requested;
        public int RequestedByUserId { get; set; }
        public DateTime? EffectiveTerminationDate { get; set; }
        public string? CompanyDecisionNotes { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedByUserId { get; set; }
        public string? AdminDecisionNotes { get; set; }
    }
}
