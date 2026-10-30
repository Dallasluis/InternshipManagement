namespace InternshipManagement.Infrastructure.Email
{
    public sealed class SmtpEmailOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool UseSsl { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderAddress { get; set; } = string.Empty;
    }
}
