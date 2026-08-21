using InternshipManagement.Domain.Common;
using InternshipManagement.Domain.Enums;
using System;
using System.Collections.Generic;

namespace InternshipManagement.Domain.Entities
{
    public class CompanyProfile : BaseEntity
    {
        public int UserId { get; set; }
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

        // Verification
        public CompanyVerificationStatus VerificationStatus { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? VerificationDocuments { get; set; }
        public string? AdminNotes { get; set; }

        // Subscription (MVP placeholder)
        public bool IsSubscribed { get; set; } = false;
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }

        // Navigation Properties
        public virtual ICollection<Internship> Internships { get; set; }
        public virtual ICollection<CompanyRepresentative> Representatives { get; set; }
    }

    public class CompanyRepresentative : BaseEntity
    {
        public int CompanyProfileId { get; set; }
        public virtual CompanyProfile CompanyProfile { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; }
        public bool IsPrimary { get; set; }
    }
}