using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.Models.Internship
{
    public class CreateInternshipRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        public string? Responsibilities { get; set; }
        public string? Requirements { get; set; }
        public string? Qualifications { get; set; }
        public string? Skills { get; set; }

        [Required(ErrorMessage = "Industry is required")]
        public string Industry { get; set; } = string.Empty;

        public string? Location { get; set; }
        public bool IsRemote { get; set; }

        [Required(ErrorMessage = "Internship type is required")]
        public string InternshipType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Duration is required")]
        public string Duration { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Application deadline is required")]
        public DateTime ApplicationDeadline { get; set; }

        [Required(ErrorMessage = "Number of positions is required")]
        [Range(1, 100)]
        public int NumberOfPositions { get; set; }

        public string? Compensation { get; set; }
        public decimal? StipendAmount { get; set; }
        public string? Currency { get; set; }

        public List<string>? EligibleProgrammes { get; set; }
    }
}