namespace InternshipManagement.Application.DTOs.Matching
{
    public sealed class StudentMatchScore
    {
        public int UserId { get; set; }
        public int Score { get; set; }
        public List<string> MatchingSkills { get; set; } = new();
    }
}
