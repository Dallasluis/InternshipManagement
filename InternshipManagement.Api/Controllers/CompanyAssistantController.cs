using System.Text.Json;
using InternshipManagement.Application.DTOs.Student;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>
    /// Read-only assistant for companies. The reply streams as Server-Sent Events, exactly like the student assistant:
    ///   data: {"text":"..."}  repeated; event: done; event: error.
    /// Problems found before the reply starts are normal HTTP errors: 400 bad input, 429 limits. Nothing is stored.
    /// </summary>
    [ApiController]
    [Route("api/companies/assistant")]
    [Authorize(Roles = "Company")]
    public class CompanyAssistantController : ControllerBase
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly ICompanyChatService _chat;
        private readonly ILogger<CompanyAssistantController> _logger;

        public CompanyAssistantController(ICompanyChatService chat, ILogger<CompanyAssistantController> logger)
        {
            _chat = chat;
            _logger = logger;
        }

        [HttpPost("chat")]
        [RequestSizeLimit(64 * 1024)]
        public async Task<IActionResult> Chat([FromBody] CareerChatRequest request, CancellationToken cancellationToken)
        {
            ICareerChatSession session;
            try
            {
                session = await _chat.BeginAsync(GetUserId(), request, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (CareerChatLimitException ex)
            {
                Response.Headers.RetryAfter = ex.RetryAfterSeconds.ToString();
                return StatusCode(StatusCodes.Status429TooManyRequests, new { Error = ex.Message });
            }

            using (session)
            {
                Response.StatusCode = StatusCodes.Status200OK;
                Response.ContentType = "text/event-stream; charset=utf-8";
                Response.Headers.CacheControl = "no-cache";
                Response.Headers["X-Accel-Buffering"] = "no";
                HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

                try
                {
                    await foreach (var text in session.StreamAsync(cancellationToken))
                        await WriteEventAsync(null, new { text }, cancellationToken);

                    await WriteEventAsync("done", new { }, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // the page closed the connection: nothing more to send
                }
                catch (Exception ex)
                {
                    // Log the type only: messages can carry user text.
                    _logger.LogWarning("Company assistant stream failed: {Type}", ex.GetType().Name);
                    await TryWriteErrorAsync();
                }
            }

            return new EmptyResult();
        }

        private async Task WriteEventAsync(string? name, object data, CancellationToken cancellationToken)
        {
            var prefix = name == null ? string.Empty : $"event: {name}\n";
            await Response.WriteAsync($"{prefix}data: {JsonSerializer.Serialize(data, Json)}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        private async Task TryWriteErrorAsync()
        {
            try
            {
                await WriteEventAsync("error",
                    new { message = "The assistant is unavailable right now. Please try again." }, CancellationToken.None);
            }
            catch
            {
                // the connection is already gone
            }
        }

        private int GetUserId() => int.Parse(User.FindFirst("userId")?.Value ?? "0");
    }
}
