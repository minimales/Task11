# Clean Architecture Recommendations

This document describes how the current `task11` solution could be refactored from a layered architecture into a stricter Clean Architecture layout.

## Current State

The current solution mostly follows the assignment requirement:

```text
Controllers -> Services -> Repositories
```

The actual project dependency direction is:

```text
task11.Web -> task11.ApplicationCore -> task11.Data
```

This is a valid layered architecture for the educational task. Controllers call services, services call repository abstractions, and repositories work with the database. The implementation also includes optional features from the task: users, JWT authentication, wallets, wallet isolation, online currency conversion, logging middleware, soft delete, Swagger, PostgreSQL, Docker, and tests.

However, this is not strict Clean Architecture because `ApplicationCore` depends on `Data`. In Clean Architecture, the application layer should not depend on EF Core, PostgreSQL, migrations, or other infrastructure details.

## Target Dependency Direction

A stricter Clean Architecture version would use this dependency direction:

```text
Domain <- Application <- Web
Domain <- Application <- Infrastructure
```

In practical terms:

- `Domain` contains business entities and business concepts.
- `Application` contains use cases, service interfaces, repository interfaces, DTOs, validators, and application exceptions.
- `Infrastructure` implements persistence, repositories, currency clients, password hashing, JWT generation, and system services.
- `Web` contains controllers, middleware, authentication pipeline, Swagger, and dependency injection composition.

The center of the application should not know anything about HTTP, EF Core, PostgreSQL, Docker, JWT bearer configuration, or third-party exchange-rate APIs.

## Recommended Project Structure

```text
task11.Domain
  Common/
    BaseEntity.cs
  Entities/
    User.cs
    Wallet.cs
    OperationType.cs
    FinancialOperation.cs
  Enums/
    OperationKind.cs

task11.Application
  Abstractions/
    Auth/
      ICurrentUser.cs
      IJwtTokenGenerator.cs
      IPasswordHasher.cs
    Currency/
      ICurrencyConverter.cs
    Persistence/
      IUserRepository.cs
      IWalletRepository.cs
      IOperationTypeRepository.cs
      IOperationRepository.cs
      IReportRepository.cs
    Time/
      IClock.cs
  Models/
  Validators/
  Services/
  Exceptions/
  DependencyInjection.cs

task11.Infrastructure
  Auth/
    JwtTokenGenerator.cs
    PasswordHasher.cs
  Currency/
    CurrencyConverter.cs
    FrankfurterClient.cs
    PrivatBankClient.cs
    FxOptions.cs
  Persistence/
    AppDbContext.cs
    EntityConfigurations/
    Interceptors/
    Migrations/
  Repositories/
    UserRepository.cs
    WalletRepository.cs
    OperationTypeRepository.cs
    OperationRepository.cs
    ReportRepository.cs
  Time/
    SystemClock.cs
  DependencyInjection.cs

task11.Web
  Controllers/
  Middleware/
  Infrastructure/
    Auth/
      CurrentUser.cs
    Logging/
      LogSanitizer.cs
  Program.cs
```

## Recommended Project References

```text
task11.Domain
  no project references

task11.Application
  -> task11.Domain

task11.Infrastructure
  -> task11.Application
  -> task11.Domain

task11.Web
  -> task11.Application
  -> task11.Infrastructure
```

`task11.Web` may reference `task11.Infrastructure` only to register infrastructure services in the composition root. Controllers should depend on application services, not infrastructure classes.

## What To Move

### Move Entities To Domain

Move:

```text
task11.Data/Entities/*
```

To:

```text
task11.Domain/Entities/*
task11.Domain/Common/BaseEntity.cs
task11.Domain/Enums/OperationKind.cs
```

Recommended rename:

```text
UserEntity -> User
WalletEntity -> Wallet
OperationTypeEntity -> OperationType
FinancialOperationEntity -> FinancialOperation
```

The domain entities should not contain EF Core attributes or persistence-specific behavior. EF-specific configuration should stay in `Infrastructure/Persistence/EntityConfigurations`.

### Move Repository Interfaces To Application

Move:

```text
task11.ApplicationCore/Repositories/Abstractions/*
```

To:

```text
task11.Application/Abstractions/Persistence/*
```

These interfaces represent what the application needs from persistence. The application owns these contracts.

### Move Repository Implementations To Infrastructure

Move:

```text
task11.ApplicationCore/Repositories/*.cs
```

To:

```text
task11.Infrastructure/Repositories/*.cs
```

Repository implementations use EF Core and should therefore live in Infrastructure.

### Move EF Core To Infrastructure

Move:

```text
task11.Data/AppDbContext.cs
task11.Data/DesignTimeDbContextFactory.cs
task11.Data/ModelBuilderExtensions.cs
task11.Data/EntityConfigurations/*
task11.Data/Interceptors/*
task11.Data/Migrations/*
```

To:

```text
task11.Infrastructure/Persistence/*
```

EF Core, migrations, PostgreSQL configuration, query filters, and save-change interceptors are infrastructure concerns.

### Move Auth Implementations To Infrastructure

Move concrete implementations:

```text
task11.ApplicationCore/Auth/PasswordHasher.cs
task11.ApplicationCore/Auth/JwtTokenGenerator.cs
```

To:

```text
task11.Infrastructure/Auth/*
```

Keep interfaces in Application:

```text
task11.Application/Abstractions/Auth/IPasswordHasher.cs
task11.Application/Abstractions/Auth/IJwtTokenGenerator.cs
task11.Application/Abstractions/Auth/ICurrentUser.cs
```

`JwtSettings` can live in Infrastructure or Web depending on whether token generation remains infrastructure-owned. For cleaner separation, keep token configuration near the JWT implementation in Infrastructure.

### Move Currency Implementations To Infrastructure

Move:

```text
task11.ApplicationCore/Currency/FrankfurterClient.cs
task11.ApplicationCore/Currency/PrivatBankClient.cs
task11.ApplicationCore/Currency/CurrencyConverter.cs
task11.ApplicationCore/Currency/FxOptions.cs
```

To:

```text
task11.Infrastructure/Currency/*
```

Keep only the application-facing interface in Application:

```text
task11.Application/Abstractions/Currency/ICurrencyConverter.cs
```

The application should know that currency conversion exists, but not which HTTP provider performs it.

### Move Clock Implementation To Infrastructure

Keep interface:

```text
task11.Application/Abstractions/Time/IClock.cs
```

Move implementation:

```text
task11.Infrastructure/Time/SystemClock.cs
```

## DbContext Lifetime Recommendation

The current solution uses a custom `DbContextFactory` and creates a new context per repository method. For a stricter and more common ASP.NET Core setup, prefer standard dependency injection:

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
```

Then inject `AppDbContext` into repositories:

```csharp
public class OperationRepository : IOperationRepository
{
    private readonly AppDbContext _db;

    public OperationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(FinancialOperation operation, CancellationToken cancellationToken)
    {
        await _db.FinancialOperations.AddAsync(operation, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
```

This keeps EF Core in Infrastructure and uses the framework's normal scoped `DbContext` lifetime.

## Application Service Example

The application service should depend on abstractions and domain entities only:

```csharp
public class OperationService : IOperationService
{
    private readonly IOperationRepository _operations;
    private readonly IWalletRepository _wallets;
    private readonly ICurrencyConverter _currencyConverter;
    private readonly ICurrentUser _currentUser;

    public OperationService(
        IOperationRepository operations,
        IWalletRepository wallets,
        ICurrencyConverter currencyConverter,
        ICurrentUser currentUser)
    {
        _operations = operations;
        _wallets = wallets;
        _currencyConverter = currencyConverter;
        _currentUser = currentUser;
    }

    public async Task<OperationModel> CreateAsync(CreateOperationModel request, CancellationToken cancellationToken)
    {
        var wallet = await _wallets.GetByIdAsync(request.WalletId, cancellationToken)
            ?? throw new NotFoundException("Wallet", request.WalletId);

        // Business rules:
        // - current user can access wallet
        // - operation type belongs to wallet
        // - amount/date/currency are valid
        // - foreign-currency amount is converted to wallet base currency

        var operation = new FinancialOperation
        {
            WalletId = wallet.Id,
            OperationTypeId = request.TypeId,
            Amount = request.Amount,
            OccurredAtUtc = ToUtc(request.Date),
            Note = request.Note
        };

        await _operations.AddAsync(operation, cancellationToken);

        return OperationModel.From(operation, wallet.BaseCurrency);
    }
}
```

The service does not know whether operations are stored in PostgreSQL, SQL Server, a file, or an in-memory test store.

## Infrastructure Registration

`task11.Infrastructure` should expose a single registration method:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is missing.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IOperationTypeRepository, OperationTypeRepository>();
        services.AddScoped<IOperationRepository, OperationRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ICurrencyConverter, CurrencyConverter>();

        return services;
    }
}
```

## Application Registration

`task11.Application` should expose its own registration method:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IOperationTypeService, OperationTypeService>();
        services.AddScoped<IOperationService, OperationService>();
        services.AddScoped<IReportService, ReportService>();

        services.AddValidatorsFromAssemblyContaining<LoginModelValidator>(includeInternalTypes: true);

        return services;
    }
}
```

## Web Composition Root

`Program.cs` should mostly compose the application:

```csharp
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddControllers();
builder.Services.AddAuthentication(...);
builder.Services.AddAuthorization(...);
builder.Services.AddSwaggerGen(...);
```

The Web project remains responsible for:

- Controllers
- Middleware
- Swagger
- Authentication and authorization pipeline
- HTTP-specific current user implementation
- Exception handling
- Request/response logging
- Health endpoint
- Docker runtime configuration

## Suggested Migration Order

1. Create `task11.Domain`, `task11.Application`, and `task11.Infrastructure` projects.
2. Move entities and enums from `task11.Data` to `task11.Domain`.
3. Move service interfaces, repository interfaces, DTOs, validators, and exceptions to `task11.Application`.
4. Move EF Core, migrations, interceptors, repository implementations, currency clients, password hashing, JWT generation, and system clock to `task11.Infrastructure`.
5. Replace `ApplicationCore -> Data` dependency with `Application -> Domain`.
6. Add `Infrastructure -> Application` and `Infrastructure -> Domain`.
7. Update `Web` to reference `Application` and `Infrastructure`.
8. Replace inline DI registration in `Program.cs` with `AddApplication()` and `AddInfrastructure(...)`.
9. Update namespaces and tests.
10. Run build, unit tests, and Docker Compose verification.

## What Should Stay The Same

The refactor should preserve existing behavior:

- CRUD for income and expense types.
- CRUD for financial operations.
- Soft delete for financial operations and other deletable entities.
- Daily and period reports.
- Request/response logging with sensitive data redaction.
- Correlation ID and user ID in logs.
- User CRUD and default admin user.
- JWT login and protected endpoints.
- Wallet ownership isolation.
- Base currency and transaction currency conversion.
- Original transaction amount appended to operation note.
- Swagger UI at `/swagger`.
- PostgreSQL persistence.
- Docker Compose startup at `http://localhost:8080`.

## Summary

The current solution is acceptable for the educational task because it follows the requested `Controllers -> Services -> Repositories` structure.

For stricter Clean Architecture, the key improvement is to invert the dependency between application logic and data access:

```text
Current:
Web -> ApplicationCore -> Data

Recommended:
Web -> Application -> Domain
Web -> Infrastructure -> Application -> Domain
```

The most important practical changes are:

- Move domain entities out of `Data` into `Domain`.
- Move EF Core and concrete repositories into `Infrastructure`.
- Keep repository interfaces in `Application`.
- Ensure `Application` depends only on `Domain`, not on EF Core or database code.
- Keep `Web` as the composition root and HTTP boundary.
