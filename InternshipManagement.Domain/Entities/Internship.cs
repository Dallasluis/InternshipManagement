using InternshipManagement.Domain.Common;
using InternshipManagement.Domain.Enums;
using System;
using System.Collections.Generic;

namespace InternshipManagement.Domain.Entities
{
    public class Internship : BaseEntity
    {
        public int CompanyProfileId { get; set; }
        public virtual CompanyProfile CompanyProfile { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public string? Responsibilities { get; set; }
        public string? Requirements { get; set; }
        public string? Qualifications { get; set; }
        public string? Skills { get; set; }

        public string Industry { get; set; }
        public string? Location { get; set; }
        public bool IsRemote { get; set; }
        public InternshipType InternshipType { get; set; }
        public InternshipDuration Duration { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime ApplicationDeadline { get; set; }
        public int NumberOfPositions { get; set; }

        public string? Compensation { get; set; }
        public decimal? StipendAmount { get; set; }
        public string? Currency { get; set; }

        // Course-Agnostic: Eligible Programmes
        public string? EligibleProgrammes { get; set; }  // Store as JSON array or comma-separated
                                                         // e.g., "Civil Engineering, Construction Engineering"
                                                         // or JSON: ["Civil Engineering", "Construction Engineering"]

        public InternshipStatus Status { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int Views { get; set; } = 0;
        public int ApplicationsCount { get; set; } = 0;

        public ModerationStatus ModerationStatus { get; set; }
        public string? ModerationNotes { get; set; }
        public DateTime? LastModeratedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<InternshipApplication> InternshipApplications { get; set; }
    }
}