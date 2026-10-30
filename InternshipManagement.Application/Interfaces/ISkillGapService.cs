using InternshipManagement.Application.DTOs.Student;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    public interface ISkillGapService
    {
        /// <summary>
        /// Compares the student's profile with one open internship (or, when internshipId is null, with their top
        /// matches) and suggests catalogue certifications that cover the gaps. Database and code only: instant, no AI.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">No profile, or the internship is missing or no longer open.</exception>
        Task<SkillGapResponse> GetGapsAsync(int userId, int? internshipId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Same result plus an AI-written summary and a reason per certification. If the AI is unavailable, the
        /// deterministic result is still returned with AiAvailable = false.
        /// </summary>
        Task<SkillGapResponse> AnalyzeAsync(int userId, int? internshipId, CancellationToken cancellationToken = default);
    }
}
