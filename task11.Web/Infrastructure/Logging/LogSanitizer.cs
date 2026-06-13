using System.Text.Json;
using System.Text.Json.Nodes;

namespace task11.Web.Infrastructure.Logging;

/// <summary>
/// Redacts secrets from request/response bodies before they are logged.
/// JSON bodies are parsed and deny-listed keys are recursively replaced with <c>"***"</c>.
/// Non-JSON or oversized bodies are replaced with an <c>[omitted: N bytes]</c> marker.
/// </summary>
public static class LogSanitizer
{
    private const string _redacted = "***";

    /// <summary>Keys whose values must never be logged (case-insensitive).</summary>
    private static readonly HashSet<string> _denyList = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwordHash",
        "accessToken",
        "token",
        "secret",
        "authorization",
        "refreshToken",
        "apiKey"
    };

    /// <summary>
    /// Sanitizes a body for logging. JSON is recursively redacted; anything else
    /// (or bodies larger than <paramref name="maxBodyBytes"/>) is summarized by size.
    /// </summary>
    public static string Sanitize(string? body, int maxBodyBytes)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        int byteCount = System.Text.Encoding.UTF8.GetByteCount(body);
        if (byteCount > maxBodyBytes)
        {
            return $"[omitted: {byteCount} bytes]";
        }

        try
        {
            JsonNode? node = JsonNode.Parse(body);
            if (node is null)
            {
                return body;
            }

            Redact(node);
            return node.ToJsonString();
        }
        catch (JsonException)
        {
            // Non-JSON payload; do not leak raw content that may contain secrets.
            return $"[omitted: {byteCount} bytes]";
        }
    }

    private static void Redact(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj.ToList())
                {
                    if (_denyList.Contains(property.Key))
                    {
                        obj[property.Key] = _redacted;
                    }
                    else
                    {
                        Redact(property.Value);
                    }
                }
                break;

            case JsonArray array:
                foreach (var item in array)
                {
                    Redact(item);
                }
                break;
        }
    }
}
