// InternshipManagement.Infrastructure/Integrations/N8n/N8nClient.cs
using InternshipManagement.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Infrastructure.Integrations.N8n;

public sealed class N8nClient(
    HttpClient http,
    IOptions<N8nOptions> options,
    ILogger<N8nClient> logger) : IN8nClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<TResponse> RunWorkflowAsync<TRequest, TResponse>(
        string webhookPath, TRequest payload, CancellationToken cancellationToken = default)
    {
        var prefix = options.Value.UseTestWebhooks ? "webhook-test" : "webhook";
        var url = $"{prefix}/{webhookPath.Trim('/')}";

        try
        {
            using var response = await http.PostAsJsonAsync(url, payload, Json, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Log the status only. The body can contain CV text or other personal data.
                logger.LogWarning("n8n workflow {Path} returned {Status}", webhookPath, (int)response.StatusCode);
                throw new N8nException($"The automation service returned {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<TResponse>(Json, cancellationToken);
            return result ?? throw new N8nException("The automation service returned an empty response.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Could not reach n8n for workflow {Path}", webhookPath);
            throw new N8nException("The automation service is unreachable.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new N8nException("The automation service timed out.", ex);
        }
        catch (JsonException ex)
        {
            throw new N8nException("The automation service returned invalid JSON.", ex);
        }
    }
}
