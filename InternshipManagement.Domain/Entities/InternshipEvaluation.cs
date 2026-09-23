using InternshipManagement.Domain.Common;

namespace InternshipManagement.Domain.Entities
{
    public class InternshipEvaluation : BaseEntity
    {
        public int PlacementId { get; set; }
        public virtual Placement Placement { get; set; }
        public int EvaluatorUserId { get; set; }
        public int TechnicalSkillsRating { get; set; }
        public int CommunicationRating { get; set; }
        public int TeamworkRating { get; set; }
        public int ProblemSolvingRating { get; set; }
        public int ProfessionalismRating { get; set; }
        public int ReliabilityRating { get; set; }
        public int OverallPerformanceRating { get; set; }
        public string? Comments { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
