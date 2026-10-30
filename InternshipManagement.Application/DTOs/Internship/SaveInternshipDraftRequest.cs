
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Internship
{
    /// <summary>The company's edits to a saved draft (the whole form, sent back as is).</summary>
    public class SaveInternshipDraftRequest
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [StringLength(2000)] public string? Responsibilities { get; set; }
        [StringLength(2000)] public string? RequiredQualifications { get; set; }
        [StringLength(2000)] public string? PreferredQualifications { get; set; }
        [StringLength(1000)] public string? RequiredSkills { get; set; }
        [StringLength(1000)] public string? PreferredSkills { get; set; }

        [StringLength(100)] public string? Industry { get; set; }
        [StringLength(200)] public string? Location { get; set; }
        public bool IsRemote { get; set; }

        /// <summary>An InternshipType name, or null.</summary>
        [StringLength(50)] public string? InternshipType { get; set; }

        /// <summary>An InternshipDuration name, or null.</summary>
        [StringLength(50)] public string? Duration { get; set; }

        public List<string>? EligibleProgrammes { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? ApplicationDeadline { get; set; }

        [Range(1, 1000)]
        public int? NumberOfPositions { get; set; }
    }
}
