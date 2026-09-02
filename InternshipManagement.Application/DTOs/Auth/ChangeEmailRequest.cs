namespace InternshipManagement.Application.DTOs.Auth
{
    public class ChangeEmailRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewEmail { get; set; } = string.Empty;
    }
}
