namespace InternshipManagement.Domain.Enums
{
    public enum ApplicationStatus
    {
        Applied,
        UnderReview,
        Shortlisted,
        Rejected,
        Withdrawn,
        WithdrawnAcceptedElsewhere
        // Note: InterviewScheduled, InterviewCompleted, OfferMade, 
        // OfferAccepted, OfferDeclined, Placed - Deferred to later releases
    }
}