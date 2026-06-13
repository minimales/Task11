namespace task11.Data;

/// <summary>
/// Default <see cref="IClock"/> backed by <see cref="DateTime.UtcNow"/>. Registered as a singleton.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
