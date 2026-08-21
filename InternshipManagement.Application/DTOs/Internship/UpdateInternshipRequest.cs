using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Internship
{
    public class UpdateInternshipRequest
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public string? Responsibilities { get; set; }
        public string? Requirements { get; set; }
        public string? Qualifications { get; set; }
        public string? Skills { get; set; }

        [Required]
        public string Industry { get; set; }

        public string? Location { get; set; }
        public bool IsRemote { get; set; }

        [Required]
        public string InternshipType { get; set; }

        [Required]
        public string Duration { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        public DateTime ApplicationDeadline { get; set; }

        [Required]
        [Range(1, 100)]
        public int NumberOfPositions { get; set; }

        public string? Compensation { get; set; }
        public decimal? StipendAmount { get; set; }
        public string? Currency { get; set; }

        public List<string>? EligibleProgrammes { get; set; }
    }
}