using System.Security.Claims;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace InternshipManagement.Api.Middleware
{
    public sealed class UnverifiedAccountMiddleware
    {
        private static readonly HashSet<string> AllowedAccountWrites = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/auth/resend-verification",
            "/api/auth/logout",
            "/api/auth/change-password",
            "/api/auth/change-email",
            "/api/auth/preferences"
        };

        private readonly RequestDelegate _next;

        public UnverifiedAccountMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IIdentityService identityService)
        {
            if (context.User.HasClaim("two_factor_login", "true"))
            {
                if (context.Request.Path.Value?.Equals("/api/auth/two-factor/login", StringComparison.OrdinalIgnoreCase) == true
                    || context.Request.Path.Value?.Equals("/api/auth/two-factor/resend-login-code", StringComparison.OrdinalIgnoreCase) == true
                    || context.Request.Path.Value?.Equals("/api/auth/two-factor/cancel-login", StringComparison.OrdinalIgnoreCase) == true)
                {
                    await _next(context);
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Complete the sign-in code step first." });
                return;
            }

            if (context.User.HasClaim("two_factor_setup", "true")
                && !context.Request.Path.StartsWithSegments("/api/auth/two-factor/setup")
                && !context.Request.Path.StartsWithSegments("/api/auth/two-factor/enable"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Two-factor setup is required." });
                return;
            }

            var method = context.Request.Method;
            var isWrite = method is "POST" or "PUT" or "PATCH" or "DELETE";
            if (isWrite && context.User.Identity?.IsAuthenticated == true
                && !AllowedAccountWrites.Contains(context.Request.Path.Value ?? string.Empty))
            {
                var userId = context.User.FindFirstValue("userId")
                    ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = userId == null ? null : await identityService.FindUserByIdAsync(userId);
                if (user is { EmailConfirmed: false })
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Verify your email address before using this feature."
                    });
                    return;
                }
            }

            await _next(context);
        }
    }
}
