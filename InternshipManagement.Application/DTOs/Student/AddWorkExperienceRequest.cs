using System;

namespace InternshipManagement.Application.DTOs.Student
{
    public class AddWorkExperienceRequest
    {
        public string Company { get; set; }
        public string Position { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
    }
}