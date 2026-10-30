using InternshipManagement.Api.Filters;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>
    /// Read-only endpoints for the career assistant's MCP tools. Not for the frontend and not reachable with a
    /// login token: they need a run token with purpose "chat" and role "Student". The student always comes from
    /// the token, never from the request, so the model cannot ask about another student.
    /// </summary>
    [ApiController]
    [Route("api/agent-tools/chat")]
    [RunTokenAuth("chat", requiredRole: "Student")]
    public class ChatAgentToolsController : ControllerBase
    {
        private readonly ICareerAgentToolsService _profile;
        private readonly IMatchingService _matching;
        private readonly ISkillGapService _gaps;
        private readonly ILogger<ChatAgentToolsController> _logger;

        public ChatAgentToolsController(
            ICareerAgentToolsService profile, IMatchingService matching, ISkillGapService gaps,
            ILogger<ChatAgentToolsController> logger)
        {
            _profile = profile;
            _matching = matching;
            _gaps = gaps;
            _logger = logger;
        }

        /// <summary>Programme, year, skills, education, certifications, jobs and project titles. No name or contact details.</summary>
        [HttpGet("profile-summary")]
        public async Task<IActionResult> ProfileSummary(CancellationToken cancellationToken)
        {
            var claims = Claims();
            _logger.LogInformation("Chat tool profile-summary called for user {UserId}", claims.UserId);

            try
            {
                return Ok(await _profile.GetProfileSummaryAsync(claims.UserId, cancellationToken));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }

        /// <summary>The student's best open internships, ranked by the platform.</summary>
        [HttpGet("my-matches")]
        public async Task<IActionResult> MyMatches([FromQuery] int maxResults = 5)
        {
            var claims = Claims();
            _logger.LogInformation("Chat tool my-matches called for user {UserId}", claims.UserId);

            return Ok(await _matching.RecommendInternshipsForStudentAsync(claims.UserId, Math.Clamp(maxResults, 1, 10)));
        }

        /// <summary>
        /// What the student's profile lacks for one open internship, or (internshipId 0 or less) for their top matches,
        /// plus certifications from the platform's list that could help.
        /// </summary>
        [HttpGet("skill-gaps")]
        public async Task<IActionResult> SkillGaps([FromQuery] int internshipId = 0, CancellationToken cancellationToken = default)
        {
            var claims = Claims();
            _logger.LogInformation("Chat tool skill-gaps called for user {UserId}", claims.UserId);

            try
            {
                return Ok(await _gaps.GetGapsAsync(claims.UserId, internshipId > 0 ? internshipId : null, cancellationToken));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }

        // RunTokenAuth has already rejected the request if this is missing.
        private RunTokenClaims Claims() => (RunTokenClaims)HttpContext.Items[RunTokenAuthAttribute.ItemKey]!;
    }
}
