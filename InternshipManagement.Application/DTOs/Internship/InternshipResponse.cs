using System;
using System.Collections.Generic;

namespace InternshipManagement.Application.DTOs.Internship
{
    public class InternshipResponse
    {
        public int Id { get; set; }
        public int CompanyProfileId { get; set; }
        public string CompanyName { get; set; }
        public string? CompanyLogoUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string? Responsibilities { get; set; }
        public string? Requirements { get; set; }
        public string? Qualifications { get; set; }
        public string? Skills { get; set; }
        public string Industry { get; set; }
        public string? Location { get; set; }
        public bool IsRemote { get; set; }
        public string InternshipType { get; set; }
        public string Duration { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime ApplicationDeadline { get; set; }
        public int NumberOfPositions { get; set; }
        public string? Compensation { get; set; }
        public decimal? StipendAmount { get; set; }
        public string? Currency { get; set; }
        public List<string>? EligibleProgrammes { get; set; }
        public string Status { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int Views { get; set; }
        public int ApplicationsCount { get; set; }
        public string ModerationStatus { get; set; }
        public bool IsSaved { get; set; } = false;
        public bool HasApplied { get; set; } = false;
        public int MatchScore { get; set; }  // ✅ Add this
    }
}