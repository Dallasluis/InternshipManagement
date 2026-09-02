namespace InternshipManagement.Application.DTOs.Auth
{
    public class AccountPreferencesDto
    {
        public bool EmailNotifications { get; set; } = true;
        public bool InternshipAlerts { get; set; } = true;
    }
}
