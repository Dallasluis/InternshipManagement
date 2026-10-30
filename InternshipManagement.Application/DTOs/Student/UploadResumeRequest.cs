using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Student
{
    public class UploadResumeRequest
    {
        [Required]
        public Guid ResumeDocumentId { get; set; }
    }
}