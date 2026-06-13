namespace task11.ApplicationCore;

public class FxUnavailableException : Exception
{
    public FxUnavailableException(string message = "Currency conversion is temporarily unavailable.")
        : base(message) { }

    public FxUnavailableException(string message, Exception innerException)
        : base(message, innerException) { }
}
