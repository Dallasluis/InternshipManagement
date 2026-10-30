namespace InternshipManagement.Domain.Entities
{
    public class EmailActionToken
    {
        public string TokenHash { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string? TargetEmail { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? ConsumedAt { get; set; }
        public DateTimeOffset? CancelledAt { get; set; }
        public DateTimeOffset? ReminderSentAt { get; set; }
        public bool IsResend { get; set; }
    }
}
