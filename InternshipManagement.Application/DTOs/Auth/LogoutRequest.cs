using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Auth
{
    public class LogoutRequest
    {
        [Required]
        public int UserId { get; set; }
    }
}