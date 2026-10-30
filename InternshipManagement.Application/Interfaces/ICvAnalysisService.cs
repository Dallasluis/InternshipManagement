using InternshipManagement.Application.DTOs.Student;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    /// <summary>Thrown when n8n/Gemini answered, but nothing usable came out of the CV.</summary>
    public sealed class CvAnalysisRejectedException : Exception
    {
        public CvAnalysisRejectedException(string message) : base(message) { }
    }

    /// <summary>Thrown by an IPdfTextExtractor when the file is corrupt, encrypted or not a PDF.</summary>
    public sealed class PdfUnreadableException : Exception
    {
        public PdfUnreadableException(string message, Exception? inner = null) : base(message, inner) { }
    }

    /// <summary>Reads the text layer of a PDF. Implemented in Infrastructure so Application has no PDF library.</summary>
    public interface IPdfTextExtractor
    {
        /// <exception cref="PdfUnreadableException">The bytes are not a readable PDF.</exception>
        string ExtractText(byte[] pdf, int maxPages);
    }

    public interface ICvAnalysisService
    {
        /// <summary>
        /// Reads the CV, asks the AI for suggested profile entries and compares them with the profile.
        /// Nothing is stored: not the file, not the text, not the suggestions.
        /// </summary>
        Task<CvAnalysisResponse> AnalyzeAsync(int userId, byte[] pdfBytes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Fast half of the analysis: validates the PDF, reads it and removes contact details. Returns the cleaned text
        /// (held in memory only). Throws InvalidOperationException with a user-facing message for bad input.
        /// </summary>
        Task<string> PrepareAsync(int userId, byte[] pdfBytes, CancellationToken cancellationToken = default);

        /// <summary>Slow half: sends the cleaned text to the AI and returns validated suggestions.</summary>
        Task<CvAnalysisResponse> AnalyzeTextAsync(int userId, string cvText, CancellationToken cancellationToken = default);

        /// <summary>Adds the entries the student accepted, using the existing profile add logic.</summary>
        Task<ConfirmCvAnalysisResponse> ConfirmAsync(int userId, ConfirmCvAnalysisRequest request, CancellationToken cancellationToken = default);
    }

    // ---- Student-side agent tools (read-only, reference data only) ------------------------------

    public sealed class CertificationVocabularyResponse
    {
        /// <summary>Certification names already on student profiles, most common first.</summary>
        public string[] Certifications { get; set; } = Array.Empty<string>();
    }

    public interface ICvAgentToolsService
    {
        Task<CertificationVocabularyResponse> GetCertificationVocabularyAsync(CancellationToken cancellationToken = default);
    }
}
