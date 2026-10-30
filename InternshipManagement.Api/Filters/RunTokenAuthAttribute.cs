using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InternshipManagement.Api.Filters
{
    /// <summary>
    /// Authenticates a request with a short-lived run token instead of a login JWT.
    /// Accepts "Authorization: Bearer ..." or a ?runToken= query parameter (n8n tool nodes
    /// drop custom headers, so the query parameter is the reliable path).
    /// Used only on /api/agent-tools. On success the caller is available as
    /// HttpContext.Items["RunToken"] (a RunTokenClaims).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RunTokenAuthAttribute : Attribute, IAuthorizationFilter
    {
        public const string ItemKey = "RunToken";
        private readonly string _purpose;
        private readonly string? _requiredRole;

        public RunTokenAuthAttribute(string purpose, string? requiredRole = null)
        {
            _purpose = purpose;
            _requiredRole = requiredRole;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var request = context.HttpContext.Request;
            const string prefix = "Bearer ";
            string? token = null;

            var header = request.Headers.Authorization.ToString();
            if (header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                token = header[prefix.Length..].Trim();
            else if (request.Query.TryGetValue("runToken", out var fromQuery))
                token = fromQuery.ToString().Trim().Trim('"');

            if (string.IsNullOrEmpty(token))
            {
                context.Result = new UnauthorizedObjectResult(new { Error = "Run token required." });
                return;
            }

            var tokens = context.HttpContext.RequestServices.GetRequiredService<IRunTokenService>();
            var claims = tokens.Validate(token, _purpose);

            if (claims == null)
            {
                context.Result = new UnauthorizedObjectResult(new { Error = "Run token invalid or expired." });
                return;
            }

            if (_requiredRole != null && !string.Equals(claims.Role, _requiredRole, StringComparison.Ordinal))
            {
                context.Result = new ObjectResult(new { Error = "Not allowed." }) { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }

            context.HttpContext.Items[ItemKey] = claims;
        }
    }
}
