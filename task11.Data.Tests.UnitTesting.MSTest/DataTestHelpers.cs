using Microsoft.EntityFrameworkCore.Metadata;
using task11.Data;

/// <summary>
/// Deterministic <see cref="IClock"/> used by the Data-layer tests so audit/soft-delete
/// stamps are predictable. Hand-rolled fake; no Moq.
/// </summary>
internal sealed class FixedClock : IClock
{
    public DateTime UtcNow { get; set; }

    public FixedClock(DateTime utcNow) => UtcNow = utcNow;

    public static FixedClock At(int year, int month, int day) =>
        new(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc));
}

/// <summary>
/// Shared builders for the Data-layer tests. <see cref="InMemory"/> opens an isolated
/// in-memory store (named per test); <see cref="RelationalModel"/> builds the Npgsql model
/// without a live database so relational-only metadata (precision, filtered indexes, column
/// types, defaults) can be asserted against the EF model.
/// </summary>
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
