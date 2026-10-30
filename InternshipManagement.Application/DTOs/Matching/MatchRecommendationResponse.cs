namespace InternshipManagement.Application.DTOs.Matching
{
    public class MatchRecommendationResponse
    {
        public int TargetId { get; set; }
        public string TargetType { get; set; }
        public string Title { get; set; }
        public string? Subtitle { get; set; }
        public int MatchScore { get; set; }
        public List<string> MatchingSkills { get; set; } = new();
        public List<string> MatchingQualifications { get; set; } = new();
        public List<string> MissingPreferredSkills { get; set; } = new();
        public string Explanation { get; set; }
    }
}
