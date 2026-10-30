using System;
using System.Text.Json;

namespace InternshipManagement.Application.DTOs.AiJobs
{
    /// <summary>What the app gets when it starts a job and every time it asks how the job is doing.</summary>
    public class AiJobResponse
    {
        public int Id { get; set; }

        /// <summary>CvAnalysis or MatchExplanation.</summary>
        public string JobType { get; set; } = string.Empty;

        /// <summary>Pending, Running, Done or Failed.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Only when Done: the same JSON the old synchronous endpoint returned.</summary>
        public JsonElement? Result { get; set; }

        /// <summary>Only when Failed: a short message that can be shown to the user.</summary>
        public string? Error { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
