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
        public string? ResumeUrl { get; set; }
        public string? CoverLetterUrl { get; set; }
        public string? AcademicTranscriptUrl { get; set; }
        public string? QualificationDocumentUrl { get; set; }
        public string? IdentificationDocumentUrl { get; set; }
        public string? CertificatesUrl { get; set; }
        public string? OtherSupportingDocumentsUrl { get; set; }
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
    }
}