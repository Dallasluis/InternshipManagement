namespace InternshipManagement.Web.Models.Application
{
    public class ApplicationResponse
    {
        public int Id { get; set; }
        public int InternshipId { get; set; }
        public string InternshipTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public DateTime? StatusUpdatedAt { get; set; }
        public string? CoverLetter { get; set; }
        public bool IsShortlisted { get; set; }
        public StudentInfo? Student { get; set; }
    }

    public class StudentInfo
    {
        public int StudentProfileId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Location { get; set; }
        public string? University { get; set; }
        public string? Programme { get; set; }
        public string? YearOfStudy { get; set; }
        public string? ResumeUrl { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}