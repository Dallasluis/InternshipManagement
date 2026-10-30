namespace InternshipManagement.Domain.Entities
{
    public class AccountSecurityEvent
    {
        public long Id { get; set; }
        public int? UserId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
