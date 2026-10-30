using InternshipManagement.Api.Filters;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>
    /// Read-only endpoints that the AI agent's MCP tools call. They are NOT for the frontend and are not
    /// reachable with a normal login token: they need a run token minted by the API for one AI job.
    /// Nothing here writes data. The user is always taken from the run token, never from the request,
    /// so the model cannot ask about another company.
    /// </summary>
    [ApiController]
    [Route("api/agent-tools")]
    [RunTokenAuth("draft", requiredRole: "Company")]
    public class AgentToolsController : ControllerBase
    {
        private readonly IAgentToolsService _tools;
        private readonly ILogger<AgentToolsController> _logger;

        public AgentToolsController(IAgentToolsService tools, ILogger<AgentToolsController> logger)
        {
            _tools = tools;
            _logger = logger;
        }

        [HttpGet("posting-options")]
        public async Task<IActionResult> PostingOptions(CancellationToken cancellationToken)
        {
            LogCall("posting-options");
            return Ok(await _tools.GetPostingOptionsAsync(cancellationToken));
        }

        [HttpGet("skill-vocabulary")]
        public async Task<IActionResult> SkillVocabulary(CancellationToken cancellationToken)
        {
            LogCall("skill-vocabulary");
            return Ok(await _tools.GetSkillVocabularyAsync(cancellationToken));
        }

        /// <summary>The agent checks its own draft before answering. POST only because the text can be long.</summary>
        [HttpPost("validate-internship")]
        public IActionResult ValidateInternship([FromBody] ValidateInternshipRequest request)
        {
            LogCall("validate-internship");
            return Ok(_tools.ValidateInternship(request));
        }

        [HttpGet("duplicate-check")]
        public async Task<IActionResult> DuplicateCheck(
            [FromQuery] string? title, [FromQuery] string? requiredSkills, CancellationToken cancellationToken)
        {
            var claims = LogCall("duplicate-check");
            return Ok(await _tools.CheckDuplicateAsync(claims.UserId, title, requiredSkills, cancellationToken));
        }

        [HttpGet("posting-suggestions")]
        public async Task<IActionResult> PostingSuggestions(
            [FromQuery] string? role, [FromQuery] string? industry, CancellationToken cancellationToken)
        {
            var claims = LogCall("posting-suggestions");
            return Ok(await _tools.GetPostingSuggestionsAsync(claims.UserId, role, industry, cancellationToken));
        }

        private RunTokenClaims LogCall(string tool)
        {
            // RunTokenAuth has already rejected the request if this is missing.
            var claims = (RunTokenClaims)HttpContext.Items[RunTokenAuthAttribute.ItemKey]!;
            _logger.LogInformation("Agent tool {Tool} called for user {UserId} ({Purpose})", tool, claims.UserId, claims.Purpose);
            return claims;
        }
    }
}
