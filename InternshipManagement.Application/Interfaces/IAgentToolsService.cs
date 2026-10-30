namespace InternshipManagement.Application.Interfaces
{
    // ---- Step 1: reference data (not user-specific) -------------------------------------------

    public sealed class PostingOptionsResponse
    {
        public string[] InternshipTypes { get; set; } = Array.Empty<string>();
        public string[] Durations { get; set; } = Array.Empty<string>();
        /// <summary>Programmes / fields of study already used on the platform (examples, not a closed list).</summary>
        public string[] KnownProgrammes { get; set; } = Array.Empty<string>();
    }

    public sealed class SkillVocabularyResponse
    {
        /// <summary>Skill terms already used by students and postings, most common first.</summary>
        public string[] Skills { get; set; } = Array.Empty<string>();
    }

    // ---- Step 2: self-check, duplicate check, suggestions --------------------------------------

    /// <summary>The draft fields the agent wants checked. Everything is optional text: it comes from the model.</summary>
    public sealed class ValidateInternshipRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        /// <summary>Comma-separated terms.</summary>
        public string? RequiredSkills { get; set; }
        /// <summary>Comma-separated terms.</summary>
        public string? PreferredSkills { get; set; }
        public string? InternshipType { get; set; }
        public string? Duration { get; set; }
    }

    public sealed class DraftIssue
    {
        public string Field { get; set; } = string.Empty;
        public string Problem { get; set; } = string.Empty;
        public string Fix { get; set; } = string.Empty;
    }

    public sealed class ValidateInternshipResponse
    {
        public bool IsValid { get; set; }
        public List<DraftIssue> Issues { get; set; } = new();
    }

    public sealed class DuplicateMatch
    {
        /// <summary>"internship" (already created) or "draft" (saved, unpublished).</summary>
        public string Kind { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        /// <summary>Draft, Published, Closed, Filled, or "Saved draft".</summary>
        public string Status { get; set; } = string.Empty;
        public int SimilarityPercent { get; set; }
    }

    public sealed class DuplicateCheckResponse
    {
        public bool HasPossibleDuplicates { get; set; }
        /// <summary>Only the calling company's own postings and drafts, best match first (max 5).</summary>
        public List<DuplicateMatch> Matches { get; set; } = new();
    }

    public sealed class CompanyPostingDefaults
    {
        public string? InternshipType { get; set; }
        public string? Duration { get; set; }
        public string? Location { get; set; }
        public bool? IsRemote { get; set; }
    }

    public sealed class PostingSuggestionsResponse
    {
        /// <summary>Skills used by at least two postings whose title shares a word with the role.</summary>
        public string[] RelatedSkills { get; set; } = Array.Empty<string>();
        /// <summary>Skills used by at least two postings in the same industry.</summary>
        public string[] IndustrySkills { get; set; } = Array.Empty<string>();
        /// <summary>What this company chose in its recent postings (null when it has none).</summary>
        public CompanyPostingDefaults? CompanyDefaults { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// Read-only tools for the AI agent. Reference data is the same for everyone; the duplicate check and
    /// suggestions are scoped to the company the run token belongs to. Nothing here writes data.
    /// </summary>
    public interface IAgentToolsService
    {
        Task<PostingOptionsResponse> GetPostingOptionsAsync(CancellationToken cancellationToken = default);
        Task<SkillVocabularyResponse> GetSkillVocabularyAsync(CancellationToken cancellationToken = default);

        ValidateInternshipResponse ValidateInternship(ValidateInternshipRequest request);
        Task<DuplicateCheckResponse> CheckDuplicateAsync(int userId, string? title, string? requiredSkills, CancellationToken cancellationToken = default);
        Task<PostingSuggestionsResponse> GetPostingSuggestionsAsync(int userId, string? role, string? industry, CancellationToken cancellationToken = default);
    }
}
