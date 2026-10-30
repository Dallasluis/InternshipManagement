using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Domain.Entities
{
    public class PrivateDocument
    {
        public Guid Id { get; set; }
        public int OwnerUserId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Length { get; set; }
        public DocumentRetentionState RetentionState { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ClosedAt { get; set; }
    }
}
