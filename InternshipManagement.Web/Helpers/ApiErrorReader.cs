using System.Text.Json;

namespace InternshipManagement.Web.Helpers
{
    /// <summary>Pulls the readable message out of an API error answer such as {"error":"..."}.</summary>
    public static class ApiErrorReader
    {
        public static string Read(string? body, string fallback)
        {
            if (string.IsNullOrWhiteSpace(body)) return fallback;

            try
            {
                using var doc = JsonDocument.Parse(body);
                foreach (var name in new[] { "error", "Error", "message", "Message", "title" })
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Object
                        && doc.RootElement.TryGetProperty(name, out var value)
                        && value.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(value.GetString()))
                        return value.GetString()!;
                }
            }
            catch (JsonException) { /* not JSON: use the fallback */ }

            return fallback;
        }
    }
}
