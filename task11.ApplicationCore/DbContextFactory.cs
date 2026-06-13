using Microsoft.EntityFrameworkCore;
using task11.Data;

namespace task11.ApplicationCore;

/// <summary>
/// Creates <see cref="AppDbContext"/> instances on demand. Holds the connection settings and
/// the <see cref="IClock"/> the context needs for its audit/soft-delete interceptors.
/// Repositories use a fresh context per operation via <see cref="CreateDbContext"/>; tests
/// subclass this factory to point at an in-memory database.
/// </summary>
public class DbContextFactory
{
    private readonly string _connectionString;
    private readonly bool _useInMemory;
    private readonly IClock _clock;

    public DbContextFactory(string connectionString, bool useInMemory, IClock clock)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _useInMemory = useInMemory;
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Creates a new <see cref="AppDbContext"/> bound to the configured store.</summary>
    public virtual AppDbContext CreateDbContext()
    {
        return new AppDbContext(_connectionString, _useInMemory, _clock);
    }

    /// <summary>Applies pending migrations when the configured store is relational.</summary>
    public void MigrateIfRelational()
    {
        using AppDbContext context = CreateDbContext();
        if (context.Database.IsRelational())
        {
            context.Database.Migrate();
        }
    }
}
