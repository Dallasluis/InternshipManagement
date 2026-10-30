using System;

namespace InternshipManagement.Application.DTOs.Reports
{
    public class InternshipReportResponse
    {
        public int Id { get; set; }
        public int InternshipId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
