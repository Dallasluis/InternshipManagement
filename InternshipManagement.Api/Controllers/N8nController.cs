// InternshipManagement.Api/Controllers/N8nController.cs
using InternshipManagement.Api.Filters;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Api.Controllers;

/// <summary>Endpoints n8n calls. Not JWT-protected; guarded by the shared API key instead.</summary>
[ApiController]
[Route("api/n8n")]
public class N8nCallbackController : ControllerBase
{
    [HttpGet("ping")]
    [AllowAnonymous]
    [N8nApiKey]
    public IActionResult Ping() => Ok(new { status = "ok", utc = DateTime.UtcNow });
}

/// <summary>Admin-only connectivity check: API -> n8n -> response.</summary>
[ApiController]
[Route("api/admin/n8n")]
[Authorize(Roles = "Admin")]
public class N8nAdminController(IN8nClient n8n) : ControllerBase
{
    [HttpPost("ping")]
    public async Task<IActionResult> Ping(CancellationToken ct)
    {
        try
        {
            var result = await n8n.RunWorkflowAsync<object, JsonElement>(
                "ping", new { message = "hello from the API" }, ct);
            return Ok(result);
        }
        catch (N8nException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }
}
