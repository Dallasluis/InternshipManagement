// InternshipManagement.Api/Filters/N8nApiKeyAttribute.cs
using InternshipManagement.Infrastructure.Integrations.N8n;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace InternshipManagement.Api.Filters;

/// <summary>Protects endpoints that n8n calls back into. Requires the shared secret in X-Api-Key.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class N8nApiKeyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var expected = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<N8nOptions>>().Value.InboundApiKey;

        var provided = context.HttpContext.Request.Headers[N8nServiceCollectionExtensions.ApiKeyHeader].ToString();

        if (!SecretsMatch(provided, expected))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }

    // Hash first so lengths are equal, then compare in constant time.
    private static bool SecretsMatch(string provided, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(provided)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
}
