namespace InternshipManagement.Application.DTOs.Auth
{
    public class UpdateAccountPreferencesRequest
    {
        public bool EmailNotifications { get; set; }
        public bool InternshipAlerts { get; set; }
    }
}
