using System.Collections.Generic;

namespace InternshipManagement.Application.DTOs.Student
{
    /// <summary>One internship the student was compared with, and what their profile does not show yet.</summary>
    public class SkillGapTarget
    {
        public int InternshipId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Company { get; set; }
        public int? MatchScore { get; set; }

        public List<string> MissingRequiredSkills { get; set; } = new();
        public List<string> MissingPreferredSkills { get; set; } = new();
        public List<string> MissingRequiredQualifications { get; set; } = new();
        public List<string> MissingPreferredQualifications { get; set; } = new();
    }

    /// <summary>A missing skill or qualification, counted across the internships compared.</summary>
    public class SkillGapItem
    {
        public string Term { get; set; } = string.Empty;

        /// <summary>"Skill" or "Qualification".</summary>
        public string Kind { get; set; } = "Skill";

        /// <summary>"Required" if any internship requires it, otherwise "Preferred".</summary>
        public string Importance { get; set; } = "Required";

        /// <summary>In how many of the compared internships it is missing.</summary>
        public int InternshipCount { get; set; }
    }

    /// <summary>A certification from the platform's curated catalogue that covers some of the gaps.</summary>
    public class CertificationRecommendation
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;

        /// <summary>The gaps this certification covers.</summary>
        public List<string> Covers { get; set; } = new();

        /// <summary>AI-written reason in plain words. Null when the AI part was not used or its text was rejected.</summary>
        public string? Why { get; set; }
    }

    public class SkillGapResponse
    {
        public List<SkillGapTarget> Targets { get; set; } = new();
        public List<SkillGapItem> Gaps { get; set; } = new();
        public List<CertificationRecommendation> Recommendations { get; set; } = new();

        /// <summary>Short AI overview. Null when not requested or unavailable.</summary>
        public string? AiSummary { get; set; }

        /// <summary>False when the AI text was not requested or the AI service was down. Everything else is still valid.</summary>
        public bool AiAvailable { get; set; }

        public List<string> Notes { get; set; } = new();
    }

    // ---- n8n contract (workflow path "skills/gaps") -----------------------------------------------
    // No student data is sent: only the public internship titles, the missing terms and catalogue entries.

    public class N8nSkillGapRequest
    {
        public List<string> Targets { get; set; } = new();
        public List<N8nGapTerm> Gaps { get; set; } = new();
        public List<N8nCertOption> Certifications { get; set; } = new();
    }

    public class N8nGapTerm
    {
        public string Term { get; set; } = string.Empty;
        public string Importance { get; set; } = string.Empty;
        public int Internships { get; set; }
    }

    public class N8nCertOption
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public List<string> Covers { get; set; } = new();
    }

    /// <summary>What n8n sends back. UNTRUSTED: only catalogue ids from the request are accepted, text is cleaned.</summary>
    public class N8nSkillGapPayload
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(LenientStringConverter))]
        public string? Summary { get; set; }

        public List<N8nSkillGapItem>? Items { get; set; }
    }

    public class N8nSkillGapItem
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(LenientStringConverter))] public string? Id { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(LenientStringConverter))] public string? Why { get; set; }
    }
}
