namespace InternshipManagement.Web.Models.Student
{
    public class StudentProfileResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? PhoneNumber { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? University { get; set; }
        public string? Programme { get; set; }
        public string? YearOfStudy { get; set; }
        public string? ExpectedGraduationYear { get; set; }
        public string? ResumeUrl { get; set; }
        public string? CoverLetterUrl { get; set; }
        public string? AcademicTranscriptUrl { get; set; }
        public string? QualificationDocumentUrl { get; set; }
        public string? IdentificationDocumentUrl { get; set; }
        public string? CertificatesUrl { get; set; }
        public string? OtherSupportingDocumentsUrl { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public List<EducationDto>? Education { get; set; }
        public List<WorkExperienceDto>? WorkExperience { get; set; }
        public List<SkillDto>? Skills { get; set; }
    }

    public class EducationDto
    {
        public int Id { get; set; }
        public string Institution { get; set; } = string.Empty;
        public string Degree { get; set; } = string.Empty;
        public string FieldOfStudy { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public string? Grade { get; set; }
    }

    public class WorkExperienceDto
    {
        public int Id { get; set; }
        public string Company { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class SkillDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ProficiencyLevel { get; set; }
    }
}