using InternshipManagement.Domain.Common;
using System.Collections.Generic;

namespace InternshipManagement.Domain.Entities
{
    public class StudentProfile : BaseEntity
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? PhoneNumber { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? PortfolioUrl { get; set; }

        // Academic Information - Course Agnostic
        public string? University { get; set; }
        public string? Programme { get; set; }  // e.g., "Civil Engineering", "Accounting", "Marketing"
        public string? YearOfStudy { get; set; }  // e.g., "1st Year", "2nd Year", "Final Year"
        public string? ExpectedGraduationYear { get; set; }

        public string? ResumeUrl { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // Navigation Properties
        public virtual ICollection<InternshipApplication> InternshipApplications { get; set; }
        public virtual ICollection<Education> Education { get; set; }
        public virtual ICollection<WorkExperience> WorkExperience { get; set; }
        public virtual ICollection<Skill> Skills { get; set; }
    }

    public class Education : BaseEntity
    {
        public int StudentProfileId { get; set; }
        public virtual StudentProfile StudentProfile { get; set; }
        public string Institution { get; set; }
        public string Degree { get; set; }
        public string FieldOfStudy { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public string? Grade { get; set; }
    }

    public class WorkExperience : BaseEntity
    {
        public int StudentProfileId { get; set; }
        public virtual StudentProfile StudentProfile { get; set; }
        public string Company { get; set; }
        public string Position { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class Skill : BaseEntity
    {
        public int StudentProfileId { get; set; }
        public virtual StudentProfile StudentProfile { get; set; }
        public string Name { get; set; }
        public string? ProficiencyLevel { get; set; }
    }
}