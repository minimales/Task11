namespace task11.Data;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
