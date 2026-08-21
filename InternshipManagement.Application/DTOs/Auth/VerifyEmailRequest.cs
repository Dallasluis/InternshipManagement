using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Auth
{
    public class VerifyEmailRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string Token { get; set; }
    }
}