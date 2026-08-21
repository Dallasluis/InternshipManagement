using System.Collections.Generic;

namespace InternshipManagement.Application.DTOs.Auth
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public string? UserId { get; set; }
        public string? UserType { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public List<string>? Errors { get; set; }
        public string? Message { get; set; }
    }
}