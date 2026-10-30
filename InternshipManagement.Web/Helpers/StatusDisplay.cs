using System.Text.RegularExpressions;

namespace InternshipManagement.Web.Helpers
{
    /// <summary>One place that decides how every status is worded and coloured, so the same status looks the same on every page.</summary>
    public static class StatusDisplay
    {
        private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Applied"] = "Applied",
            ["UnderReview"] = "Under review",
            ["Shortlisted"] = "Shortlisted",
            ["InterviewScheduled"] = "Interview scheduled",
            ["InterviewCompleted"] = "Interview completed",
            ["OfferMade"] = "Offer made",
            ["Accepted"] = "Offer accepted",
            ["OfferDeclined"] = "Offer declined",
            ["Declined"] = "Offer declined",
            ["Rejected"] = "Rejected",
            ["Withdrawn"] = "Withdrawn",
            ["WithdrawnAcceptedElsewhere"] = "Withdrawn - accepted elsewhere",
            ["UnderInvestigation"] = "Under investigation",
            ["MoreInformationRequested"] = "More information requested",
            ["ReverificationRequired"] = "Re-verification required"
        };

        private static readonly Dictionary<string, string> Badges = new(StringComparer.OrdinalIgnoreCase)
        {
            // waiting on someone
            ["Pending"] = "bg-warning text-dark",
            ["UnderReview"] = "bg-warning text-dark",
            ["PendingReview"] = "bg-warning text-dark",
            ["Flagged"] = "bg-warning text-dark",
            ["Requested"] = "bg-warning text-dark",
            ["Proposed"] = "bg-warning text-dark",
            ["Submitted"] = "bg-warning text-dark",
            ["MoreInformationRequested"] = "bg-warning text-dark",
            ["ReverificationRequired"] = "bg-warning text-dark",
            // moving forward
            ["Applied"] = "bg-info text-dark",
            ["InterviewCompleted"] = "bg-info text-dark",
            ["Extended"] = "bg-info text-dark",
            ["UnderInvestigation"] = "bg-info text-dark",
            ["Disputed"] = "bg-info text-dark",
            ["Shortlisted"] = "bg-primary",
            ["InterviewScheduled"] = "bg-primary",
            ["OfferMade"] = "bg-primary",
            ["Completed"] = "bg-primary",
            ["Reviewed"] = "bg-primary",
            // good
            ["Accepted"] = "bg-success",
            ["Approved"] = "bg-success",
            ["Verified"] = "bg-success",
            ["Active"] = "bg-success",
            ["Published"] = "bg-success",
            ["Resolved"] = "bg-success",
            // bad
            ["Rejected"] = "bg-danger",
            ["Terminated"] = "bg-danger",
            ["Suspended"] = "bg-danger",
            ["Closed"] = "bg-danger",
            ["Cancelled"] = "bg-danger",
            ["Expired"] = "bg-danger",
            // finished / inactive
            ["Withdrawn"] = "bg-secondary",
            ["WithdrawnAcceptedElsewhere"] = "bg-secondary",
            ["OfferDeclined"] = "bg-secondary",
            ["Declined"] = "bg-secondary",
            ["Dismissed"] = "bg-secondary",
            ["Removed"] = "bg-secondary",
            ["Draft"] = "bg-secondary",
            ["Filled"] = "bg-secondary",
            ["Inactive"] = "bg-secondary",
            ["Unverified"] = "bg-secondary"
        };

        public static string Badge(string? status) =>
            !string.IsNullOrWhiteSpace(status) && Badges.TryGetValue(status, out var css) ? css : "bg-secondary";

        public static string Label(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return "-";
            if (Labels.TryGetValue(status, out var label)) return label;
            var spaced = Regex.Replace(status, "(?<=[a-z])(?=[A-Z])", " ");
            return spaced.Length > 1 ? spaced[0] + spaced[1..].ToLowerInvariant() : spaced;
        }

        // Company verification: the enum has aliases (Unverified/Pending, PendingReview/UnderReview), so the API may send either
        // name for the same state. Both are shown the same way.
        public static string VerificationKey(string? status) => (status ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "unverified" or "pending" or "" => "Unverified",
            "pendingreview" or "underreview" => "PendingReview",
            "verified" => "Verified",
            "rejected" => "Rejected",
            "suspended" => "Suspended",
            "moreinformationrequested" => "MoreInformationRequested",
            "reverificationrequired" => "ReverificationRequired",
            _ => status ?? string.Empty
        };

        public static string VerificationBadge(string? status) => Badge(VerificationKey(status));

        public static string VerificationLabel(string? status) => VerificationKey(status) switch
        {
            "Unverified" => "Not verified",
            "PendingReview" => "Under review",
            var key => Label(key)
        };
    }
}
