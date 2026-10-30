using InternshipManagement.Application.DTOs.Student;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InternshipManagement.Application.DTOs.Matching
{
    /// <summary>What the API sends to the n8n "matching/explain" workflow. No student data: the AI's tool fetches the matches itself.</summary>
    public class N8nMatchExplainRequest
    {
        public int MaxResults { get; set; }

        /// <summary>Short-lived token the AI's tool sends back to the API (purpose "match", role "Student").</summary>
        public string RunToken { get; set; } = string.Empty;
    }

    /// <summary>What n8n sends back. UNTRUSTED: cleaned and matched to real internship ids before the student sees it.</summary>
    public class N8nMatchExplainPayload
    {
        public List<N8nMatchExplanation>? Explanations { get; set; }
    }

    public class N8nMatchExplanation
    {
        [JsonConverter(typeof(LenientStringConverter))] public string? TargetId { get; set; }
        [JsonConverter(typeof(LenientStringConverter))] public string? Explanation { get; set; }
    }

    /// <summary>A match from the existing matching service, plus an AI-written explanation in plain words.</summary>
    public class ExplainedMatchResponse : MatchRecommendationResponse
    {
        /// <summary>Null when the AI part was unavailable or its text was rejected. Score and skill lists never come from the AI.</summary>
        public string? AiExplanation { get; set; }
    }

    public class ExplainedMatchesResponse
    {
        /// <summary>False when the AI service was down: the matches are still returned, only without AiExplanation.</summary>
        public bool AiExplanationsAvailable { get; set; }
        public List<ExplainedMatchResponse> Matches { get; set; } = new();
    }
}
