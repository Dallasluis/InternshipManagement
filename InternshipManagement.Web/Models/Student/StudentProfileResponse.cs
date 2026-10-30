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
        public Guid? ResumeDocumentId { get; set; }
        public Guid? CoverLetterDocumentId { get; set; }
        public Guid? AcademicTranscriptDocumentId { get; set; }
        public Guid? QualificationDocumentId { get; set; }
        public Guid? IdentificationDocumentId { get; set; }
        public Guid? CertificatesDocumentId { get; set; }
        public Guid? OtherSupportingDocumentsId { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public List<EducationDto>? Education { get; set; }
        public List<WorkExperienceDto>? WorkExperience { get; set; }
        public List<SkillDto>? Skills { get; set; }
        public List<StudentCertificationDto>? Certifications { get; set; }
        public List<StudentProjectDto>? Projects { get; set; }
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
        public string? CompletionStatus { get; set; }
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

    public class StudentCertificationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IssuingOrganization { get; set; } = string.Empty;
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }

    public class StudentProjectDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Role { get; set; }
        public string? Technologies { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
