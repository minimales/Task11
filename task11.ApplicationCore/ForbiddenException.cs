namespace task11.ApplicationCore;

/// <summary>Thrown when the current user may not access a resource. Mapped to HTTP 403.</summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You do not have access to this resource.")
        : base(message) { }
}
