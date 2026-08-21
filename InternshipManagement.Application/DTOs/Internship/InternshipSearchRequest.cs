namespace InternshipManagement.Application.DTOs.Internship
{
    public class InternshipSearchRequest
    {
        public string? Keyword { get; set; }
        public string? Location { get; set; }
        public string? Industry { get; set; }
        public string? InternshipType { get; set; }
        public decimal? MinStipend { get; set; }
        public bool? IsRemote { get; set; }
        public string? Programme { get; set; }
        public bool ShowOnlyMatchingProgrammes { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
    }
}