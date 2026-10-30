using InternshipManagement.Application.DTOs.Student;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>
    /// Read-only career assistant for students. The reply streams as Server-Sent Events:
    ///   data: {"text":"..."}            repeated, one per piece of text
    ///   event: done / data: {}          the reply is complete
    ///   event: error / data: {"message":"..."}   something failed (what was already sent stays valid)
    /// Problems found before the reply starts are normal HTTP errors: 400 bad input, 429 limits.
    /// Nothing is stored: the app sends the earlier turns with every message.
    /// </summary>
    [ApiController]
    [Route("api/students/career-assistant")]
    [Authorize(Roles = "Student")]
    public class CareerAssistantController : ControllerBase
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly ICareerChatService _chat;
        private readonly ILogger<CareerAssistantController> _logger;

        public CareerAssistantController(ICareerChatService chat, ILogger<CareerAssistantController> logger)
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
                Response.Headers["X-Accel-Buffering"] = "no";   // tell proxies (nginx) not to hold the stream back
                HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

                try
                {
                    await foreach (var text in session.StreamAsync(cancellationToken))
                        await WriteEventAsync(null, new { text }, cancellationToken);

                    await WriteEventAsync("done", new { }, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // the app closed the connection: nothing more to send
                }
                catch (Exception ex)
                {
                    // Log the type only: messages can carry user text.
                    _logger.LogWarning("Career assistant stream failed: {Type}", ex.GetType().Name);
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
