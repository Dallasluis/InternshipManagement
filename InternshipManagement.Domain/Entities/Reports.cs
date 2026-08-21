using InternshipManagement.Domain.Common;
using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Domain.Entities
{
    public class Report : BaseEntity
    {
        public int ReporterId { get; set; } // Student who reported
        public int InternshipId { get; set; }
        public virtual Internship Internship { get; set; }

        public ReportType Type { get; set; }
        public string Description { get; set; }
        public ReportStatus Status { get; set; }
        public string? AdminResponse { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedBy { get; set; } // Admin ID
    }
}