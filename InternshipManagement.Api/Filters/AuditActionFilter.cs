using System.Security.Claims;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InternshipManagement.Api.Filters
{
    /// <summary>
    /// Records every change an administrator or a company makes (any POST, PUT, PATCH or DELETE), with who did it,
    /// what, to which record, and whether it worked. It stores no request contents, so no personal data is copied
    /// into the log. Saving the record can never make the action itself fail.
    /// </summary>
    public sealed class AuditActionFilter : IAsyncActionFilter
    {
        private static readonly HashSet<string> ChangingMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "POST", "PUT", "PATCH", "DELETE"
        };

        // not business actions, so they would only add noise
        private static readonly HashSet<string> SkippedControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Notifications"
        };

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuditActionFilter> _logger;

        public AuditActionFilter(IServiceScopeFactory scopeFactory, ILogger<AuditActionFilter> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next();

            try
            {
                var http = context.HttpContext;
                if (!ChangingMethods.Contains(http.Request.Method)) return;
                if (http.User.Identity?.IsAuthenticated != true) return;

                var role = http.User.IsInRole("Admin") ? "Admin" : http.User.IsInRole("Company") ? "Company" : null;
                if (role == null) return;

                if (context.ActionDescriptor is not ControllerActionDescriptor descriptor) return;
                if (SkippedControllers.Contains(descriptor.ControllerName)) return;

                var status = executed.Exception != null && !executed.ExceptionHandled
                    ? StatusCodes.Status500InternalServerError
                    : (executed.Result as Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult)?.StatusCode
                      ?? http.Response.StatusCode;

                var entityId = context.RouteData.Values.TryGetValue("id", out var id) ? id?.ToString() : null;

                var entry = new AuditLog
                {
                    CreatedAt = DateTime.UtcNow,
                    UserId = int.TryParse(http.User.FindFirst("userId")?.Value, out var userId) ? userId : null,
                    UserEmail = Trim(http.User.FindFirst(ClaimTypes.Email)?.Value, 256),
                    Role = role,
                    Action = Trim($"{descriptor.ControllerName}.{descriptor.ActionName}", 150)!,
                    HttpMethod = http.Request.Method.ToUpperInvariant(),
                    Path = Trim(http.Request.Path.Value, 300) ?? string.Empty,
                    EntityId = Trim(entityId, 50),
                    StatusCode = status,
                    IpAddress = Trim(http.Connection.RemoteIpAddress?.ToString(), 64)
                };

                // its own scope and context: whatever state the request's context is in cannot affect, or be affected by, this write
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.AuditLogs.Add(entry);
                await db.SaveChangesAsync(http.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not write an audit log entry.");
            }
        }

        private static string? Trim(string? value, int max) =>
            string.IsNullOrEmpty(value) ? value : value.Length <= max ? value : value[..max];
    }
}
