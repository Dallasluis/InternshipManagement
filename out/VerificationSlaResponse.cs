namespace InternshipManagement.Application.DTOs.Company
{
    public sealed record VerificationSlaResponse(
        int DecisionsMeasured,
        int DecisionsWithinThreeBusinessDays,
        double WithinTargetPercentage,
        double TargetPercentage,
        IReadOnlyList<OverdueAppealResponse> OverdueAppeals);

    /// <summary>An appeal still waiting for a decision after more than 5 business days.</summary>
    public sealed record OverdueAppealResponse(
        int CompanyId,
        string CompanyName,
        DateTime SubmittedAt,
        int BusinessDaysWaiting);
}
