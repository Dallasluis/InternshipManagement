using System;

namespace InternshipManagement.Application.DTOs.Student
{
    public class UpdateCertificationRequest
    {
        public string Name { get; set; }
        public string IssuingOrganization { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }
}
