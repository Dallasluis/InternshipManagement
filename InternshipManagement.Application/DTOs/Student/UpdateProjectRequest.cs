using System;

namespace InternshipManagement.Application.DTOs.Student
{
    public class UpdateProjectRequest
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Role { get; set; }
        public string? Technologies { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
