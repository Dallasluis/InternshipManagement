namespace InternshipManagement.Web.Models.Student
{
    public sealed class SavedInternshipItem
    {
        public int InternshipId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public DateTime ApplicationDeadline { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; }
    }
}
