using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Auth
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
        public string? TwoFactorCode { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public string? ClientIpAddress { get; set; }
    }
}