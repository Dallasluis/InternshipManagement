namespace InternshipManagement.Web.Models.Internship
{
    public class InternshipSearchViewModel
    {
        public string? Keyword { get; set; }
        public string? Location { get; set; }
        public string? Industry { get; set; }
        public string? InternshipType { get; set; }
        public decimal? MinStipend { get; set; }
        public bool? IsRemote { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    // Mirrors the API's anonymous { Items, TotalCount, PageNumber, PageSize } response
    // from InternshipsController.Search.
    public class PagedInternshipResult
    {
        public List<InternshipResponse> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}