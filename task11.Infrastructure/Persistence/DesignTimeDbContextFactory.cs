using Microsoft.EntityFrameworkCore.Design;
using task11.Infrastructure.Time;

namespace task11.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string _defaultConnectionString =
        "Host=localhost;Port=5432;Database=finance;Username=finance;Password=finance";

    public AppDbContext CreateDbContext(string[] args)
    {
        string connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? _defaultConnectionString;

        return new AppDbContext(connectionString, useInMemory: false, new SystemClock());
    }
}
