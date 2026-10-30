using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>
    /// The student's best internship matches with an AI-written explanation for each, as a background job.
    /// POST starts it (202 + job id), GET api/ai-jobs/{id} returns the result when Done.
    /// The plain ranking without explanations stays at GET api/Matching/student/internships (instant, unchanged).
    /// </summary>
    [ApiController]
    [Route("api/Matching/student/internships/explained")]
    [Authorize(Roles = "Student")]
    public class MatchExplanationController : ControllerBase
    {
        private readonly IAiJobService _jobs;

        public MatchExplanationController(IAiJobService jobs)
        {
            _jobs = jobs;
        }

        [HttpPost]
        [EnableRateLimiting("ai-generation")]
        public async Task<IActionResult> Start([FromQuery] int maxResults = 5, CancellationToken cancellationToken = default)
        {
            try
            {
                var job = await _jobs.StartMatchExplanationAsync(GetUserId(), maxResults, cancellationToken);
                Response.Headers.Location = $"/api/ai-jobs/{job.Id}";
                return Accepted(job);
            }
            catch (AiJobConflictException ex)
            {
                return Conflict(new { Error = ex.Message });
            }
            catch (AiJobBusyException ex)
            {
                Response.Headers.RetryAfter = "60";
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { Error = ex.Message });
            }
        }

        private int GetUserId() => int.Parse(User.FindFirst("userId")?.Value ?? "0");
    }
}
