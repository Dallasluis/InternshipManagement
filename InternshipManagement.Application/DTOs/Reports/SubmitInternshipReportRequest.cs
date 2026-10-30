using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Reports
{
    public class SubmitInternshipReportRequest
    {
        [Required]
        public int InternshipId { get; set; }

        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        [StringLength(4000, MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;
    }
}
