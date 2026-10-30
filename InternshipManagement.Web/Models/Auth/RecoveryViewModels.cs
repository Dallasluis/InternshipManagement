using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.Models.Auth
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "The reset code is missing. Open the link from your email again.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a new password.")]
        [MinLength(10, ErrorMessage = "The password must be at least 10 characters.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Repeat the new password.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>One page for the three email links: verify email, confirm a new email, cancel an email change.</summary>
    public class EmailActionViewModel
    {
        public string Kind { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;

        public string Heading { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string ButtonText { get; set; } = string.Empty;

        /// <summary>False while the page still asks for the click; true once the API has answered.</summary>
        public bool Done { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
