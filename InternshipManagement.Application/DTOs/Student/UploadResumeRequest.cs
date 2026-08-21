using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Student
{
    public class UploadResumeRequest
    {
        [Required]
        public string ResumeUrl { get; set; }
    }
}