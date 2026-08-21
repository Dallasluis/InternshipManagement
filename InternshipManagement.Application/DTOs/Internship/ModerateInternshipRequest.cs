using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Application.DTOs.Internship
{
    public class ModerateInternshipRequest
    {
        public ModerationStatus Status { get; set; }
        public string? Notes { get; set; }
    }
}