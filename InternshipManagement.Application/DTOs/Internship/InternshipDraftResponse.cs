using System;
using System.Collections.Generic;

namespace InternshipManagement.Application.DTOs.Internship
{
    /// <summary>
    /// A saved posting draft. Field names and formats match CreateInternshipRequest so the front end can
    /// prefill the posting form directly. Nothing becomes an Internship until the company submits the
    /// normal create form.
    /// </summary>
    public class InternshipDraftResponse
    {
        /// <summary>Saved draft id. Use it to reopen, edit or delete the draft.</summary>
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>One responsibility per line, each starting with "- ".</summary>
        public string? Responsibilities { get; set; }

        // Comma-separated short terms, the format MatchingService expects.
        public string? RequiredQualifications { get; set; }
        public string? PreferredQualifications { get; set; }
        public string? RequiredSkills { get; set; }
        public string? PreferredSkills { get; set; }

        public string Industry { get; set; } = string.Empty;
        public string? Location { get; set; }
        public bool IsRemote { get; set; }

        /// <summary>A valid InternshipType name, or null if not set yet.</summary>
        public string? InternshipType { get; set; }

        /// <summary>A valid InternshipDuration name, or null if not set yet.</summary>
        public string? Duration { get; set; }

        public List<string> EligibleProgrammes { get; set; } = new();

        // The AI never invents these. The company fills them in and they are saved with the draft.
        public DateTime? StartDate { get; set; }
        public DateTime? ApplicationDeadline { get; set; }
        public int? NumberOfPositions { get; set; }

        /// <summary>Required CreateInternshipRequest fields still empty. Recomputed on every read.</summary>
        public List<string> FieldsToComplete { get; set; } = new();

        /// <summary>Questions the AI could not answer from the description.</summary>
        public List<string> MissingDetails { get; set; } = new();

        public bool IsAiGenerated { get; set; } = true;
    }

    /// <summary>One row in the "my drafts" list.</summary>
    public class InternshipDraftSummary
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
