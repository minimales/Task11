namespace task11.ApplicationCore;

public interface IClock
{
    DateTime UtcNow { get; }
}
