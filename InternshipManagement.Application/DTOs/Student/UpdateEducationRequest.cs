using System;

namespace InternshipManagement.Application.DTOs.Student
{
    public class UpdateEducationRequest
    {
        public string Institution { get; set; }
        public string Degree { get; set; }
        public string FieldOfStudy { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public string? Grade { get; set; }
    }
}