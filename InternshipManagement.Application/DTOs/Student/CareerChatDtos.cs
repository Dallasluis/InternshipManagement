using System.Collections.Generic;

namespace InternshipManagement.Application.DTOs.Student
{
    /// <summary>One message from the student plus the conversation so far. The API stores nothing: the app keeps the history.</summary>
    public class CareerChatRequest
    {
        /// <summary>The new message (1 to 1000 characters).</summary>
        public string? Message { get; set; }

        /// <summary>Earlier turns, oldest first. Only the last 10 are used.</summary>
        public List<CareerChatTurn>? History { get; set; }
    }

    public class CareerChatTurn
    {
        /// <summary>"user" or "assistant".</summary>
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    // ---- n8n contract (workflow path "career/chat") -------------------------------------------------

    public class N8nCareerChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<N8nChatTurn> History { get; set; } = new();

        /// <summary>Short-lived token the assistant's tools send back (purpose "chat", role "Student").</summary>
        public string RunToken { get; set; } = string.Empty;
    }

    public class N8nChatTurn
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    // ---- what the assistant's profile tool returns ----------------------------------------------------

    /// <summary>
    /// A deliberately small view of the profile for the assistant: no name, contact details, address, links or documents.
    /// </summary>
    public class StudentCareerSummary
    {
        public string? Programme { get; set; }
        public string? YearOfStudy { get; set; }
        public string? ExpectedGraduationYear { get; set; }
        public List<CareerSkill> Skills { get; set; } = new();
        public List<CareerEducation> Education { get; set; } = new();
        public List<CareerCertification> Certifications { get; set; } = new();
        public List<CareerWork> WorkExperience { get; set; } = new();
        public List<string> Projects { get; set; } = new();
    }

    public class CareerSkill { public string Name { get; set; } = string.Empty; public string? Level { get; set; } }
    public class CareerEducation { public string? Degree { get; set; } public string? FieldOfStudy { get; set; } public string Status { get; set; } = string.Empty; }
    public class CareerCertification { public string Name { get; set; } = string.Empty; public string? Issuer { get; set; } }
    public class CareerWork { public string Position { get; set; } = string.Empty; public string? Company { get; set; } }
}
