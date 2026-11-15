namespace InternshipManagement.Domain.Entities
{
    /// <summary>One administrator or company action, kept as a permanent record. Rows are only ever added.</summary>
    public class AuditLog
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string Role { get; set; } = string.Empty;
        /// <summary>For example "Admin.SuspendUser" or "Applications.ScheduleInterview".</summary>
        public string Action { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        /// <summary>The record the action was about (the id in the address), when there is one.</summary>
        public string? EntityId { get; set; }
        public int StatusCode { get; set; }
        public string? IpAddress { get; set; }
    }
}
