using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InternshipManagement.Application.DTOs.Student
{
    // =====================================================================================
    // 1. API <-> n8n contract ("cv/analyze" workflow)
    // =====================================================================================

    /// <summary>What the API sends to the n8n "cv/analyze" workflow. The CV text has contact details removed.</summary>
    public class N8nCvAnalysisRequest
    {
        public string CvText { get; set; } = string.Empty;
        public string[] AllowedProficiencyLevels { get; set; } = Array.Empty<string>();

        /// <summary>Short-lived token the AI's tools send back to the API (purpose "cv", role "Student").</summary>
        public string RunToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// What n8n sends back. UNTRUSTED: everything is nullable and is cleaned and validated in
    /// CvAnalysisService before the student sees it. Dates arrive as text ("2021-03" or "2021").
    /// </summary>
    public class N8nCvAnalysisPayload
    {
        public List<N8nCvSkill>? Skills { get; set; }
        public List<N8nCvEducation>? Education { get; set; }
        public List<N8nCvWorkExperience>? WorkExperience { get; set; }
        public List<N8nCvCertification>? Certifications { get; set; }
        public List<N8nCvProject>? Projects { get; set; }
        public List<string>? Notes { get; set; }
    }

    public class N8nCvSkill
    {
        public string? Name { get; set; }
        public string? ProficiencyLevel { get; set; }
    }

    public class N8nCvEducation
    {
        public string? Institution { get; set; }
        public string? Degree { get; set; }
        public string? FieldOfStudy { get; set; }
        [JsonConverter(typeof(LenientStringConverter))] public string? StartDate { get; set; }
        [JsonConverter(typeof(LenientStringConverter))] public string? EndDate { get; set; }
        public bool? IsCurrent { get; set; }
        [JsonConverter(typeof(LenientStringConverter))] public string? Grade { get; set; }
        public string? CompletionStatus { get; set; }
    }

    public class N8nCvWorkExperience
    {
        public string? Company { get; set; }
        public string? Position { get; set; }
        public string? Description { get; set; }
        [JsonConverter(typeof(LenientStringConverter))] public string? StartDate { get; set; }
        [JsonConverter(typeof(LenientStringConverter))] public string? EndDate { get; set; }
        public bool? IsCurrent { get; set; }
    }

    public class N8nCvCertification
    {
        public string? Name { get; set; }
        public string? IssuingOrganization { get; set; }
        [JsonConverter(typeof(LenientStringConverter))] public string? IssueDate { get; set; }
        [JsonConverter(typeof(LenientStringConverter))] public string? ExpirationDate { get; set; }
    }

    public class N8nCvProject
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Role { get; set; }
        public string? Technologies { get; set; }
    }

    /// <summary>
    /// Models sometimes send 2021 (a number) where text was asked for. Reads strings, numbers and
    /// booleans as text and ignores anything else, so one sloppy field does not fail the whole analysis.
    /// </summary>
    public sealed class LenientStringConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Number:
                    return reader.GetDouble().ToString("0.##", CultureInfo.InvariantCulture);
                case JsonTokenType.True:
                    return "true";
                case JsonTokenType.False:
                    return "false";
                case JsonTokenType.Null:
                    return null;
                default:
                    reader.Skip();
                    return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
            => writer.WriteStringValue(value);
    }

    // =====================================================================================
    // 2. What the student's screen gets (suggestions only, nothing is saved yet)
    // =====================================================================================

    public static class CvSuggestionStatus
    {
        /// <summary>Not on the student's profile yet.</summary>
        public const string New = "New";

        /// <summary>The profile already has an entry with the same name. Shown for information, not re-added.</summary>
        public const string AlreadyOnProfile = "AlreadyOnProfile";
    }

    public class CvSuggestedSkill
    {
        public string Name { get; set; } = string.Empty;
        public string? ProficiencyLevel { get; set; }
        public string Status { get; set; } = CvSuggestionStatus.New;
    }

    public class CvSuggestedEducation
    {
        public string Institution { get; set; } = string.Empty;
        public string Degree { get; set; } = string.Empty;
        public string FieldOfStudy { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public string? Grade { get; set; }
        public string? CompletionStatus { get; set; }
        public string Status { get; set; } = CvSuggestionStatus.New;

        /// <summary>The CV gave no usable start date. The student must fill it in before this can be added.</summary>
        public bool NeedsStartDate => StartDate is null;
    }

    public class CvSuggestedWorkExperience
    {
        public string Company { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public string Status { get; set; } = CvSuggestionStatus.New;

        /// <summary>The CV gave no usable start date. The student must fill it in before this can be added.</summary>
        public bool NeedsStartDate => StartDate is null;
    }

    public class CvSuggestedCertification
    {
        public string Name { get; set; } = string.Empty;
        public string IssuingOrganization { get; set; } = string.Empty;
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string Status { get; set; } = CvSuggestionStatus.New;
    }

    public class CvSuggestedProject
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Role { get; set; }
        public string? Technologies { get; set; }
        public string Status { get; set; } = CvSuggestionStatus.New;
    }

    /// <summary>Items already on the profile that this CV does not mention. Information only: nothing is ever removed.</summary>
    public class CvNotMentioned
    {
        public List<string> Skills { get; set; } = new();
        public List<string> Education { get; set; } = new();
        public List<string> WorkExperience { get; set; } = new();
        public List<string> Certifications { get; set; } = new();
        public List<string> Projects { get; set; } = new();
    }

    public class CvAnalysisResponse
    {
        public List<CvSuggestedSkill> Skills { get; set; } = new();
        public List<CvSuggestedEducation> Education { get; set; } = new();
        public List<CvSuggestedWorkExperience> WorkExperience { get; set; } = new();
        public List<CvSuggestedCertification> Certifications { get; set; } = new();
        public List<CvSuggestedProject> Projects { get; set; } = new();

        /// <summary>On the profile already, but not found in this CV. The student decides what to do with them.</summary>
        public CvNotMentioned NotMentionedInCv { get; set; } = new();

        /// <summary>Short notes from the AI for the student, e.g. "No end date found for the job at X".</summary>
        public List<string> Notes { get; set; } = new();
    }

    // =====================================================================================
    // 3. Confirm step: the student sends back only what they accepted (and edited)
    // =====================================================================================

    /// <summary>
    /// Reuses the existing Add*Request shapes, so confirming goes through exactly the same
    /// IStudentService.Add*Async methods as adding an entry by hand.
    /// </summary>
    public class ConfirmCvAnalysisRequest
    {
        public List<AddSkillRequest>? Skills { get; set; }
        public List<AddEducationRequest>? Education { get; set; }
        public List<AddWorkExperienceRequest>? WorkExperience { get; set; }
        public List<AddCertificationRequest>? Certifications { get; set; }
        public List<AddProjectRequest>? Projects { get; set; }
    }

    public class ConfirmCvAnalysisResponse
    {
        public int SkillsAdded { get; set; }
        public int EducationAdded { get; set; }
        public int WorkExperienceAdded { get; set; }
        public int CertificationsAdded { get; set; }
        public int ProjectsAdded { get; set; }

        /// <summary>Items skipped because the profile (or the same request) already had them.</summary>
        public int SkippedAsDuplicate { get; set; }
    }
}
