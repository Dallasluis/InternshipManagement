using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.Models.Lifecycle
{
    public class PlacementResponse
    {
        public int Id { get; set; }
        public int StudentProfileId { get; set; }
        public int CompanyProfileId { get; set; }
        public int InternshipId { get; set; }
        public int InternshipApplicationId { get; set; }
        public string InternshipTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime OriginalStartDate { get; set; }
        public DateTime? OriginalEndDate { get; set; }
        public DateTime CurrentStartDate { get; set; }
        public DateTime? CurrentEndDate { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? TerminatedAt { get; set; }
        public string? TerminationReason { get; set; }
    }

    public class ApplicationStatusHistoryResponse
    {
        public int Id { get; set; }
        public int InternshipApplicationId { get; set; }
        public string PreviousStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public int? ChangedByUserId { get; set; }
        public string? Notes { get; set; }
    }

    public class SubmitProgressReportRequest
    {
        [Required]
        public DateTime PeriodStart { get; set; }

        [Required]
        public DateTime PeriodEnd { get; set; }

        [Required]
        public string WorkCompleted { get; set; } = string.Empty;

        public string? SkillsLearned { get; set; }
        public string? Challenges { get; set; }
        public string? Achievements { get; set; }
        public string? Comments { get; set; }
        public List<string>? SupportingDocumentUrls { get; set; }
    }

    public class ProgressReportResponse
    {
        public int Id { get; set; }
        public int PlacementId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string WorkCompleted { get; set; } = string.Empty;
        public string? SkillsLearned { get; set; }
        public string? Challenges { get; set; }
        public string? Achievements { get; set; }
        public string? Comments { get; set; }
        public List<string> SupportingDocumentUrls { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public string? CompanyFeedback { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class ReviewProgressReportRequest
    {
        [Required]
        public string Feedback { get; set; } = string.Empty;
    }

    public class ProposeExtensionRequest
    {
        [Required]
        public DateTime ProposedEndDate { get; set; }
        public string? Reason { get; set; }
    }

    public class RespondToExtensionRequest
    {
        public bool Accepted { get; set; }
        public string? Notes { get; set; }
    }

    public class RequestWithdrawalRequest
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
    }

    public class ReviewWithdrawalRequest
    {
        public bool Approved { get; set; }
        public DateTime? EffectiveTerminationDate { get; set; }
        public string? Notes { get; set; }
    }

    public class CompletePlacementRequest
    {
        public string? Notes { get; set; }
    }

    public class TerminatePlacementRequest
    {
        public DateTime? EffectiveTerminationDate { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;
    }

    public class SubmitEvaluationRequest
    {
        [Range(1, 5)]
        public int TechnicalSkillsRating { get; set; }

        [Range(1, 5)]
        public int CommunicationRating { get; set; }

        [Range(1, 5)]
        public int TeamworkRating { get; set; }

        [Range(1, 5)]
        public int ProblemSolvingRating { get; set; }

        [Range(1, 5)]
        public int ProfessionalismRating { get; set; }

        [Range(1, 5)]
        public int ReliabilityRating { get; set; }

        [Range(1, 5)]
        public int OverallPerformanceRating { get; set; }

        public string? Comments { get; set; }
    }

    public class EvaluationResponse
    {
        public int Id { get; set; }
        public int PlacementId { get; set; }
        public int EvaluatorUserId { get; set; }
        public int TechnicalSkillsRating { get; set; }
        public int CommunicationRating { get; set; }
        public int TeamworkRating { get; set; }
        public int ProblemSolvingRating { get; set; }
        public int ProfessionalismRating { get; set; }
        public int ReliabilityRating { get; set; }
        public int OverallPerformanceRating { get; set; }
        public string? Comments { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}
