using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}