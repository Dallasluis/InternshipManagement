using InternshipManagement.Application.Interfaces;
using System.Security.Claims;

namespace InternshipManagement.Api.Middleware
{
    public sealed class SessionSecurityMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionSecurityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IIdentityService identityService)
        {
            if (context.User.Identity?.IsAuthenticated == true
                && !context.User.HasClaim("two_factor_setup", "true"))
            {
                var userId = context.User.FindFirstValue("userId")
                    ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null || !await identityService.TouchSessionAsync(userId, DateTime.UtcNow))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "Your session has expired. Sign in again." });
                    return;
                }
            }

            await _next(context);
        }
    }
}
