using InternshipManagement.Domain.Common;
using InternshipManagement.Domain.Enums;
using System;

namespace InternshipManagement.Domain.Entities
{
    public class InternshipApplication : BaseEntity
    {
        public int StudentProfileId { get; set; }
        public virtual StudentProfile StudentProfile { get; set; }

        public int InternshipId { get; set; }
        public virtual Internship Internship { get; set; }

        public string? CoverLetter { get; set; }
        public string? AdditionalDocuments { get; set; }

        public ApplicationStatus Status { get; set; }
        public DateTime? StatusUpdatedAt { get; set; }
        public string? StatusNotes { get; set; }

        public bool IsShortlisted { get; set; }
        public DateTime? ShortlistedAt { get; set; }
        public string? ShortlistNotes { get; set; }

        public DateTime? InterviewDateTime { get; set; }
        public string? InterviewType { get; set; }
        public string? InterviewLocationOrLink { get; set; }
        public string? InterviewNotes { get; set; }

        public decimal? OfferStipendAmount { get; set; }
        public DateTime? OfferStartDate { get; set; }
        public string? OfferDetails { get; set; }
        public DateTime? OfferExpiryDate { get; set; }
        public DateTime? OfferAcceptedAt { get; set; }
        public DateTime? OfferDeclinedAt { get; set; }
    }
}