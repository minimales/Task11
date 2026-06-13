# task11 Conventions

This document captures the conventions, layout, and architectural decisions for the
`task11.*` multi-project solution. It mirrors the sibling solution in
`H:\Work\dotNet_Task\repo\task10` and reconciles those conventions with the feature
set being ported from the working implementation at
`H:\Work\dotNet_Task\test\Task11\src\PersonalFinance.Api`.

Later phases (porting entities, configurations, services, controllers, tests) MUST
follow this document.

## Status

Phase 0 (scaffold) is complete: six empty-but-compiling projects, a green solution
build, and one passing smoke test per test project. All `*.cs` files currently in the
`task11.*` projects are temporary placeholders (`Placeholder.cs`, `Program.cs`,
`ScaffoldSmokeTests.cs`) and will be replaced when real source lands.

## Solution layout

```
H:\Work\dotNet_Task\test\Task11\
  global.json                              # pins SDK 9.0.101, rollForward latestFeature
  task11.sln                               # references all six projects below
  docs\CONVENTIONS-task11.md               # this file
  docs\ARCHITECTURE.md                     # behaviour spec for the ported app

  task11.Data\                             # Microsoft.NET.Sdk
  task11.ApplicationCore\                  # Microsoft.NET.Sdk -> task11.Data
  task11.Web\                              # Microsoft.NET.Sdk.Web -> task11.ApplicationCore

  task11.Data.Tests.UnitTesting.MSTest\            -> task11.Data
  task11.ApplicationCore.Tests.UnitTesting.MSTest\ -> task11.Data, task11.ApplicationCore
  task11.Web.Tests.UnitTesting.MSTest\             -> task11.Data, task11.ApplicationCore, task11.Web
```

## Project / csproj conventions

- `net9.0`, `ImplicitUsings enable`, `Nullable enable`, `LangVersion latest` (test projects).
- Root namespace is **lowercase** `task11.*` (e.g. `task11.Data`, `task11.ApplicationCore`,
  `task11.Web`). Set explicitly via `<RootNamespace>` in each csproj.
- **File-scoped namespaces** everywhere (`namespace task11.Data;`).
- SDKs: Data + ApplicationCore use `Microsoft.NET.Sdk`; Web uses `Microsoft.NET.Sdk.Web`.
- Project references form the layering chain: Web -> ApplicationCore -> Data.
- Test projects: `IsPackable=false`, `<Using Include="Microsoft.VisualStudio.TestTools.UnitTesting" />`,
  Web tests additionally add `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.
- **NO Moq, NO FluentAssertions, NO xUnit** — MSTest + plain `Assert.*` only.

## Package versions (pinned)

EF / data packages are pinned to the **9.0.x** line (EF Core 9, Npgsql EF Core 9).

| Project | Package | Version |
|---|---|---|
| task11.Data | Microsoft.EntityFrameworkCore | 9.0.4 |
| task11.Data | Microsoft.EntityFrameworkCore.Relational | 9.0.4 |
| task11.Data | Microsoft.EntityFrameworkCore.Design (PrivateAssets=all) | 9.0.4 |
| task11.Data | Microsoft.EntityFrameworkCore.InMemory | 9.0.4 |
| task11.Data | Npgsql.EntityFrameworkCore.PostgreSQL | 9.0.4 |
| task11.ApplicationCore | FluentValidation | 11.11.0 |
| task11.ApplicationCore | Microsoft.Extensions.Http.Polly | 9.0.4 |
| task11.ApplicationCore | Microsoft.Extensions.Caching.Memory | 9.0.4 |
| task11.ApplicationCore | Microsoft.Extensions.Configuration.Abstractions | 9.0.4 |
| task11.ApplicationCore | System.IdentityModel.Tokens.Jwt | 8.2.1 |
| task11.Web | Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.4 |
| task11.Web | Microsoft.EntityFrameworkCore.Design (PrivateAssets=all) | 9.0.4 |
| task11.Web | Swashbuckle.AspNetCore | 7.2.0 |
| task11.Web | Serilog.AspNetCore | 9.0.0 |
| task11.Web | Serilog.Sinks.Console | 6.0.0 |
| task11.Web | FluentValidation.DependencyInjectionExtensions | 11.11.0 |
| task11.Web | SharpGrip.FluentValidation.AutoValidation.Mvc | 1.5.0 |
| all test projects | Microsoft.NET.Test.Sdk | 17.12.0 |
| all test projects | MSTest | 3.6.4 |
| all test projects | Microsoft.EntityFrameworkCore.InMemory | 9.0.4 |

SDK pinned via `global.json` to `9.0.101` (`rollForward: latestFeature`).

## Folder / namespace layout (target, populated by later phases)

### task11.Data (`namespace task11.Data[.X]`)
- `Entities/` — `*Entity` types plus `BaseEntity`.
- `Entities/Enums/` — `OperationKind`.
- `EntityConfigurations/` — `*EntityConfiguration` (`IEntityTypeConfiguration<T>`).
- `AppDbContext.cs` — the `DbContext` (see decision below).
- `DesignTimeDbContextFactory.cs` — `IDesignTimeDbContextFactory<AppDbContext>`.
- `Migrations/` — EF migrations.

### task11.ApplicationCore (`namespace task11.ApplicationCore[.X]`)
- `Models/` — DTOs suffixed `*Model`.
- `Validators/` — FluentValidation validators over the `*Model` request types.
- `Repositories/` (+ `Repositories/Abstractions/`) — repository layer over `DbContextFactory`.
- `Services/` (+ `Services/Abstractions/`) — orchestrate repositories + collaborators.
- `Auth/` — `PasswordHasher`, `JwtTokenGenerator`, `JwtSettings`.
- `Currency/` — `FrankfurterClient`, `PrivatBankClient`, `FxOptions`, `CurrencyConverter`, `ICurrencyConverter`.
- `DbContextFactory.cs` — factory holding `(connectionString, useInMemory, IClock)`.
- `IClock` / `SystemClock`.
- Domain exceptions: `NotFoundException`, `ForbiddenException`, `ConflictException`, `FxUnavailableException`.

### task11.Web (`namespace task11.Web[.X]`)
- `Program.cs` — DI wiring (inline or small module classes) + middleware pipeline.
- `Controllers/` — `AuthController`, `WalletsController`, `OperationTypesController`,
  `OperationsController`, `ReportsController`.
- `Middleware/` — `CorrelationIdMiddleware`, `ExceptionHandlingMiddleware`, `RequestResponseLoggingMiddleware`.
- `Infrastructure/Auth/` — `ICurrentUser` + `CurrentUser` (reads `HttpContext`).
- `Logging/` — `LogSanitizer`.
- `appsettings*.json`.

## Rename map

### Entities (task11.Data/Entities) — `*Entity` suffix
| Ported (PersonalFinance.Api) | task11 |
|---|---|
| `OperationType` | `OperationTypeEntity` |
| `FinancialOperation` | `FinancialOperationEntity` |
| `User` | `UserEntity` |
| `Wallet` | `WalletEntity` |
| `BaseEntity` | `BaseEntity` (unchanged) |
| `OperationKind` (enum) | `OperationKind` (unchanged) — `task11.Data/Entities/Enums/OperationKind.cs` |

EF configs are named `*EntityConfiguration` in `task11.Data/EntityConfigurations/`.

### DTOs (task11.ApplicationCore/Models) — `*Model` suffix
| Ported DTO | task11 Model |
|---|---|
| `LoginRequest` | `LoginModel` |
| `LoginResponse` | `AuthTokenModel` |
| `CreateUserRequest` / `UpdateUserRequest` | `CreateUserModel` / `UpdateUserModel` |
| `UserResponse` | `UserModel` |
| `CreateWalletRequest` / `UpdateWalletRequest` | `CreateWalletModel` / `UpdateWalletModel` |
| `WalletResponse` | `WalletModel` |
| `CreateOperationTypeRequest` / `UpdateOperationTypeRequest` | `CreateOperationTypeModel` / `UpdateOperationTypeModel` |
| `OperationTypeResponse` | `OperationTypeModel` |
| `CreateOperationRequest` / `UpdateOperationRequest` | `CreateOperationModel` / `UpdateOperationModel` |
| `OperationResponse` | `OperationModel` |
| `DailyReportRequest` | `DailyReportModel` |
| `PeriodReportRequest` | `PeriodReportModel` |
| `ReportResponse` (+ line type) | `ReportModel` (+ `ReportOperationLineModel`) |
| `ErrorResponse` | `ErrorModel` |

Validators validate the `*Model` request types.

## Architectural decisions

### DbContext lives in task11.Data
`AppDbContext(string connectionString, bool useInMemory, IClock clock)`:
- `OnConfiguring`: if `useInMemory` -> `UseInMemoryDatabase(connectionString)` +
  `Ignore(InMemoryEventId.TransactionIgnoredWarning)`; else `UseNpgsql(connectionString)`.
  ALWAYS `optionsBuilder.AddInterceptors(new SoftDeleteInterceptor(), new AuditInterceptor(clock))`.
- `OnModelCreating`: `ApplyConfigurationsFromAssembly` + a global soft-delete query filter
  on every `BaseEntity`-derived type.

### DesignTimeDbContextFactory (task11.Data)
Implements `IDesignTimeDbContextFactory<AppDbContext>` with a local Npgsql connection
string (`useInMemory: false`, `SystemClock`) so `dotnet ef migrations add` works without a
live DB. (task10's variant reads `appsettings.json` from the Web project; task11 may use a
hardcoded local connection string per the architecture decision.)

### DbContextFactory over repositories (task11.ApplicationCore)
- `DbContextFactory` holds `(connectionString, useInMemory, IClock)`; `virtual CreateDbContext()`
  returns `new AppDbContext(...)`; provides `MigrateIfRelational()`.
- This mirrors task10's `DbContextFactory` (which the test suite subclasses).
- **Context-per-operation**: each repository takes a `DbContextFactory`; every repository
  method does `await using var ctx = _factory.CreateDbContext();` and performs its
  read/write + `SaveChanges` within that scope. This matches task10's philosophy.
- **Soft delete** = set `IsDeleted` (the `SoftDeleteInterceptor` also rewrites `Remove` into a
  soft delete; the global query filter hides soft-deleted rows).
- **Services** orchestrate repositories plus collaborators (`ICurrencyConverter`,
  `IWalletService`, `ICurrentUser`). Controllers -> Services -> Repositories.

### Web layer wiring (task11.Web/Program.cs)
- Registers `DbContextFactory` as a **singleton** (`connStr` + `useInMemory: false` + `SystemClock`).
- All repositories + services registered **scoped**.
- JWT, Swagger (`/swagger`), FluentValidation, currency typed `HttpClient`s
  (Frankfurter + PrivatBank) + memory cache.
- Middleware pipeline order:
  `CorrelationId -> Exception -> Swagger(anon) -> Authentication -> RequestResponseLogging -> Authorization -> controllers`.
- On startup: `MigrateIfRelational` + seed `admin/admin` + shared wallet.

## Test conventions

- MSTest: `[TestClass]`, `[TestMethod]`, `[DataTestMethod]` + `[DataRow]`.
- Plain `Assert.*` only: `AreEqual`, `IsNull`, `IsNotNull`, `IsTrue`, `ThrowsException`,
  `ThrowsExceptionAsync`. NO Moq, NO FluentAssertions.
- **No namespace** on test classes (matches task10).
- Test method naming: `Test_<Class>_<Method>_<Scenario>_<Expected>`.
- For data access, use `Microsoft.EntityFrameworkCore.InMemory` plus an
  `InMemoryDbContextFactory` subclass of `DbContextFactory` (constructed with a unique
  database name, `useInMemory: true`), exactly like task10's
  `task10.ApplicationCore.Tests.UnitTesting.MSTest/InMemoryDbContextFactory.cs`.
- Replace Moq-style collaborators with hand-rolled fakes/stubs implementing the relevant
  interfaces (`ICurrencyConverter`, `ICurrentUser`, `IClock`, repository abstractions, etc.).
- Each test project currently ships one placeholder smoke test
  (`Test_Scaffold_<Layer>Tests_Compiles_Passes`) that later phases replace.
