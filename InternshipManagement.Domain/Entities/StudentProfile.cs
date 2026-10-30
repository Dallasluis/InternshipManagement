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
        public string? CoverLetterUrl { get; set; }
        public string? AcademicTranscriptUrl { get; set; }
        public string? QualificationDocumentUrl { get; set; }
        public string? IdentificationDocumentUrl { get; set; }
        public string? CertificatesUrl { get; set; }
        public string? OtherSupportingDocumentsUrl { get; set; }
        public Guid? ResumeDocumentId { get; set; }
        public Guid? CoverLetterDocumentId { get; set; }
        public Guid? AcademicTranscriptDocumentId { get; set; }
        public Guid? QualificationDocumentId { get; set; }
        public Guid? IdentificationDocumentId { get; set; }
        public Guid? CertificatesDocumentId { get; set; }
        public Guid? OtherSupportingDocumentsId { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // Navigation Properties
        public virtual ICollection<InternshipApplication> InternshipApplications { get; set; }
        public virtual ICollection<Education> Education { get; set; }
        public virtual ICollection<WorkExperience> WorkExperience { get; set; }
        public virtual ICollection<Skill> Skills { get; set; }
        public virtual ICollection<StudentCertification> Certifications { get; set; }
        public virtual ICollection<StudentProject> Projects { get; set; }
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
        public string? CompletionStatus { get; set; }
        public string? SupportingDocumentUrl { get; set; }
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

    public class StudentCertification : BaseEntity
    {
        public int StudentProfileId { get; set; }
        public virtual StudentProfile StudentProfile { get; set; }
        public string Name { get; set; }
        public string IssuingOrganization { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? CredentialUrl { get; set; }
        public string? CertificateFileUrl { get; set; }
    }

    public class StudentProject : BaseEntity
    {
        public int StudentProfileId { get; set; }
        public virtual StudentProfile StudentProfile { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Role { get; set; }
        public string? Technologies { get; set; }
        public string? ProjectUrl { get; set; }
        public string? RepositoryUrl { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
