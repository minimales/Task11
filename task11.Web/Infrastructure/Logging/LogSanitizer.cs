using System.Text.Json;
using System.Text.Json.Nodes;

namespace task11.Web.Infrastructure.Logging;

public static class LogSanitizer
{
    private const string _redacted = "***";

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
        catch (Exception)
        {
            return $"[omitted: {byteCount} bytes]";
        }
    }

    private static void Redact(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (KeyValuePair<string, JsonNode?> property in obj.ToList())
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
                foreach (JsonNode? item in array)
                {
                    Redact(item);
                }
                break;
        }
    }
}
