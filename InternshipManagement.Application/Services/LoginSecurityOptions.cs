namespace InternshipManagement.Application.Services
{
    public sealed class LoginSecurityOptions
    {
        public int PasswordMinimumLength { get; set; } = 10;
        public int PasswordHashIterations { get; set; } = 600000;
        public int LockoutMaxFailedAttempts { get; set; } = 5;
        public int LockoutWindowMinutes { get; set; } = 15;
        public int InitialLockoutMinutes { get; set; } = 15;
        public int MaxLockoutMinutes { get; set; } = 60;
        public int LoginAttemptsPerIpPerMinute { get; set; } = 20;
        public int StudentIdleTimeoutHours { get; set; } = 2;
        public int AdminCompanyIdleTimeoutMinutes { get; set; } = 30;
        public int AbsoluteSessionHours { get; set; } = 12;
        public int RememberStudentDays { get; set; } = 30;
        public int OtpLifetimeMinutes { get; set; } = 5;
        public int OtpMaxAttempts { get; set; } = 5;
        public int OtpResendSeconds { get; set; } = 60;
        public int SecurityEventRetentionMonths { get; set; } = 12;
    }
}
