using InternshipManagement.Api.Filters;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>
    /// Read-only endpoints for the CV analysis agent's MCP tools. Like AgentToolsController they are not
    /// for the frontend and not reachable with a login token: they need a run token with purpose "cv" and
    /// role "Student". They return reference data only (term spellings), never another student's profile.
    /// </summary>
    [ApiController]
    [Route("api/agent-tools/student")]
    [RunTokenAuth("cv", requiredRole: "Student")]
    public class StudentAgentToolsController : ControllerBase
    {
        private readonly IAgentToolsService _tools;
        private readonly ICvAgentToolsService _cvTools;
        private readonly ILogger<StudentAgentToolsController> _logger;

        public StudentAgentToolsController(
            IAgentToolsService tools, ICvAgentToolsService cvTools, ILogger<StudentAgentToolsController> logger)
        {
            _tools = tools;
            _cvTools = cvTools;
            _logger = logger;
        }

        [HttpGet("skill-vocabulary")]
        public async Task<IActionResult> SkillVocabulary(CancellationToken cancellationToken)
        {
            LogCall("skill-vocabulary");
            return Ok(await _tools.GetSkillVocabularyAsync(cancellationToken));
        }

        [HttpGet("certification-vocabulary")]
        public async Task<IActionResult> CertificationVocabulary(CancellationToken cancellationToken)
        {
            LogCall("certification-vocabulary");
            return Ok(await _cvTools.GetCertificationVocabularyAsync(cancellationToken));
        }

        private void LogCall(string tool)
        {
            // RunTokenAuth has already rejected the request if this is missing.
            var claims = (RunTokenClaims)HttpContext.Items[RunTokenAuthAttribute.ItemKey]!;
            _logger.LogInformation("Student agent tool {Tool} called for user {UserId} ({Purpose})", tool, claims.UserId, claims.Purpose);
        }
    }
}
