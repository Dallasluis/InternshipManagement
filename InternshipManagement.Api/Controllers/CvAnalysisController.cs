using InternshipManagement.Application.DTOs.Student;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>
    /// CV analysis for students, as a background job (the API never waits for the AI):
    /// 1. POST api/students/cv-analysis          upload a PDF. Bad files are rejected at once (400); otherwise 202 + a job id
    /// 2. GET  api/ai-jobs/{id}                  poll until status is Done (the result holds the SUGGESTIONS, nothing is saved) or Failed
    /// 3. POST api/students/cv-analysis/confirm  send back what the student accepted, it is added to the profile
    /// The CV file and its text are never stored.
    /// </summary>
    [ApiController]
    [Route("api/students/cv-analysis")]
    [Authorize(Roles = "Student")]
    public class CvAnalysisController : ControllerBase
    {
        private const long MaxPdfBytes = 5 * 1024 * 1024;

        private readonly IAiJobService _jobs;
        private readonly ICvAnalysisService _cvAnalysis;

        public CvAnalysisController(IAiJobService jobs, ICvAnalysisService cvAnalysis)
        {
            _jobs = jobs;
            _cvAnalysis = cvAnalysis;
        }

        /// <summary>Multipart form with one PDF in the field "file" (max 5 MB).</summary>
        [HttpPost]
        [EnableRateLimiting("ai-generation")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
        public async Task<IActionResult> Start(IFormFile? file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Error = "Upload a PDF in the form field \"file\"." });

            if (file.Length > MaxPdfBytes)
                return BadRequest(new { Error = "The CV is larger than 5 MB." });

            try
            {
                using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer, cancellationToken);

                var job = await _jobs.StartCvAnalysisAsync(GetUserId(), buffer.ToArray(), cancellationToken);
                Response.Headers.Location = $"/api/ai-jobs/{job.Id}";
                return Accepted(job);
            }
            catch (InvalidOperationException ex)   // no profile, not a PDF, unreadable, no text
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

        /// <summary>Adds the entries the student accepted (and possibly edited) to the profile.</summary>
        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm(
            [FromBody] ConfirmCvAnalysisRequest request, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _cvAnalysis.ConfirmAsync(GetUserId(), request, cancellationToken));
            }
            catch (InvalidOperationException ex)   // validation: missing dates, empty names, too many items
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        private int GetUserId() => int.Parse(User.FindFirst("userId")?.Value ?? "0");
    }
}
