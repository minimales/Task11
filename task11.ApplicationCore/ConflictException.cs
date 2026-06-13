namespace task11.ApplicationCore;

/// <summary>Thrown on a state conflict (e.g. duplicate name, immutable currency change). Mapped to HTTP 409.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
