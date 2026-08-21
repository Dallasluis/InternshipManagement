namespace InternshipManagement.Domain.Enums
{
    public enum InternshipStatus
    {
        Draft,
        Published,
        Closed,
        Filled
    }

    // Additional enums needed for the system
    public enum CompanyVerificationStatus
    {
        Pending,
        UnderReview,
        Verified,
        Rejected,
        Suspended
    }

    public enum ModerationStatus
    {
        Pending,
        Approved,
        Rejected,
        Flagged,
        Removed
    }

    public enum InternshipType
    {
        FullTime,
        PartTime,
        Summer,
        Winter,
        YearRound,
        Flexible
    }

    public enum InternshipDuration
    {
        LessThan3Months,
        ThreeToSixMonths,
        SixToTwelveMonths,
        MoreThanTwelveMonths
    }

    // For reporting (MVP)
    public enum ReportType
    {
        Fraud,
        Scam,
        MisleadingInformation,
        InappropriateContent,
        Harassment,
        Discrimination,
        Other
    }

    public enum ReportStatus
    {
        Pending,
        UnderInvestigation,
        Resolved,
        Dismissed
    }

    // For subscription (placeholder for MVP)
    public enum SubscriptionStatus
    {
        Inactive,
        Active,
        Expired,
        Cancelled
    }
}