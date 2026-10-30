using InternshipManagement.Application.DTOs.Internship;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Api.Controllers
{
    [ApiController]
    [Route("api/internships/ai-draft")]
    [Authorize(Roles = "Company")]
    public class InternshipDraftController : ControllerBase
    {
        private readonly IInternshipDraftService _draftService;

        public InternshipDraftController(IInternshipDraftService draftService)
        {
            _draftService = draftService;
        }

        /// <summary>
        /// Generates a draft from a plain-language description and SAVES it (as a draft, not an internship).
        /// The response carries the draft id. The company edits it, then publishes through the normal
        /// POST api/internships endpoint.
        /// </summary>
        [HttpPost]
        [EnableRateLimiting("ai-generation")]
        public async Task<IActionResult> Generate(
            [FromBody] GenerateInternshipDraftRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var draft = await _draftService.GenerateDraftAsync(GetUserId(), request, cancellationToken);
                return Ok(draft);
            }
            catch (InvalidOperationException ex)   // company missing, unverified, not subscribed, draft limit
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (AiDraftRejectedException ex)    // the AI answered but the draft is unusable
            {
                return UnprocessableEntity(new { Error = ex.Message });
            }
            catch (N8nException)                   // n8n or Gemini unreachable, timed out or returned an error
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    Error = "The AI service is unavailable right now. Try again, or fill in the form manually."
                });
            }
        }

        /// <summary>The company's saved drafts, newest first.</summary>
        [HttpGet]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _draftService.ListDraftsAsync(GetUserId(), cancellationToken));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>One saved draft, to reopen the form after a reload.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
        {
            try
            {
                var draft = await _draftService.GetDraftAsync(GetUserId(), id, cancellationToken);
                return draft == null ? NotFound(new { Error = "Draft not found." }) : Ok(draft);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>Saves the company's edits to a draft.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id, [FromBody] SaveInternshipDraftRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var draft = await _draftService.UpdateDraftAsync(GetUserId(), id, request, cancellationToken);
                return draft == null ? NotFound(new { Error = "Draft not found." }) : Ok(draft);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>Deletes a draft. Call this after the internship is published, or to discard it.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                return await _draftService.DeleteDraftAsync(GetUserId(), id, cancellationToken)
                    ? NoContent()
                    : NotFound(new { Error = "Draft not found." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        private int GetUserId() => int.Parse(User.FindFirst("userId")?.Value ?? "0");
    }
}
