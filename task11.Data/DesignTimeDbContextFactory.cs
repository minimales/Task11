using Microsoft.EntityFrameworkCore.Design;

namespace task11.Data;

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
