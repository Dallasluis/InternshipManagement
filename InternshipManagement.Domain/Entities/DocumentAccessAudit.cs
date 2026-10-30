namespace InternshipManagement.Domain.Entities
{
    public class DocumentAccessAudit
    {
        public long Id { get; set; }
        public Guid DocumentId { get; set; }
        public int UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public int? ReportId { get; set; }
        public int? CompanyProfileId { get; set; }
        public DateTimeOffset AccessedAt { get; set; }
    }
}
