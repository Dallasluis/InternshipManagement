namespace InternshipManagement.Application.Services
{
    public sealed class EmailVerificationOptions
    {
        public int LinkExpiryHours { get; set; } = 24;
        public int ResendsPerHour { get; set; } = 3;
        public int ResendsPerDay { get; set; } = 10;
        public int ReminderAfterDays { get; set; } = 3;
        public int DeleteAfterDays { get; set; } = 30;
        public int SecurityEventRetentionMonths { get; set; } = 12;
        public string PublicApiBaseUrl { get; set; } = "http://localhost:49162";
    }
}
