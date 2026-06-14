using Microsoft.EntityFrameworkCore;
using task11.ApplicationCore;

namespace task11.Infrastructure.Persistence;

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

    public virtual AppDbContext CreateDbContext()
    {
        return new AppDbContext(_connectionString, _useInMemory, _clock);
    }

    public void MigrateIfRelational()
    {
        using AppDbContext context = CreateDbContext();
        if (context.Database.IsRelational())
        {
            context.Database.Migrate();
        }
    }
}
