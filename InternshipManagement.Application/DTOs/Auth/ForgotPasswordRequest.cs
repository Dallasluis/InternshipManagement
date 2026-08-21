using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Auth
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}