using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.Models.Auth
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
        public string? TwoFactorCode { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public string? TwoFactorMessage { get; set; }
    }
}
