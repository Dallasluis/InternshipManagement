namespace InternshipManagement.Web.Models.Auth
{
    public class AuthApiResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public string? UserId { get; set; }
        public string? UserType { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
    }
}