using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.Models.Application
{
    public class ScheduleInterviewRequest
    {
        [Required]
        public DateTime InterviewDateTime { get; set; }

        [Required]
        public string InterviewType { get; set; } = string.Empty;

        public string? InterviewLocationOrLink { get; set; }

        public string? InterviewNotes { get; set; }
    }
}
