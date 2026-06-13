using Microsoft.EntityFrameworkCore.Metadata;
using task11.Data;

internal class FixedClock : IClock
{
    public DateTime UtcNow { get; set; }

    public FixedClock(DateTime utcNow) => UtcNow = utcNow;

    public static FixedClock At(int year, int month, int day) =>
        new(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc));
}

internal static class DataTestHelpers
{
    private const string _fakeNpgsqlConnectionString =
        "Host=localhost;Port=5432;Database=test;Username=test;Password=test";

    public static AppDbContext InMemory(string databaseName) =>
        new(databaseName, useInMemory: true, new SystemClock());

    public static AppDbContext InMemory(string databaseName, IClock clock) =>
        new(databaseName, useInMemory: true, clock);

    public static AppDbContext RelationalModel() =>
        new(_fakeNpgsqlConnectionString, useInMemory: false, new SystemClock());

    public static IEntityType EntityType<T>(AppDbContext context) =>
        context.Model.FindEntityType(typeof(T))!;
}
