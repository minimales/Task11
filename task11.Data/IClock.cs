namespace task11.Data;

/// <summary>
/// Abstraction over the system clock so audit stamping and time-dependent logic are testable.
/// </summary>
public interface IClock
{
    /// <summary>The current UTC time (always <see cref="DateTimeKind.Utc"/>).</summary>
    DateTime UtcNow { get; }
}
