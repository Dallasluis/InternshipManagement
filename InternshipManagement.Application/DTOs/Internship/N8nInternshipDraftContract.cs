using System.Collections.Generic;

namespace InternshipManagement.Application.DTOs.Internship
{
    /// <summary>What the API sends to the n8n "internship/generate" workflow.</summary>
    public class N8nInternshipDraftRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public string? CompanyIndustry { get; set; }
        public string[] AllowedInternshipTypes { get; set; } = System.Array.Empty<string>();
        public string[] AllowedDurations { get; set; } = System.Array.Empty<string>();

        /// <summary>Short-lived token the AI's tools send back to the API (see IRunTokenService). Not a login token.</summary>
        public string RunToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// What n8n sends back. Treated as UNTRUSTED: every field is nullable and is cleaned and
    /// validated in InternshipDraftService before anything reaches the user.
    /// </summary>
    public class N8nInternshipDraftPayload
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public List<string>? Responsibilities { get; set; }
        public List<string>? RequiredQualifications { get; set; }
        public List<string>? PreferredQualifications { get; set; }
        public List<string>? RequiredSkills { get; set; }
        public List<string>? PreferredSkills { get; set; }
        public string? Industry { get; set; }
        public string? Location { get; set; }
        public bool? IsRemote { get; set; }
        public string? InternshipType { get; set; }
        public string? Duration { get; set; }
        public List<string>? EligibleProgrammes { get; set; }
        public List<string>? MissingDetails { get; set; }
    }
}
