using InternshipManagement.Api.Filters;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>
    /// Read-only endpoints for the company assistant's MCP tools. Not for the frontend and not reachable with a login
    /// token: they need a run token with purpose "company-chat" and role "Company". The company always comes from
    /// the token, never from the request, so the model cannot ask about another company.
    /// </summary>
    [ApiController]
    [Route("api/agent-tools/company-chat")]
    [RunTokenAuth("company-chat", requiredRole: "Company")]
    public class CompanyChatAgentToolsController : ControllerBase
    {
        private readonly ICompanyAgentToolsService _tools;
        private readonly ILogger<CompanyChatAgentToolsController> _logger;

        public CompanyChatAgentToolsController(ICompanyAgentToolsService tools, ILogger<CompanyChatAgentToolsController> logger)
        {
            _tools = tools;
            _logger = logger;
        }

        /// <summary>Company name, industry, verification status and counts of internships, applications and placements.</summary>
        [HttpGet("overview")]
        public async Task<IActionResult> Overview(CancellationToken cancellationToken)
        {
            var claims = Claims();
            _logger.LogInformation("Company chat tool overview called for user {UserId}", claims.UserId);
            try { return Ok(await _tools.GetOverviewAsync(claims.UserId, cancellationToken)); }
            catch (InvalidOperationException ex) { return NotFound(new { Error = ex.Message }); }
        }

        /// <summary>The company's newest internships with deadlines and application counts.</summary>
        [HttpGet("internships")]
        public async Task<IActionResult> Internships(CancellationToken cancellationToken)
        {
            var claims = Claims();
            _logger.LogInformation("Company chat tool internships called for user {UserId}", claims.UserId);
            try { return Ok(await _tools.GetInternshipsAsync(claims.UserId, cancellationToken)); }
            catch (InvalidOperationException ex) { return NotFound(new { Error = ex.Message }); }
        }

        /// <summary>Applicants for one of the company's internships, identified only by application number.</summary>
        [HttpGet("applicants")]
        public async Task<IActionResult> Applicants([FromQuery] int internshipId, CancellationToken cancellationToken)
        {
            var claims = Claims();
            _logger.LogInformation("Company chat tool applicants called for user {UserId}", claims.UserId);
            try { return Ok(await _tools.GetApplicantsAsync(claims.UserId, internshipId, cancellationToken)); }
            catch (InvalidOperationException ex) { return NotFound(new { Error = ex.Message }); }
        }

        // RunTokenAuth has already rejected the request if this is missing.
        private RunTokenClaims Claims() => (RunTokenClaims)HttpContext.Items[RunTokenAuthAttribute.ItemKey]!;
    }
}
