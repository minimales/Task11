# Personal Finance Web API — Architecture

A personal finance management Web API (users + JWT, per-user wallets, operation types,
financial operations, daily/period reports, and online currency conversion) built on
**.NET 9 / ASP.NET Core**, **EF Core 9** over **PostgreSQL (Npgsql)**, and tested with
**MSTest**.

This document describes the **current** state of the codebase.

---

## 1. Clean Architecture & the Dependency Rule

The solution follows Clean Architecture: dependencies point **inward**, toward a
framework-free core. The reference graph is:

```
        task11.Web  (host / composition root)
          │   │
          │   └──────────────┐
          ▼                  ▼
task11.Infrastructure ──> task11.ApplicationCore  (no outward references)
```

| Project | Role | References |
|---|---|---|
| **task11.ApplicationCore** | Framework-free core: entities, domain exceptions, abstractions (repository + service interfaces), application services, validators, auth/clock contracts. | **None** (no project references; no EF Core, ASP.NET Core, or Npgsql packages). |
| **task11.Infrastructure** | EF Core / Npgsql persistence, repository implementations, FX HTTP clients, system clock — all implementing ApplicationCore interfaces. | `task11.ApplicationCore` |
| **task11.Web** | ASP.NET Core host: controllers, middleware, DI composition, the ProblemDetails pipeline, Swagger, Serilog. | `task11.ApplicationCore` **and** `task11.Infrastructure` |

**ApplicationCore depends on nothing.** It contains only entities, interfaces, services,
validators, and contracts. It references `FluentValidation`,
`Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`, and
`System.IdentityModel.Tokens.Jwt` — abstractions only, no infrastructure frameworks.

**Infrastructure** owns `AppDbContext`, EF entity configurations, interceptors, migrations,
the concrete repositories, `SystemClock`, and the `CurrencyConverter` + `FrankfurterClient` /
`PrivatBankClient`. Every concrete type here implements an interface declared in
ApplicationCore (`IUserRepository`, `IWalletRepository`, `ICurrencyConverter`, `IClock`, …),
so the core never sees EF or HTTP.

**Web** is the only project that knows about both. It wires interfaces to implementations,
hosts the controllers, and never references `AppDbContext` from a controller — controllers
talk to `Services/Abstractions` only.

### Architecture guardrail (NetArchTest)

`task11.ApplicationCore.Tests.UnitTesting.MSTest/ArchitectureTests.cs` enforces the
dependency rule at test time with **NetArchTest.Rules**. Three tests assert that types in
`task11.ApplicationCore.*` have **no dependency on**:

- `task11.Infrastructure`
- `Microsoft.EntityFrameworkCore`
- `Microsoft.AspNetCore` and `Npgsql`

If anyone reintroduces an infrastructure dependency into the core, the build's test run fails.

---

## 2. Solution Layout

```
task11.sln
├─ task11.ApplicationCore/                 framework-free core
│  ├─ Entities/                            BaseEntity, User, Wallet, OperationType,
│  │  └─ Enums/                            FinancialOperation, OperationKind
│  ├─ Models/                              request/response DTOs
│  ├─ Services/  + Services/Abstractions/  application services + interfaces
│  ├─ Repositories/Abstractions/          repository interfaces
│  ├─ Validators/                          FluentValidation validators
│  ├─ Auth/                                JwtSettings, JwtTokenGenerator,
│  │                                       PasswordHasher, ICurrentUser
│  ├─ Currency/                            ICurrencyConverter
│  ├─ IClock.cs
│  └─ *Exception.cs                        NotFound, Forbidden, Conflict, FxUnavailable
│
├─ task11.Infrastructure/                  EF Core / Npgsql + integrations
│  ├─ Persistence/
│  │  ├─ AppDbContext.cs, DbContextFactory.cs, DesignTimeDbContextFactory.cs
│  │  ├─ ModelBuilderExtensions.cs         (soft-delete query filter)
│  │  ├─ EntityConfigurations/             one IEntityTypeConfiguration<T> per entity
│  │  ├─ Interceptors/                     AuditInterceptor, SoftDeleteInterceptor
│  │  └─ Migrations/                       InitialCreate
│  ├─ Repositories/                        User/Wallet/OperationType/Operation/Report
│  ├─ Currency/                            CurrencyConverter, FrankfurterClient,
│  │                                       PrivatBankClient, FxOptions
│  └─ Time/                                SystemClock
│
├─ task11.Web/                             ASP.NET Core host
│  ├─ Program.cs                           composition root; wires every layer
│  ├─ Controllers/                         Auth, Wallets, OperationTypes,
│  │                                       Operations, Reports
│  ├─ Middleware/                          CorrelationIdMiddleware,
│  │                                       RequestResponseLoggingMiddleware
│  ├─ Infrastructure/
│  │  ├─ GlobalExceptionHandler.cs         IExceptionHandler → ProblemDetails
│  │  ├─ ProblemDetailsEnricher.cs         adds traceId / correlation id
│  │  ├─ Auth/CurrentUser.cs               ICurrentUser implementation
│  │  └─ Logging/LogSanitizer.cs           secret redaction
│  ├─ appsettings*.json
│
└─ task11.{ApplicationCore,Infrastructure,Web}.Tests.UnitTesting.MSTest/   MSTest
```

---

## 3. Target Framework & Key Packages

- **TFM:** `net9.0` across all projects. **DB:** PostgreSQL via Npgsql, **EF Core 9** (9.0.4).
- **SDK pin:** `global.json` → `9.0.101` (`rollForward: latestFeature`).

| Project | Notable packages |
|---|---|
| ApplicationCore | FluentValidation 11.11; Microsoft.Extensions.{Logging.Abstractions, Options} 9.0.4; System.IdentityModel.Tokens.Jwt 8.2.1 |
| Infrastructure | Microsoft.EntityFrameworkCore(.Relational/.Design/.InMemory) 9.0.4; Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4; Microsoft.Extensions.Http(.Resilience) 9.x; Microsoft.Extensions.Caching.Memory 9.0.4 |
| Web | Microsoft.AspNetCore.Authentication.JwtBearer 9.0.4; Microsoft.Extensions.Http.Resilience 9.0.0; Swashbuckle.AspNetCore 7.2.0; Serilog.AspNetCore 9.0.0; FluentValidation.DependencyInjectionExtensions 11.11; SharpGrip.FluentValidation.AutoValidation.Mvc 1.5.0 |
| Tests | Microsoft.NET.Test.Sdk 17.12; MSTest 3.6.4; Microsoft.EntityFrameworkCore.InMemory 9.0.4; NetArchTest.Rules 1.3.2 (ApplicationCore tests only) |

The deprecated `Microsoft.Extensions.Http.Polly` package is **not used** anywhere. HTTP
resilience comes from `Microsoft.Extensions.Http.Resilience` (see §5); Polly v8 (`Polly.Core`)
is present only as a transitive dependency of that package.

---

## 4. Error Handling — ProblemDetails Pipeline

Errors are returned as RFC 7807 `application/problem+json` through the modern
`IExceptionHandler` pipeline (the old custom `ExceptionHandlingMiddleware` has been removed):

- **`AddProblemDetails(...)`** registers the ProblemDetails service and a
  `CustomizeProblemDetails` callback that defaults `Type` to `about:blank` and runs
  `ProblemDetailsEnricher.Enrich` (adds `traceId` / correlation id) — a single enrichment point.
- **`AddExceptionHandler<GlobalExceptionHandler>()`** maps domain exceptions to status codes:
  `NotFoundException → 404`, `ForbiddenException → 403`, `ConflictException → 409`,
  `FxUnavailableException → 503`, `ValidationException → 400`, everything else `→ 500`
  (5xx detail is scrubbed; the full exception is logged).
- **`app.UseExceptionHandler()`** activates the handler; **`app.UseStatusCodePages()`** gives
  empty-body challenge responses (401/403/404/405) a ProblemDetails body too.
- Validation 400s are unified to the same contract: `ApiBehaviorOptions
  .InvalidModelStateResponseFactory` emits a `ValidationProblemDetails` (also enriched), so
  SharpGrip auto-validation and built-in `[ApiController]` validation produce one shape.

---

## 5. HTTP Resilience

The FX integrations (`FrankfurterClient`, `PrivatBankClient`) are registered as typed
`HttpClient`s in `Program.cs`, each followed by **`.AddStandardResilienceHandler(...)`** from
`Microsoft.Extensions.Http.Resilience`. The standard handler bundles retry (with exponential
backoff + jitter), a circuit breaker, and total/attempt timeouts; retry attempts are tuned via
`Fx:RetryCount`.

Startup database migration uses Polly v8's `ResiliencePipelineBuilder` directly
(`MigrateAndSeedAsync`) to retry transient connection failures while the database container
becomes ready.

---

## 6. Domain & Persistence Notes

- **Entities** (`BaseEntity` + `User`, `Wallet`, `OperationType`, `FinancialOperation`,
  `OperationKind` enum) live in ApplicationCore and carry audit/soft-delete fields
  (`CreatedAtUtc`, `UpdatedAtUtc`, `IsDeleted`, `DeletedAtUtc`).
- **AppDbContext** (Infrastructure) applies one `IEntityTypeConfiguration<T>` per entity, a
  global `HasQueryFilter(e => !e.IsDeleted)` for every `BaseEntity`, `decimal(18,2)` for
  amounts, and `timestamptz` (`DateTimeKind.Utc`) for all dates.
- **SoftDeleteInterceptor** rewrites `EntityState.Deleted` into a soft delete; combined with the
  query filter, physical deletes are effectively impossible. **AuditInterceptor** stamps audit
  timestamps using the injected `IClock` (`SystemClock`).
- **Currency conversion:** UAH pairs route to **PrivatBank**, all others to **Frankfurter**
  (ECB, no key, historical). Immutable historical rates are cached in `IMemoryCache`; on hard
  failure after retries the service throws `FxUnavailableException` → **503**, never storing an
  unconverted amount as converted.

---

## 7. Cross-Cutting Concerns (Web)

- **Auth:** JWT bearer (HS256), global fallback `RequireAuthenticatedUser` policy, PBKDF2
  password hashing, `ICurrentUser` (implemented by `CurrentUser`) for wallet ownership checks
  enforced in the service layer.
- **Logging:** Serilog structured JSON. `CorrelationIdMiddleware` reads/generates
  `X-Correlation-Id` and pushes it (plus `UserId`) into the log scope;
  `RequestResponseLoggingMiddleware` logs request/response with `LogSanitizer` redacting
  secrets (passwords, tokens, `Authorization`, etc.).
- **Validation:** FluentValidation validators in ApplicationCore, wired via assembly scan +
  SharpGrip AutoValidation; failures become the unified ValidationProblemDetails 400 (see §4).

---

## 8. Tests

Three MSTest projects mirror the production projects:

- **task11.ApplicationCore.Tests** — service/validator unit tests **plus the NetArchTest
  architecture guardrail** (§1).
- **task11.Infrastructure.Tests** — repository / EF (InMemory) / FX tests.
- **task11.Web.Tests** — controller / pipeline tests.

Run the full suite with `dotnet test task11.sln`.
