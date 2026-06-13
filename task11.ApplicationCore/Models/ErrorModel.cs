namespace task11.ApplicationCore.Models;

/// <summary>
/// RFC 7807-style problem details body returned by the exception-handling middleware.
/// </summary>
public sealed class ErrorModel
{
    /// <summary>A URI reference identifying the problem type.</summary>
    public string Type { get; set; } = "about:blank";

    /// <summary>A short, human-readable summary of the problem type.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>The HTTP status code.</summary>
    public int Status { get; set; }

    /// <summary>A human-readable explanation specific to this occurrence.</summary>
    public string? Detail { get; set; }

    /// <summary>The request path that produced the error.</summary>
    public string? Instance { get; set; }

    /// <summary>The correlation id for cross-referencing logs.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Per-field validation errors, when applicable.</summary>
    public IDictionary<string, string[]>? Errors { get; set; }
}
