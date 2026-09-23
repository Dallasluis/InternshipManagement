using InternshipManagement.Domain.Common;
using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Domain.Entities
{
    public class ApplicationStatusHistory : BaseEntity
    {
        public int InternshipApplicationId { get; set; }
        public virtual InternshipApplication InternshipApplication { get; set; }
        public ApplicationStatus PreviousStatus { get; set; }
        public ApplicationStatus NewStatus { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public int? ChangedByUserId { get; set; }
        public string? Notes { get; set; }
    }
}
