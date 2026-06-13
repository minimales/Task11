namespace task11.ApplicationCore;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You do not have access to this resource.")
        : base(message) { }
}
