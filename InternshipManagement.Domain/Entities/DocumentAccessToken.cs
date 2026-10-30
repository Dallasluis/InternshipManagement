namespace InternshipManagement.Domain.Entities
{
    public class DocumentAccessToken
    {
        public string TokenHash { get; set; } = string.Empty;
        public Guid DocumentId { get; set; }
        public int RequestedByUserId { get; set; }
        public int? ReportId { get; set; }
        public int? CompanyProfileId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
    }
}
