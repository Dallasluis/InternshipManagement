using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Auth
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(8)]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string? PhoneNumber { get; set; }
        public string? UserType { get; set; }
        public string? CompanyName { get; set; }
        public string? Industry { get; set; }
    }
}