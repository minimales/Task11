using Microsoft.EntityFrameworkCore.Design;

namespace task11.Data;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> build the context without a running database or the web host.
/// Uses a local default connection string (or <c>ConnectionStrings__Default</c> when present),
/// <c>useInMemory: false</c> and a real <see cref="SystemClock"/>.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
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
