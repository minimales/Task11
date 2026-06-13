namespace task11.ApplicationCore;

/// <summary>
/// Thrown when a currency rate cannot be obtained after retries. Mapped to HTTP 503.
/// Guarantees an unconverted amount is never stored as if converted.
/// </summary>
public sealed class FxUnavailableException : Exception
{
    public FxUnavailableException(string message = "Currency conversion is temporarily unavailable.")
        : base(message) { }

    public FxUnavailableException(string message, Exception innerException)
        : base(message, innerException) { }
}
