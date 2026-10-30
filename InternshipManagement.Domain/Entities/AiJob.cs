using InternshipManagement.Domain.Common;
using System;

namespace InternshipManagement.Domain.Entities
{
    public static class AiJobTypes
    {
        public const string CvAnalysis = "CvAnalysis";
        public const string MatchExplanation = "MatchExplanation";
        public const string SkillGapAnalysis = "SkillGapAnalysis";
    }

    public static class AiJobStatuses
    {
        public const string Pending = "Pending";
        public const string Running = "Running";
        public const string Done = "Done";
        public const string Failed = "Failed";
    }

    /// <summary>
    /// One slow AI task (CV analysis, match explanations) that runs in the background so the HTTP request
    /// that started it does not have to wait for n8n/Gemini. The row holds the status and, when done, the
    /// RESULT as JSON (suggestions only). It never holds the CV file or its text. Rows are deleted a few
    /// hours after they were created.
    /// </summary>
    public class AiJob : BaseEntity
    {
        /// <summary>Owner. Only this user can read the job.</summary>
        public int UserId { get; set; }

        public string JobType { get; set; } = string.Empty;
        public string Status { get; set; } = AiJobStatuses.Pending;

        /// <summary>The finished result, same shape as the old synchronous response. Null until Done.</summary>
        public string? ResultJson { get; set; }

        /// <summary>A short message for the user when Failed. Never contains CV text or stack traces.</summary>
        public string? Error { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
