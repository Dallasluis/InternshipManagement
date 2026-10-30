using InternshipManagement.Api.Filters;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>
    /// Read-only endpoint for the match-explanation agent's MCP tool. Not for the frontend and not reachable
    /// with a login token: it needs a run token with purpose "match" and role "Student". The student always
    /// comes from the token, never from the request, so the model cannot ask about another student.
    /// </summary>
    [ApiController]
    [Route("api/agent-tools/matching")]
    [RunTokenAuth("match", requiredRole: "Student")]
    public class MatchAgentToolsController : ControllerBase
    {
        private readonly IMatchingService _matching;
        private readonly ILogger<MatchAgentToolsController> _logger;

        public MatchAgentToolsController(IMatchingService matching, ILogger<MatchAgentToolsController> logger)
        {
            _matching = matching;
            _logger = logger;
        }

        /// <summary>The token owner's best matches: title, score, matching skills, missing preferred skills.</summary>
        [HttpGet("my-matches")]
        public async Task<IActionResult> MyMatches([FromQuery] int maxResults = 5, CancellationToken cancellationToken = default)
        {
            // RunTokenAuth has already rejected the request if this is missing.
            var claims = (RunTokenClaims)HttpContext.Items[RunTokenAuthAttribute.ItemKey]!;
            _logger.LogInformation("Match agent tool my-matches called for user {UserId} ({Purpose})", claims.UserId, claims.Purpose);

            var matches = await _matching.RecommendInternshipsForStudentAsync(claims.UserId, Math.Clamp(maxResults, 1, 10));
            return Ok(matches);
        }
    }
}
