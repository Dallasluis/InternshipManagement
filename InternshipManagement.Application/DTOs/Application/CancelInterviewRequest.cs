using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Application
{
    public class CancelInterviewRequest
    {
        [StringLength(500)]
        public string? Reason { get; set; }
    }
}
