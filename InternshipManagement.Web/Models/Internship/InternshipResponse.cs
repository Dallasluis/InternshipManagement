namespace InternshipManagement.Web.Models.Internship
{
    public class InternshipResponse
    {
        public int Id { get; set; }
        public int CompanyProfileId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyLogoUrl { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Responsibilities { get; set; }
        public string? Requirements { get; set; }
        public string? Qualifications { get; set; }
        public string? Skills { get; set; }
        public string Industry { get; set; } = string.Empty;
        public string? Location { get; set; }
        public bool IsRemote { get; set; }
        public string InternshipType { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime ApplicationDeadline { get; set; }
        public int NumberOfPositions { get; set; }
        public string? Compensation { get; set; }
        public decimal? StipendAmount { get; set; }
        public string? Currency { get; set; }
        public List<string>? EligibleProgrammes { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PublishedAt { get; set; }
        public int Views { get; set; }
        public int ApplicationsCount { get; set; }
        public string ModerationStatus { get; set; } = string.Empty;
        public bool IsSaved { get; set; }
        public bool HasApplied { get; set; }
        public int MatchScore { get; set; }
        public DateTime CreatedAt { get; set; }  
    }
}