using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>
    /// Status and result of a background AI job. Only the user who started the job can read it (others get 404).
    /// Not rate limited: polling every 2 to 3 seconds is expected.
    /// </summary>
    [ApiController]
    [Route("api/ai-jobs")]
    [Authorize]
    public class AiJobsController : ControllerBase
    {
        private readonly IAiJobService _jobs;

        public AiJobsController(IAiJobService jobs)
        {
            _jobs = jobs;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            var job = await _jobs.GetAsync(userId, id, cancellationToken);
            return job == null ? NotFound() : Ok(job);
        }
    }
}
