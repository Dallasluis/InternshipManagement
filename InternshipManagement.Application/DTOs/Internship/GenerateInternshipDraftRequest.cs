using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Internship
{
    /// <summary>What the company types: a plain-language description of the opportunity.</summary>
    public class GenerateInternshipDraftRequest
    {
        [Required]
        [StringLength(4000, MinimumLength = 20,
            ErrorMessage = "Describe the opportunity in at least 20 characters (max 4000).")]
        public string Prompt { get; set; } = string.Empty;
    }
}
