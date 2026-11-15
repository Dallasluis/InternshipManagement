using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Lifecycle
{
    public class DisputeWithdrawalRequest
    {
        [Required, StringLength(1000, MinimumLength = 5)]
        public string Reason { get; set; } = string.Empty;
    }

    public class ResolveWithdrawalDisputeRequest
    {
        /// <summary>True ends the placement as the intern asked; false keeps the company's decision.</summary>
        public bool Approved { get; set; }
        public DateTime? EffectiveTerminationDate { get; set; }
        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public class DisputedWithdrawalResponse
    {
        public int Id { get; set; }
        public int PlacementId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string InternshipTitle { get; set; } = string.Empty;
        public string PlacementStatus { get; set; } = string.Empty;
        public string StudentReason { get; set; } = string.Empty;
        public string? CompanyDecisionNotes { get; set; }
        public string? DisputeReason { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? DisputedAt { get; set; }
    }
}
