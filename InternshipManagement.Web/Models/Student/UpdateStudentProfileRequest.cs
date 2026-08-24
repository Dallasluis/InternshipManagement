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
    }
}