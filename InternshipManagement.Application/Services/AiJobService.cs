using InternshipManagement.Application.DTOs.AiJobs;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Services
{
    public class AiJobService : IAiJobService
    {
        /// <summary>
        /// A job that is still Pending/Running after this long is treated as lost (for example the API
        /// restarted while it ran). Longer than the worker's own 4 minute limit.
        /// </summary>
        private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(6);

        private readonly IApplicationDbContext _context;
        private readonly ICvAnalysisService _cv;
        private readonly ISkillGapService _gaps;
        private readonly IAiJobQueue _queue;

        public AiJobService(IApplicationDbContext context, ICvAnalysisService cv, ISkillGapService gaps, IAiJobQueue queue)
        {
            _context = context;
            _cv = cv;
            _gaps = gaps;
            _queue = queue;
        }

        public async Task<AiJobResponse> StartCvAnalysisAsync(
            int userId, byte[] pdfBytes, CancellationToken cancellationToken = default)
        {
            await EnsureNoActiveJobAsync(userId, AiJobTypes.CvAnalysis, cancellationToken);

            // Fast part, done now so a bad file is rejected immediately. The text stays in memory only.
            var cvText = await _cv.PrepareAsync(userId, pdfBytes, cancellationToken);

            var job = await CreateJobAsync(userId, AiJobTypes.CvAnalysis, cancellationToken);

            await EnqueueAsync(job, async (sp, token) =>
            {
                var service = (ICvAnalysisService)sp.GetService(typeof(ICvAnalysisService))!;
                return (object)await service.AnalyzeTextAsync(userId, cvText, token);
            });

            return ToResponse(job);
        }

        public async Task<AiJobResponse> StartMatchExplanationAsync(
            int userId, int maxResults, CancellationToken cancellationToken = default)
        {
            await EnsureNoActiveJobAsync(userId, AiJobTypes.MatchExplanation, cancellationToken);

            var job = await CreateJobAsync(userId, AiJobTypes.MatchExplanation, cancellationToken);

            await EnqueueAsync(job, async (sp, token) =>
            {
                var service = (IMatchExplanationService)sp.GetService(typeof(IMatchExplanationService))!;
                return (object)await service.GetExplainedMatchesAsync(userId, maxResults, token);
            });

            return ToResponse(job);
        }

        public async Task<AiJobResponse> StartSkillGapAnalysisAsync(
            int userId, int? internshipId, CancellationToken cancellationToken = default)
        {
            await EnsureNoActiveJobAsync(userId, AiJobTypes.SkillGapAnalysis, cancellationToken);

            // Fast, database-only check so a missing profile or a closed internship fails at once (400).
            await _gaps.GetGapsAsync(userId, internshipId, cancellationToken);

            var job = await CreateJobAsync(userId, AiJobTypes.SkillGapAnalysis, cancellationToken);

            await EnqueueAsync(job, async (sp, token) =>
            {
                var service = (ISkillGapService)sp.GetService(typeof(ISkillGapService))!;
                return (object)await service.AnalyzeAsync(userId, internshipId, token);
            });

            return ToResponse(job);
        }

        public async Task<AiJobResponse?> GetAsync(int userId, int jobId, CancellationToken cancellationToken = default)
        {
            var job = await _context.AiJobs
                .FirstOrDefaultAsync(j => j.Id == jobId && j.UserId == userId, cancellationToken);

            if (job == null) return null;

            if ((job.Status == AiJobStatuses.Pending || job.Status == AiJobStatuses.Running)
                && job.CreatedAt < DateTime.UtcNow - StaleAfter)
            {
                job.Status = AiJobStatuses.Failed;
                job.Error = "This took too long and was stopped. Please try again.";
                job.CompletedAt = DateTime.UtcNow;
                job.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return ToResponse(job);
        }

        // ---- helpers ---------------------------------------------------------------------------

        private async Task EnsureNoActiveJobAsync(int userId, string jobType, CancellationToken cancellationToken)
        {
            var cutoff = DateTime.UtcNow - StaleAfter;

            var active = await _context.AiJobs.AsNoTracking().AnyAsync(j =>
                j.UserId == userId
                && j.JobType == jobType
                && (j.Status == AiJobStatuses.Pending || j.Status == AiJobStatuses.Running)
                && j.CreatedAt > cutoff,
                cancellationToken);

            if (active)
                throw new AiJobConflictException("You already have one of these running. Wait for it to finish.");
        }

        private async Task<AiJob> CreateJobAsync(int userId, string jobType, CancellationToken cancellationToken)
        {
            var job = new AiJob
            {
                UserId = userId,
                JobType = jobType,
                Status = AiJobStatuses.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId.ToString()
            };

            _context.AiJobs.Add(job);
            await _context.SaveChangesAsync(cancellationToken);
            return job;
        }

        private async Task EnqueueAsync(AiJob job, Func<IServiceProvider, CancellationToken, Task<object>> run)
        {
            var queued = _queue.TryEnqueue(new AiJobWork
            {
                JobId = job.Id,
                UserId = job.UserId,
                JobType = job.JobType,
                Run = run
            });

            if (queued) return;

            job.Status = AiJobStatuses.Failed;
            job.Error = "The server is busy. Please try again in a minute.";
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(CancellationToken.None);

            throw new AiJobBusyException("The server is busy. Please try again in a minute.");
        }

        private static AiJobResponse ToResponse(AiJob job)
        {
            JsonElement? result = null;
            if (job.Status == AiJobStatuses.Done && !string.IsNullOrEmpty(job.ResultJson))
            {
                using var document = JsonDocument.Parse(job.ResultJson);
                result = document.RootElement.Clone();
            }

            return new AiJobResponse
            {
                Id = job.Id,
                JobType = job.JobType,
                Status = job.Status,
                Result = result,
                Error = job.Status == AiJobStatuses.Failed ? job.Error : null,
                CreatedAt = job.CreatedAt,
                CompletedAt = job.CompletedAt
            };
        }
    }
}
