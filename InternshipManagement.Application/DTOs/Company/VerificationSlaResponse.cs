namespace InternshipManagement.Application.DTOs.Company
{
    public sealed record VerificationSlaResponse(
        int DecisionsMeasured,
        int DecisionsWithinThreeBusinessDays,
        double WithinTargetPercentage,
        double TargetPercentage);
}
