using InternshipManagement.Application.DTOs.AiJobs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    /// <summary>The user already has a job of this type that is still running.</summary>
    public sealed class AiJobConflictException : Exception
    {
        public AiJobConflictException(string message) : base(message) { }
    }

    /// <summary>The server has too many jobs waiting right now. Try again shortly.</summary>
    public sealed class AiJobBusyException : Exception
    {
        public AiJobBusyException(string message) : base(message) { }
    }

    /// <summary>One queued piece of work. Lives only in memory (it may hold CV text, which is never stored).</summary>
    public sealed class AiJobWork
    {
        public int JobId { get; init; }
        public int UserId { get; init; }
        public string JobType { get; init; } = string.Empty;

        /// <summary>Runs the slow part. Gets the worker's own service scope and returns the result object.</summary>
        public Func<IServiceProvider, CancellationToken, Task<object>> Run { get; init; } = null!;
    }

    /// <summary>Hands work to the background worker (implemented in Infrastructure).</summary>
    public interface IAiJobQueue
    {
        /// <summary>False when the queue is full.</summary>
        bool TryEnqueue(AiJobWork work);
    }

    public interface IAiJobService
    {
        /// <summary>
        /// Checks the PDF and removes contact details now (so a bad file fails at once), then queues the slow AI part.
        /// </summary>
        /// <exception cref="InvalidOperationException">Bad input: no profile, not a PDF, no readable text.</exception>
        /// <exception cref="AiJobConflictException">The student already has a CV analysis running.</exception>
        /// <exception cref="AiJobBusyException">The queue is full.</exception>
        Task<AiJobResponse> StartCvAnalysisAsync(int userId, byte[] pdfBytes, CancellationToken cancellationToken = default);

        Task<AiJobResponse> StartMatchExplanationAsync(int userId, int maxResults, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks the internship (if given) and the profile now, then queues the AI part of the skill gap analysis.
        /// </summary>
        /// <exception cref="InvalidOperationException">No profile, or the internship is missing or no longer open.</exception>
        Task<AiJobResponse> StartSkillGapAnalysisAsync(int userId, int? internshipId, CancellationToken cancellationToken = default);

        /// <summary>Null when the job does not exist or belongs to someone else.</summary>
        Task<AiJobResponse?> GetAsync(int userId, int jobId, CancellationToken cancellationToken = default);
    }
}
