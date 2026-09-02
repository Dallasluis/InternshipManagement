using System;

namespace InternshipManagement.Application.DTOs.Application
{
    public class ApplicationResponse
    {
        public int Id { get; set; }
        public int InternshipId { get; set; }
        public string InternshipTitle { get; set; }
        public string CompanyName { get; set; }
        public string Status { get; set; }
        public DateTime AppliedAt { get; set; }
        public DateTime? StatusUpdatedAt { get; set; }
        public string? CoverLetter { get; set; }
        public bool IsShortlisted { get; set; }

        public DateTime? InterviewDateTime { get; set; }
        public string? InterviewType { get; set; }
        public string? InterviewLocationOrLink { get; set; }
        public string? InterviewNotes { get; set; }

        public decimal? OfferStipendAmount { get; set; }
        public DateTime? OfferStartDate { get; set; }
        public string? OfferDetails { get; set; }
        public DateTime? OfferExpiryDate { get; set; }

        public StudentInfo Student { get; set; }
    }

    public class StudentInfo
    {
        public int StudentProfileId { get; set; }
        public string FullName { get; set; }
        public string? Email { get; set; }
        public string? Location { get; set; }

        // Academic Information
        public string? University { get; set; }
        public string? Programme { get; set; }
        public string? YearOfStudy { get; set; }

        public string? ResumeUrl { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}