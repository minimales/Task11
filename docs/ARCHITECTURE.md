# Personal Finance Web API — Authoritative Build Specification

**Style:** Single ASP.NET Core Web API project, folder-based layering mirroring `Controllers → Services → Repositories` 1:1. `net8.0`, PostgreSQL via Npgsql/EF Core 8. Dockerized (app + postgres). Full scope including Users+JWT, per-user wallets, and online currency conversion.

This document is FROZEN. Downstream build agents follow it exactly.

---

## 1. Architecture & Dependency Rule

The project uses `Abstractions` sub-folders to enforce the dependency direction by interface:

- **Controllers** depend only on `Services/Abstractions`.
- **Services** depend only on `Repositories/Abstractions` (and `Services/Abstractions` of collaborators, e.g. `ICurrencyConverter`).
- **Controllers never reference `AppDbContext`.**

```
Controllers  ──>  Services/Abstractions  ──>  Repositories/Abstractions  ──>  AppDbContext
```

Two grafts strengthen integrity beyond the base proposal:

| Graft | Source | What it guarantees |
|---|---|---|
| FX integrity contract: `IMemoryCache` keyed on `(from,to,date)` + Polly retry + explicit **503** on hard failure | Proposal 1 | Never silently store an unconverted amount as if converted. |
| `SoftDeleteInterceptor` rewriting `EntityState.Deleted` → `IsDeleted=true` | Proposal 2 | Any accidental `Remove()` becomes a soft delete; pairs with the global query filter so physical delete is impossible anywhere. |

---

## 2. Solution / Folder Layout

```
task11.sln
├─ docker-compose.yml          Dockerfile          .dockerignore
├─ .env.example                .gitignore          README.md
├─ task11.Data/               (data layer)
│  ├─ AppDbContext.cs / DesignTimeDbContextFactory.cs / ModelBuilderExtensions.cs
│  ├─ Entities/ (BaseEntity, OperationTypeEntity, FinancialOperationEntity, UserEntity, WalletEntity, Enums/OperationKind)
│  ├─ EntityConfigurations/   (per-entity EF Core configurations)
│  ├─ Interceptors/ (AuditInterceptor, SoftDeleteInterceptor)
│  ├─ Migrations/             (InitialCreate)
│  └─ IClock.cs / SystemClock.cs
├─ task11.ApplicationCore/    (application/service layer)
│  ├─ Services/Abstractions/  + Services/*.cs
│  ├─ Repositories/Abstractions/ + Repositories/*.cs
│  ├─ Models/ (Auth, Users, Wallets, OperationTypes, Operations, Reports, Error)
│  ├─ Validators/             (FluentValidation, 1:1 with request models)
│  ├─ Auth/ (JwtSettings, JwtTokenGenerator, PasswordHasher, ICurrentUser)
│  ├─ Currency/ (FrankfurterClient, PrivatBankClient, CurrencyConverter, FxOptions)
│  └─ Exceptions (NotFound, Forbidden, Conflict, FxUnavailable) + DbContextFactory.cs
├─ task11.Web/                (host / API layer)
│  ├─ Program.cs               # thin host; wires up every layer
│  ├─ appsettings.json / appsettings.Development.json
│  ├─ Controllers/            (Auth, Wallets, OperationTypes, Operations, Reports)
│  ├─ Middleware/             (CorrelationId, RequestResponseLogging, ExceptionHandling)
│  └─ Infrastructure/ (Auth/CurrentUser, Logging/LogSanitizer)
└─ task11.{Data,ApplicationCore,Web}.Tests.UnitTesting.MSTest/ (MSTest)
```

---

## 3. Target Framework, Database, NuGet Packages

- **TFM:** `net8.0`  **DB:** PostgreSQL 16 (Npgsql EF Core 8).

| Package | Version | Project |
|---|---|---|
| Microsoft.EntityFrameworkCore | 8.0.11 | Api |
| Microsoft.EntityFrameworkCore.Relational | 8.0.11 | Api |
| Microsoft.EntityFrameworkCore.Design | 8.0.11 | Api |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 | Api |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.11 | Api |
| System.IdentityModel.Tokens.Jwt | 8.2.1 | Api |
| FluentValidation | 11.9.2 | Api |
| FluentValidation.DependencyInjectionExtensions | 11.9.2 | Api |
| SharpGrip.FluentValidation.AutoValidation.Mvc | 1.5.0 | Api |
| Swashbuckle.AspNetCore | 6.9.0 | Api |
| Serilog.AspNetCore | 8.0.3 | Api |
| Serilog.Sinks.Console | 6.0.0 | Api |
| Microsoft.Extensions.Http.Polly | 8.0.11 | Api |
| Microsoft.Extensions.Caching.Memory | 8.0.1 | Api |
| xunit / xunit.runner.visualstudio | 2.9.2 / 2.8.2 | Tests |
| Microsoft.NET.Test.Sdk | 17.11.1 | Tests |
| Moq / FluentAssertions | 4.20.72 / 6.12.1 | Tests |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.11 | Tests |

(EF 8.0.x and Npgsql 8.0.11 are the correct line for .NET 8 LTS; Npgsql 10.x targets newer runtimes and is intentionally avoided.)

---

## 4. Entities (exact C# names & types)

### `BaseEntity` (abstract — all entities inherit)
| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | App-generated `Guid.NewGuid()`; no DB round-trip. |
| `CreatedAtUtc` | `DateTime` | Set on Added (audit). UTC `timestamptz`. |
| `UpdatedAtUtc` | `DateTime?` | Set on Modified (audit). |
| `IsDeleted` | `bool` | Soft-delete flag; default false. |
| `DeletedAtUtc` | `DateTime?` | Set when soft-deleted. |

### `OperationKind` enum: `Income = 1`, `Expense = 2` (stored as int).

### `OperationType : BaseEntity`
| Field | Type | Notes |
|---|---|---|
| `Name` | `string` | Required 1..100. e.g. "Salary". |
| `Description` | `string?` | ≤500. e.g. "Monthly salary". |
| `Kind` | `OperationKind` | Income/Expense — drives report totals. |
| `WalletId` | `Guid` | FK → Wallet; types are wallet-scoped. |
| `Wallet` | `Wallet` | Navigation. |
| `Operations` | `ICollection<FinancialOperation>` | Navigation. |

> **Design note:** the spec's type model is `{id,name,description}`; `Kind` is added because Daily/Period reports must split income vs expense. Every operation inherits its kind from its type, so totals are a pure SQL `SUM(...) WHERE Kind = ...`.

### `FinancialOperation : BaseEntity` (soft delete required here)
| Field | Type | Notes |
|---|---|---|
| `OperationTypeId` | `Guid` | The spec's `typeId`. FK → OperationType. |
| `OperationType` | `OperationType` | Navigation (carries Kind). |
| `WalletId` | `Guid` | Denormalized for fast scoped queries + isolation. |
| `Wallet` | `Wallet` | Navigation. |
| `Amount` | `decimal` | **Always in wallet base currency.** `numeric(18,2)` via `HasPrecision(18,2)`. |
| `OccurredAtUtc` | `DateTime` | The operation "date". UTC `timestamptz`. FX lookup + report date. |
| `Note` | `string?` | ≤500 user chars; converted-original audit string appended after conversion. |

### `User : BaseEntity`
| Field | Type | Notes |
|---|---|---|
| `Username` | `string` | Unique (filtered index). 3..50, `^[a-zA-Z0-9_.-]+$`. |
| `PasswordHash` | `string` | PBKDF2 `{iterations}.{salt}.{hash}`. **Never logged/serialized.** |
| `Role` | `string` | "Admin" \| "User". Default "User". |
| `OwnedWallets` | `ICollection<Wallet>` | Navigation. |

### `Wallet : BaseEntity`
| Field | Type | Notes |
|---|---|---|
| `Name` | `string` | Required 1..100. |
| `BaseCurrency` | `string` | ISO-4217 `^[A-Z]{3}$`, default "UAH", `CHAR(3)`. Immutable once operations exist. |
| `OwnerUserId` | `Guid?` | NULL = shared wallet (all users); non-null = personal (owner only). |
| `Owner` | `User?` | Navigation. |
| `Operations` | `ICollection<FinancialOperation>` | Navigation. |
| `OperationTypes` | `ICollection<OperationType>` | Navigation. |

---

## 5. DbContext & EF Core

**`AppDbContext`** (`Infrastructure/Persistence/AppDbContext.cs`) with DbSets:
`Users`, `Wallets`, `OperationTypes`, `FinancialOperations`.

- `OnModelCreating` → `ApplyConfigurationsFromAssembly` (one `IEntityTypeConfiguration<T>` per entity), then `ModelBuilderExtensions.ApplySoftDeleteFilter` loops every type where `typeof(BaseEntity).IsAssignableFrom(clrType)` and adds `HasQueryFilter(e => !e.IsDeleted)`.
- **Decimal:** `FinancialOperation.Amount` → `HasPrecision(18,2)`.
- **DateTime (Npgsql 8):** all `DateTime` are `timestamp with time zone` and **MUST be `DateTimeKind.Utc`**; the API converts inbound dates to UTC at the boundary to avoid the `Kind=Unspecified` error.
- **Filtered unique indexes:** `User.Username` `WHERE IsDeleted=false`; `(WalletId, Name)` on `OperationType` `WHERE IsDeleted=false`. Index `FinancialOperation(WalletId, OccurredAtUtc)`.
- **Soft delete (double enforcement):** `SoftDeleteInterceptor` rewrites `EntityState.Deleted` → `Modified` with `IsDeleted=true, DeletedAtUtc=now`; the global query filter hides deleted rows from all reads.
- **Audit:** `AuditInterceptor` (or `SaveChangesAsync` override) stamps `CreatedAtUtc`/`UpdatedAtUtc` using injected `IClock`.
- **Migrations:** code-first `InitialCreate` in `Infrastructure/Persistence/Migrations`, applied on startup via `Database.Migrate()` (Polly retry), then `DbSeeder`.

---

## 6. Endpoints

All routes require a valid JWT via a **global fallback authorization policy** except `/api/auth/login`, `/health`, `/swagger*` (`[AllowAnonymous]`). Admin routes use `[Authorize(Roles="Admin")]`.

| Method | Route | Auth | Module |
|---|---|---|---|
| POST | `/api/auth/login` | Anon | AuthModule |
| GET/POST | `/api/users`, `/api/users/{id}` (GET/PUT/DELETE) | Admin | AuthModule |
| GET | `/api/wallets`, `/api/wallets/{id}` | Auth | WalletModule |
| POST/PUT/DELETE | `/api/wallets`, `/api/wallets/{id}` | Auth (owner) | WalletModule |
| GET/POST | `/api/wallets/{walletId}/operation-types` | Auth | OperationTypeModule |
| GET/PUT/DELETE | `/api/operation-types/{id}` | Auth | OperationTypeModule |
| GET | `/api/wallets/{walletId}/operations` | Auth | OperationModule |
| GET/POST/PUT/DELETE | `/api/operations`, `/api/operations/{id}` | Auth | OperationModule |
| GET | `/api/reports/daily?walletId=&date=` | Auth | ReportModule |
| GET | `/api/reports/period?walletId=&startDate=&endDate=` | Auth | ReportModule |
| GET | `/health`, `/swagger` | Anon | Scaffold |

**Reports** return `{ TotalIncome, TotalExpense, NetResult (income−expense), Currency, Operations[] }`, computed server-side via `SUM(Amount)` grouped by `OperationType.Kind` over UTC ranges `[date, date+1)` (daily) / `[start, end+1)` (period).

---

## 7. Cross-Cutting Concerns

### 7.1 Logging Middleware
Order: **Correlation → Exception → RequestResponseLogging** (before routing).
- **CorrelationIdMiddleware:** reads `X-Correlation-Id` or generates `Guid.NewGuid()`; stores in `HttpContext.Items`, pushes into the log scope, echoes the `X-Correlation-Id` response header (**request↔result correlation**). Scope also carries `UserId` (JWT `sub`, or `anonymous`) so **one user's sequence is separable from another's**.
- **RequestResponseLoggingMiddleware:** request entry `{Method, full Url (GetEncodedUrl), Body}` via `EnableBuffering()` + rewind; response entry `{StatusCode, Body, ElapsedMs}` by swapping `Response.Body` for a `MemoryStream` and copying back.
- **Secret redaction (`LogSanitizer`):** JSON-parse, recursively replace deny-listed keys with `"***"`. **Deny-list:** `password, passwordHash, accessToken, token, secret, authorization, refreshToken, apiKey`. The `Authorization` header is never logged. Non-JSON or `>32 KB` bodies → `"[omitted: <size> bytes]"`. Login password (in) and token (out) are redacted by the same rule.
- Output: structured JSON (Serilog console); every line carries `CorrelationId` + `UserId`.

### 7.2 JWT Auth & Seeding
- `JwtSettings { Issuer, Audience, Secret, ExpiryMinutes=60 }` from config section `Jwt`; `Secret` from env `JWT__Secret`, never committed (min 32 chars for HS256).
- `AddJwtBearer` validates Issuer/Audience/Lifetime/SigningKey (HMAC-SHA256). Claims: `sub`, `name`, `role`, `jti`, `exp`. Global fallback policy `RequireAuthenticatedUser` → forgotten `[Authorize]` fails closed.
- `PasswordHasher`: PBKDF2 (`Rfc2898DeriveBytes`, SHA-256, 100k iters, per-user salt).
- `DbSeeder` (after `Migrate()`): if no users, insert `admin`/hash("admin")/`Admin` + one shared wallet (`OwnerUserId=null`, `BaseCurrency="UAH"`). Idempotent.

### 7.3 Wallet Ownership Isolation
`ICurrentUser` (Scoped) exposes `UserId`/`Role`/`IsAdmin`. A wallet is accessible if `OwnerUserId==null` (shared) OR `OwnerUserId==UserId` OR `IsAdmin`. Enforced in the **service layer**: every operation/type/report call resolves the target wallet and runs `EnsureCanAccess(wallet)` → `ForbiddenException` (403). Because `FinancialOperation` carries `WalletId`, a guessed operation id is ownership-checked before any data returns. Lists filter to `OwnerUserId==me || OwnerUserId==null`.

### 7.4 Currency Conversion — PrivatBank (UAH) + Frankfurter (free, historical, no key)
**Provider routing (in `CurrencyConverter`):** any pair involving **UAH** uses **PrivatBank**
(`GET /p24api/exchange_rates?json&date=dd.MM.yyyy`, NBU official rate `saleRateNB` = UAH per unit;
`UAH→foreign` is the reciprocal) — the ECB-based Frankfurter feed does **not** publish the hryvnia,
which is the task's base-currency example. All other pairs use **Frankfurter**:
`https://api.frankfurter.dev` (mirror `api.frankfurter.app`) — ECB-sourced, no key, no rate limit,
historical rates by exact date back to 1999, back-dating supported. Both are typed `HttpClient`s with
**Polly retry (`Fx:RetryCount`, default 3, exp backoff)**.

Flow in `OperationService.Create/Update`:
1. If `TransactionCurrency` is null or `== wallet.BaseCurrency` → store `Amount` as-is, no FX call.
2. Else `rate = GetRate(TX, BASE, OccurredAtUtc.Date)` → `GET /v1/{yyyy-MM-dd}?base={TX}&symbols={BASE}`; weekend/holiday dates return the most recent prior business day's rate (deterministic, correct for back-dating).
3. `converted = Math.Round(originalAmount * rate, 2, MidpointRounding.ToEven)`.
4. Persist `Amount = converted` (always base currency → single-currency reports).
5. **Append original to note:** `Note = userNote + " [Original: {orig:0.##} {TX} @ {rate:0.######} on {date:yyyy-MM-dd} → {converted:0.00} {BASE}]"`.

**Integrity contract (Proposal 1 graft):** immutable historical rates cached in `IMemoryCache` keyed `(from,to,date)` indefinitely (today's rate ~1h). On hard failure after retries → `FxUnavailableException` → **HTTP 503**. **Never store an unconverted amount as converted.**

### 7.5 Input Validation (FluentValidation)
Registered via assembly scan + AutoValidation MVC filter → `400 ValidationProblemDetails` before the controller runs.
- `Amount > 0` (no negatives/zero), `<= 1_000_000_000`, ≤2 decimals.
- `OccurredAt` required, `<= now+1 day`, `>= 2000-01-01`.
- Period: `StartDate <= EndDate`, span `<= 366` days.
- Currency `^[A-Z]{3}$` ∈ ISO-4217 set; `Name` 1..100; `Description`/`Note` ≤500.
- `Username` 3..50 `^[a-zA-Z0-9_.-]+$`; `Password` ≥6; `Role` ∈ {Admin,User}; `Kind` a defined enum value.
- FK existence + ownership (`WalletId`, `OperationTypeId`) checked in the **service** layer.

---

## 8. DI Registration Convention (parallel-safe)

Each feature module exposes **one** static `IServiceCollection` extension in `Infrastructure/Modules/`: `AddAuthModule`, `AddWalletModule`, `AddOperationTypeModule`, `AddOperationModule`, `AddReportModule`, `AddCurrencyModule`. Scaffold adds the host registrars `AddPersistenceModule`, `AddJwtAuthModule`, `AddSwaggerModule`, `AddLoggingModule`, `AddValidationModule`.

`Program.cs` (owned by scaffold) calls every registrar; **feature agents never edit `Program.cs`** — each edits only its own `AddXModule` + feature files. Registration is **explicit (non-reflection)** so every line is readable and a missing module fails at the call site. Controllers auto-discovered by `AddControllers()`. Lifetimes: services/repositories Scoped; `ICurrentUser` Scoped; `FrankfurterClient` typed client; `IClock` singleton.

> **Build note:** the scaffold creates **stub** `AddXModule` files (`=> services;`) so the skeleton compiles; each feature agent **overwrites** its own module file with the real registrations.

---

## 9. Configuration

**appsettings keys:** `ConnectionStrings:Default`, `Jwt:{Issuer,Audience,Secret,ExpiryMinutes}`, `Fx:{BaseUrl,RetryCount}`, `Logging:MaxBodyBytes`, `Serilog:MinimumLevel`, `Seed:{AdminUsername,AdminPassword,DefaultWalletCurrency}`.

**Docker env:** `ConnectionStrings__Default=Host=db;Port=5432;Database=finance;Username=finance;Password=${POSTGRES_PASSWORD}`, `JWT__Secret=${JWT_SECRET}`, `Jwt__Issuer=PersonalFinance`, `Jwt__Audience=PersonalFinance`, `ASPNETCORE_ENVIRONMENT=Development`, `ASPNETCORE_HTTP_PORTS=8080`, plus `POSTGRES_{DB,USER,PASSWORD}`.

**Ports:** host `8080` → container `8080`; Swagger at `http://localhost:8080/swagger`. **Volume:** named `pgdata` → `/var/lib/postgresql/data`.

---

## 10. Docker

**Dockerfile** (multi-stage): `sdk:8.0` restore+`publish -c Release` → `aspnet:8.0` runtime, **non-root user**, `ENV ASPNETCORE_HTTP_PORTS=8080`, `EXPOSE 8080`, `ENTRYPOINT ["dotnet","task11.Web.dll"]`.

**docker-compose.yml** — two services:
- `db`: `postgres:16-alpine`, env `POSTGRES_{DB,USER,PASSWORD}`, volume `pgdata`, healthcheck `pg_isready -U finance -d finance` (interval 5s, retries 10).
- `app`: `build: .`, `ports: "8080:8080"`, env (connection string + `JWT__Secret`), `depends_on: db: condition: service_healthy`.
- `volumes: pgdata:` → persistence across `down`/`up`. `docker compose up -d` starts both; app self-migrates + seeds.

App-waits-for-DB: `depends_on service_healthy` + `Migrate()` Polly retry.

---

## 11. Build Plan

1. **scaffold (single):** solution + projects + csproj pins; `Program.cs` calling all registrars; appsettings; BaseEntity + all entities + enum; `AppDbContext` + configurations + soft-delete filter + both interceptors + `IClock`; JWT host; Swagger/Logging/Validation modules; all 3 middleware; repository base + abstractions; domain exceptions; `DbSeeder`; **stub** feature module files; Dockerfile/compose/.env.example/.dockerignore/README. Compiles green as a skeleton.
2. **features (parallel):** `AuthModule, WalletModule, OperationTypeModule, OperationModule, ReportModule, CurrencyModule` — each agent edits only its own files + overwrites its `AddXModule` stub.
3. **integration (single):** verify all registrars called, wire `CurrencyModule` into `OperationService`, build full solution, fix errors.
4. **migrations (single):** `dotnet ef migrations add InitialCreate`; verify schema (numeric(18,2), timestamptz, filtered indexes, soft-delete cols).
5. **docker (single):** `docker compose up -d`; app reachable at `http://localhost:8080/swagger`; volume persists.
6. **review (single):** dependency discipline, soft-delete cannot be bypassed, secrets never logged, wallet isolation, FX 503 contract.
7. **verify (single):** login admin/admin → wallet → types → back-dated EUR operation (assert conversion + note) → soft-delete (assert hidden from reports) → daily + period reports; run unit tests.

---

## 12. Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Npgsql 8 rejects non-UTC `DateTime` on `timestamptz` | Convert inbound dates to UTC at the boundary; README note. |
| Auto-migrate races with multiple replicas | Fine for single-container; note as scale-out limitation. |
| FX provider outage / unsupported currency | UAH pairs → PrivatBank, others → Frankfurter; cache + Polly retry + explicit **503**; never store unconverted as converted. |
| Changing `BaseCurrency` after data exists | Immutable once wallet has operations (service → 409). |
| Convention-only layering (controller could inject `AppDbContext`) | Abstractions folders + review; optional ArchUnitNET test. |
| Soft-delete leak via unique index / restore path | Filtered unique indexes + global filter + `SoftDeleteInterceptor`. |
| JWT key < 32 chars throws under HS256 | Guard on `JwtSettings.Secret` length; `.env.example` shows ≥32-char placeholder. |
