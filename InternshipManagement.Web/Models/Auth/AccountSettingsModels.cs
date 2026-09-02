namespace InternshipManagement.Web.Models.Auth
{
    public class ChangePasswordViewModel
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangeEmailViewModel
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewEmail { get; set; } = string.Empty;
    }

    public class AccountPreferencesViewModel
    {
        public bool EmailNotifications { get; set; } = true;
        public bool InternshipAlerts { get; set; } = true;
    }

    public class AccountSettingsViewModel
    {
        public string? UserFullName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserType { get; set; }
        public AccountPreferencesViewModel Preferences { get; set; } = new();
    }
}
