using System;

namespace InternshipManagement.Application.DTOs.Company
{
    public class CompanyProfileResponse
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string? Description { get; set; }
        public string Industry { get; set; }
        public string? Website { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? LogoUrl { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PhoneNumber { get; set; }
        public string VerificationStatus { get; set; }
        public bool IsSubscribed { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public int ActiveInternships { get; set; }
        public int TotalInternships { get; set; }
        public int TotalApplications { get; set; }
    }

    public class UpdateCompanyProfileRequest
    {
        public string? Description { get; set; }
        public string? Industry { get; set; }
        public string? Website { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class SubmitVerificationRequest
    {
        public string VerificationDocuments { get; set; }
        public string? Notes { get; set; }
    }

    public class ReviewVerificationRequest
    {
        public bool Approved { get; set; }
        public string? Notes { get; set; }
    }
}