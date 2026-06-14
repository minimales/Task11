using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using task11.ApplicationCore;
using task11.ApplicationCore.Entities;
using task11.Infrastructure.Persistence.Interceptors;

namespace task11.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly string _connectionString;
    private readonly bool _useInMemory;
    private readonly IClock _clock;

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<WalletEntity> Wallets => Set<WalletEntity>();
    public DbSet<OperationTypeEntity> OperationTypes => Set<OperationTypeEntity>();
    public DbSet<FinancialOperationEntity> FinancialOperations => Set<FinancialOperationEntity>();

    public AppDbContext(string connectionString, bool useInMemory, IClock clock)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _useInMemory = useInMemory;
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_useInMemory)
        {
            optionsBuilder
                .UseInMemoryDatabase(_connectionString)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        }
        else
        {
            optionsBuilder.UseNpgsql(_connectionString);
        }

        optionsBuilder.AddInterceptors(
            new SoftDeleteInterceptor(_clock),
            new AuditInterceptor(_clock));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.ApplySoftDeleteFilter();
    }
}
