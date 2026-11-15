namespace InternshipManagement.Web.Models.Admin
{
    public sealed class DisputedWithdrawalItem
    {
        public int Id { get; set; }
        public int PlacementId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string InternshipTitle { get; set; } = string.Empty;
        public string PlacementStatus { get; set; } = string.Empty;
        public string StudentReason { get; set; } = string.Empty;
        public string? CompanyDecisionNotes { get; set; }
        public string? DisputeReason { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? DisputedAt { get; set; }
    }

    public sealed class AuditLogItem
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public int StatusCode { get; set; }
        public string? IpAddress { get; set; }
    }

    public sealed class AuditLogPage
    {
        public List<AuditLogItem> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
