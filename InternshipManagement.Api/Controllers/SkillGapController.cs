using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>
    /// What is missing from a student's profile for an internship, and which certifications could close the gap.
    /// internshipId is optional: without it the student's top matches are compared.
    /// </summary>
    [ApiController]
    [Route("api/students/skill-gaps")]
    [Authorize(Roles = "Student")]
    public class SkillGapController : ControllerBase
    {
        private readonly ISkillGapService _gaps;
        private readonly IAiJobService _jobs;

        public SkillGapController(ISkillGapService gaps, IAiJobService jobs)
        {
            _gaps = gaps;
            _jobs = jobs;
        }

        /// <summary>Instant: gaps and suggested certifications from code only, no AI text.</summary>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? internshipId, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _gaps.GetGapsAsync(GetUserId(), internshipId, cancellationToken));
            }
            catch (InvalidOperationException ex)   // no profile, internship missing or closed
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>Background job: the same result plus an AI summary and a reason per certification. 202 + job id.</summary>
        [HttpPost]
        [EnableRateLimiting("ai-generation")]
        public async Task<IActionResult> Start([FromQuery] int? internshipId, CancellationToken cancellationToken)
        {
            try
            {
                var job = await _jobs.StartSkillGapAnalysisAsync(GetUserId(), internshipId, cancellationToken);
                Response.Headers.Location = $"/api/ai-jobs/{job.Id}";
                return Accepted(job);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
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
