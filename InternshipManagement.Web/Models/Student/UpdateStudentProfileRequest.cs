using System.Text.Json.Serialization;

namespace InternshipManagement.Web.Models.Student
{
    public class UpdateStudentProfileRequest
    {
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
        [JsonIgnore]
        public IFormFile? ResumeFile { get; set; }
        [JsonIgnore]
        public IFormFile? CoverLetterFile { get; set; }
        [JsonIgnore]
        public IFormFile? AcademicTranscriptFile { get; set; }
        [JsonIgnore]
        public IFormFile? QualificationDocumentFile { get; set; }
        [JsonIgnore]
        public IFormFile? IdentificationDocumentFile { get; set; }
        [JsonIgnore]
        public IFormFile? CertificatesFile { get; set; }
        [JsonIgnore]
        public IFormFile? OtherSupportingDocumentsFile { get; set; }
        [JsonIgnore]
        public IFormFile? ProfilePictureFile { get; set; }
    }
}