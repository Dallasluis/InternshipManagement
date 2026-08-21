using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Application
{
    public class UpdateApplicationStatusRequest
    {
        [Required]
        public string Status { get; set; }

        public string? Notes { get; set; }
    }
}