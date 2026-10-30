// InternshipManagement.Infrastructure/Integrations/N8n/N8nStreamClient.cs
using InternshipManagement.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace InternshipManagement.Infrastructure.Integrations.N8n;

/// <summary>
/// Calls an n8n webhook whose Webhook node responds in "Streaming" mode and yields the text as it arrives.
/// n8n sends one JSON object per line: {"type":"begin"|"item"|"end"|"error","content":"...","metadata":{...}}.
/// Only "item" text is passed on. If the workflow is NOT set to stream and answers with a single JSON object,
/// its "output" (or "text" / "reply") is used as one chunk, so the chat still works, just without the typing effect.
/// </summary>
public sealed class N8nStreamClient(
    HttpClient http,
    IOptions<N8nOptions> options,
    ILogger<N8nStreamClient> logger) : IN8nStreamClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const int MaxRawChars = 64 * 1024;

    private enum Kind { Ignore, Text, Error }

    public async IAsyncEnumerable<string> StreamTextAsync<TRequest>(
        string webhookPath, TRequest payload, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var prefix = options.Value.UseTestWebhooks ? "webhook-test" : "webhook";
        var url = $"{prefix}/{webhookPath.Trim('/')}";

        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload, options: Json)
            };
            response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Could not reach n8n for workflow {Path}", webhookPath);
            throw new N8nException("The automation service is unreachable.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // Status only: bodies and messages can contain personal data.
                logger.LogWarning("n8n workflow {Path} returned {Status}", webhookPath, (int)response.StatusCode);
                throw new N8nException($"The automation service returned {(int)response.StatusCode}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var raw = new StringBuilder();
            var yielded = false;

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (line.Length == 0) continue;
                if (raw.Length < MaxRawChars) raw.AppendLine(line);

                var kind = ReadLine(line, out var text);
                if (kind == Kind.Error)
                    throw new N8nException("The assistant failed to answer.");

                if (kind == Kind.Text && !string.IsNullOrEmpty(text))
                {
                    yielded = true;
                    yield return text;
                }
            }

            if (!yielded)
            {
                // Not streamed after all: one JSON object over several lines.
                var whole = ReadWhole(raw.ToString());
                if (string.IsNullOrWhiteSpace(whole))
                    throw new N8nException("The assistant returned nothing.");

                yield return whole;
            }
        }
    }

    private static Kind ReadLine(string line, out string? text)
    {
        text = null;
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return Kind.Ignore;

            if (root.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String)
            {
                switch (type.GetString())
                {
                    case "item":
                        if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
                        {
                            text = content.GetString();
                            return Kind.Text;
                        }
                        return Kind.Ignore;
                    case "error":
                        return Kind.Error;
                    default:
                        return Kind.Ignore;   // begin, end
                }
            }

            text = FirstString(root);
            return text != null ? Kind.Text : Kind.Ignore;
        }
        catch (JsonException)
        {
            return Kind.Ignore;
        }
    }

    private static string? ReadWhole(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.ValueKind == JsonValueKind.Object ? FirstString(doc.RootElement) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? FirstString(JsonElement obj)
    {
        foreach (var name in new[] { "output", "text", "reply" })
        {
            if (obj.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }
        return null;
    }
}
