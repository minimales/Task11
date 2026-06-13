namespace task11.ApplicationCore.Models;

public class ErrorModel
{

    public string Type { get; set; } = "about:blank";

    public string Title { get; set; } = string.Empty;

    public int Status { get; set; }

    public string? Detail { get; set; }

    public string? Instance { get; set; }

    public string? CorrelationId { get; set; }

    public IDictionary<string, string[]>? Errors { get; set; }
}
