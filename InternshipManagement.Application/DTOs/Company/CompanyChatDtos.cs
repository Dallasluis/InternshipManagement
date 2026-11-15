using InternshipManagement.Application.DTOs.Student;

namespace InternshipManagement.Application.DTOs.Company
{
    // ---- n8n contract (workflow path "company/chat") ------------------------------------------------

    public class N8nCompanyChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<N8nChatTurn> History { get; set; } = new();

        /// <summary>Short-lived token the assistant's tools send back (purpose "company-chat", role "Company").</summary>
        public string RunToken { get; set; } = string.Empty;
    }

    // ---- what the assistant's tools return ----------------------------------------------------------

    /// <summary>A small summary of the company's own activity. No contact details, documents or admin notes.</summary>
    public class CompanyOverview
    {
        public string CompanyName { get; set; } = string.Empty;
        public string? Industry { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;
        public Dictionary<string, int> InternshipsByStatus { get; set; } = new();
        public Dictionary<string, int> ApplicationsByStatus { get; set; } = new();
        public Dictionary<string, int> PlacementsByStatus { get; set; } = new();
    }

    public class CompanyInternshipSummary
    {
        public int InternshipId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ApplicationDeadline { get; set; } = string.Empty;
        public int Positions { get; set; }
        public int Views { get; set; }
        public int TotalApplications { get; set; }
        public int NewApplications { get; set; }
    }

    /// <summary>One applicant, identified only by the application number: no name, contact details, documents or cover letter.</summary>
    public class CompanyApplicantSummary
    {
        public int ApplicationId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AppliedOn { get; set; } = string.Empty;
        public string? Programme { get; set; }
        public string? YearOfStudy { get; set; }
        public List<string> Skills { get; set; } = new();

        /// <summary>The platform's match score (0 to 100), or null when the applicant matches none of the required skills.</summary>
        public int? MatchScore { get; set; }
        public List<string> MatchingSkills { get; set; } = new();
    }

    public class CompanyApplicantsResult
    {
        public int InternshipId { get; set; }
        public string InternshipTitle { get; set; } = string.Empty;
        public int TotalApplications { get; set; }
        public bool ShowingNewestOnly { get; set; }
        public List<CompanyApplicantSummary> Applicants { get; set; } = new();
    }
}
