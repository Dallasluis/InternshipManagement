using InternshipManagement.Application.DTOs.Company;
using InternshipManagement.Application.DTOs.Student;

namespace InternshipManagement.Application.Interfaces
{
    public interface ICompanyChatService
    {
        /// <summary>
        /// Validates the message and history, applies the limits and prepares the turn. Nothing is sent to the AI until the
        /// session is read, and nothing is stored. The session type is shared with the student assistant.
        /// </summary>
        /// <exception cref="InvalidOperationException">Bad input or no company profile (maps to 400).</exception>
        /// <exception cref="CareerChatLimitException">Limits reached (maps to 429).</exception>
        Task<ICareerChatSession> BeginAsync(int userId, CareerChatRequest request, CancellationToken cancellationToken = default);
    }

    public interface ICompanyAgentToolsService
    {
        Task<CompanyOverview> GetOverviewAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<CompanyInternshipSummary>> GetInternshipsAsync(int userId, CancellationToken cancellationToken = default);
        Task<CompanyApplicantsResult> GetApplicantsAsync(int userId, int internshipId, CancellationToken cancellationToken = default);
    }
}
