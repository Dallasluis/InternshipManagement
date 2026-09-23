using InternshipManagement.Domain.Common;
using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Domain.Entities
{
    public class InternshipExtensionRequest : BaseEntity
    {
        public int PlacementId { get; set; }
        public virtual Placement Placement { get; set; }
        public DateTime ProposedEndDate { get; set; }
        public string? Reason { get; set; }
        public ExtensionStatus Status { get; set; } = ExtensionStatus.Proposed;
        public int ProposedByUserId { get; set; }
        public DateTime? RespondedAt { get; set; }
        public int? RespondedByUserId { get; set; }
        public string? ResponseNotes { get; set; }
    }
}
