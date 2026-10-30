using System.Collections.Generic;

namespace InternshipManagement.Application.DTOs.Student
{
    public class StudentProfileResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? PhoneNumber { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? PortfolioUrl { get; set; }

        // Academic Information
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

        public List<EducationDto> Education { get; set; }
        public List<WorkExperienceDto> WorkExperience { get; set; }
        public List<SkillDto> Skills { get; set; }
        public List<StudentCertificationDto> Certifications { get; set; }
        public List<StudentProjectDto> Projects { get; set; }
    }

    public class UpdateStudentProfileRequest
    {
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? PhoneNumber { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? PortfolioUrl { get; set; }

        // Academic Information
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
    }

    public class EducationDto
    {
        public int Id { get; set; }
        public string Institution { get; set; }
        public string Degree { get; set; }
        public string FieldOfStudy { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public string? Grade { get; set; }
        public string? CompletionStatus { get; set; }
    }

    public class WorkExperienceDto
    {
        public int Id { get; set; }
        public string Company { get; set; }
        public string Position { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class SkillDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? ProficiencyLevel { get; set; }
    }

    public class StudentCertificationDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string IssuingOrganization { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }

    public class StudentProjectDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Role { get; set; }
        public string? Technologies { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
