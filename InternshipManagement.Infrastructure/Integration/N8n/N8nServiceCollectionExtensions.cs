// InternshipManagement.Infrastructure/Integrations/N8n/N8nServiceCollectionExtensions.cs
using System.Net.Http.Headers;
using InternshipManagement.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InternshipManagement.Infrastructure.Integrations.N8n;

public static class N8nServiceCollectionExtensions
{
	public const string ApiKeyHeader = "X-Api-Key";

	public static IServiceCollection AddN8nIntegration(this IServiceCollection services, IConfiguration config)
	{
		services.AddOptions<N8nOptions>()
			.Bind(config.GetSection(N8nOptions.SectionName))
			.Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "N8n:BaseUrl must be an absolute URL.")
			.Validate(o => o.OutboundApiKey.Length >= 32, "N8n:OutboundApiKey must be at least 32 characters.")
			.Validate(o => o.InboundApiKey.Length >= 32, "N8n:InboundApiKey must be at least 32 characters.")
			.Validate(o => o.OutboundApiKey != o.InboundApiKey, "N8n inbound and outbound keys must differ.")
			.ValidateOnStart();

		services.AddHttpClient<IN8nClient, N8nClient>((sp, client) =>
		{
			var o = sp.GetRequiredService<IOptions<N8nOptions>>().Value;
			client.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
			client.Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds);
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", o.OutboundApiKey);
		});

		// Streaming calls (career assistant): same address and key, but no overall timeout, because a reply
		// is read for as long as it takes. The caller enforces its own limit.
		services.AddHttpClient<IN8nStreamClient, N8nStreamClient>((sp, client) =>
		{
			var o = sp.GetRequiredService<IOptions<N8nOptions>>().Value;
			client.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
			client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", o.OutboundApiKey);
		});

		return services;
	}
}

// In Program.cs:   builder.Services.AddN8nIntegration(builder.Configuration);
// Calls TO n8n send "Authorization: Bearer <N8n:OutboundApiKey>". Calls FROM n8n use the X-Api-Key header (ApiKeyHeader).
