// InternshipManagement.Infrastructure/Integrations/N8n/N8nOptions.cs
namespace InternshipManagement.Infrastructure.Integrations.N8n;

public sealed class N8nOptions
{
    public const string SectionName = "N8n";

    /// <summary>Where the API reaches n8n, e.g. http://localhost:5678</summary>
    public string BaseUrl { get; set; } = "http://localhost:5678";

    /// <summary>Secret the API sends TO n8n (checked by the Webhook node's Header Auth). Keep in user-secrets.</summary>
    public string OutboundApiKey { get; set; } = string.Empty;

    /// <summary>Secret n8n must send TO the API on callbacks. Keep in user-secrets. Must differ from OutboundApiKey.</summary>
    public string InboundApiKey { get; set; } = string.Empty;

    /// <summary>Use /webhook-test/ URLs (only live while "Listen for test event" is active in the editor).</summary>
    public bool UseTestWebhooks { get; set; }

    public int TimeoutSeconds { get; set; } = 60;
}
