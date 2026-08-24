using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.Models.Application
{
    public class UpdateApplicationStatusRequest
    {
        [Required]
        public string Status { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }
}