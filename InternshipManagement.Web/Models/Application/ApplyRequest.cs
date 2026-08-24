using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.Models.Application
{
    public class ApplyRequest
    {
        [Required]
        public int InternshipId { get; set; }

        public string? CoverLetter { get; set; }

        public List<string>? AdditionalDocumentUrls { get; set; }
    }
}