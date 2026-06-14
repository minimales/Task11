# Task11 Personal-Finance Web API — Standards Conformance Report

*.NET 9 / ASP.NET Core / EF Core 9 / PostgreSQL — audited against Microsoft Learn, eShop/eShopOnWeb, OWASP API Security Top 10, RFC 9457, and the official .NET resilience/Docker guidance.*

---

## 1. Executive Summary

Task11 is a well-engineered layered Web API with genuinely clean runtime separation: EF Core is confined behind repositories, controllers are thin, services are fully unit-testable without a database, and the surface-level mechanics (REST semantics, EF Core mapping, JWT validation, structured logging, multi-stage Docker) are idiomatic and correct. The recurring shortfalls are *architectural and production-hardening* rather than functional: the "ApplicationCore" project depends inward on the EF-bearing "Data" project (inverting the central Clean Architecture rule), error handling and validation bypass the modern `AddProblemDetails`/`IExceptionHandler` pipeline and ship two divergent error shapes, resilience rides on the deprecated `Microsoft.Extensions.Http.Polly` with no circuit breaker, and the test suite leans on the discouraged EF InMemory provider with zero end-to-end coverage. None of these break the running app; several are deliberate, defensible student-project scoping choices, but a handful are real weaknesses worth fixing.

**Overall conformance rating: 3 / 5 (Solid)** — rounded average of nine dimension scores (3.39).

---

## 2. Scorecard

| Dimension | Score | One-line verdict |
|---|:---:|---|
| REST API Design | 4 / 5 | Idiomatic resources, verbs, status codes, and RFC 7807 errors; missing versioning, pagination, and a single error shape. |
| Architecture | 3 / 5 | Clean separation in practice, but top-down N-layer mislabeled as "Core" — dependency direction is inverted. |
| EF Core | 4 / 5 | Strong, idiomatic EF 9 (precision, UTC, interceptors, query filters, migrations); deviations are deliberate scope choices. |
| Security | 3 / 5 | Solid fundamentals (full JWT validation, BOLA checks, sound PBKDF2) undercut by a committed dev secret, no HTTPS/rate-limiting, weak password policy. |
| Validation & Errors | 3 / 5 | Correct status codes and `problem+json`, but no modern ProblemDetails pipeline, deprecated auto-validation, two error schemas, empty-body 401s. |
| Observability | 3.5 / 5 | Excellent structured logging, scopes, redaction, OWASP events; no OpenTelemetry/W3C tracing or TraceId enrichment. |
| Resilience | 3 / 5 | Good IHttpClientFactory typed-client base on a **deprecated** Polly package — retry-only, no breaker, jitter, or 429 handling. |
| Docker | 4 / 5 | Strong multi-stage build (non-root, port 8080, named volume, db healthcheck); no app healthcheck, plain env secrets, no chiseled image. |
| Testing | 3 / 5 | Strong deterministic unit tests, but uses the discouraged EF InMemory provider and has zero integration/WebApplicationFactory coverage. |

---

## 3. Per-Dimension Findings

### REST API Design — 4/5
**Conforms:** All routes are noun-based, plural, and correctly nested (`/api/wallets/{walletId}/operations`) with no verbs in URIs; HTTP verbs carry correct semantics and POST always targets collections; status codes are right across the board (`201 + Location` via `CreatedAtAction`, `204` on delete, typed `ActionResult<T>`, `[ProducesResponseType]`, `[Produces("application/json")]`); a custom middleware emits RFC 7807/9457 `application/problem+json`; 401-vs-403 separation is correct via the global fallback policy.
**Key gaps:** No API versioning of any kind (no `Asp.Versioning.*`, no `/v1` segment) — the single biggest deviation. Collection GETs are unbounded (no pagination/limit/offset), and there is no filtering/sorting/projection. Two divergent 400 bodies coexist (SharpGrip auto-validation vs the custom `ErrorModel`). Framework Problem Details are not wired, so 401/403/405/unknown-route 404 return empty bodies.

### Architecture — 3/5
**Conforms:** EF Core is fully confined to repository implementations (zero `AppDbContext`/EF imports in services or controllers), controllers depend only on Core interfaces, DTOs never leak EF entities, the composition root is centralized in `Program.cs`, and business logic is genuinely unit-testable with hand-rolled fakes and no database — the standard's practical minimum bar is met well.
**Key gaps:** The central dependency rule is inverted — `task11.ApplicationCore.csproj` references `task11.Data`, and the entities (`BaseEntity`, `FinancialOperationEntity`, etc.) live in the EF-bearing Data project, making this a traditional Web→BLL→DAL chain mislabeled as "Core." Repositories are one-per-table rather than per-aggregate-root (no `IAggregateRoot` marker), entities are anemic property bags with all rules in transaction-script services, and the dependency rule is convention-only (no `NetArchTest`/`ArchUnitNET` guardrail). `docs/ARCHITECTURE.md` is stale (describes net8/EF8/xUnit/single-project vs the actual net9/EF9/MSTest/three-project build).

### EF Core — 4/5
**Conforms:** Explicit decimal precision (`HasPrecision(18,2)` → `numeric(18,2)`), correct UTC/`timestamptz` handling forced at the service boundary via an injectable `IClock`, one `IEntityTypeConfiguration<T>` per entity, global soft-delete query filters with `IgnoreQueryFilters()` bypass, interceptor-driven soft-delete and audit stamping, committed migrations applied via `Database.Migrate()` (never `EnsureCreated()`), consistent `AsNoTracking()` on reads, NRT-enabled with bounded string lengths, and no concurrent DbContext use.
**Key gaps:** All deliberate scope choices — a hand-rolled `DbContextFactory` (singleton) instead of `AddDbContext`/`IDbContextFactory<T>` with hard-coded provider selection in `OnConfiguring`; runtime `Migrate()` on startup rather than idempotent SQL scripts; no `EnableRetryOnFailure()` for managed Postgres; `AppDbContext` not sealed; redundant `HasColumnType("char(3)")` alongside `IsFixedLength().HasMaxLength(3)`; and a hard-coded credential fallback in the design-time factory.

### Security — 3/5
**Conforms:** JWT validation is complete and correct (issuer/audience/lifetime/signing-key all validated, pinned key, tight 30s skew, `MapInboundClaims=false`); secure-by-default global `RequireAuthenticatedUser` fallback with correct middleware order; explicit `[Authorize(Roles="Admin")]`; real object-level authorization (OWASP API1/BOLA) on every client-supplied ID via `EnsureCanAccessAsync` with unguessable GUIDs; sound password hashing (128-bit CSPRNG salt, PBKDF2-HMAC-SHA256, 100k iterations, constant-time compare); login timing-attack guard; secrets externalized to env vars with a fail-fast key-length check.
**Key gaps:** A placeholder JWT secret is committed in git-tracked `appsettings.Development.json` (highest priority) and no `UserSecretsId`/user-secrets workflow exists. No HTTPS enforcement (no `UseHttpsRedirection`/`UseHsts`/`RequireHttpsMetadata`; container exposes plaintext 8080). No rate limiting/lockout on `/api/auth/login`. Weak 6-char password policy with no breach check. No step-up/re-auth or MFA for credential/role changes. Default seeded `admin/admin`. Self-issued HS256 (vs OIDC/RS256) and direct PBKDF2 (vs Identity's `PasswordHasher<TUser>`) are acknowledged student-project choices.

### Validation & Errors — 3/5
**Conforms:** Centralized exception-to-status mapping (404/403/409/503/400/500) rather than blanket 500s; correct `application/problem+json` with body `status` matching the response line; `ErrorModel` carries RFC 9457 members plus a field-level `errors` dictionary; 5xx details masked while logged with full context; validation lives in dedicated `AbstractValidator<T>` classes (no DataAnnotations); all controllers `[ApiController]`-annotated.
**Key gaps:** No `AddProblemDetails()`, `UseExceptionHandler()`, `UseStatusCodePages()`, or `IExceptionHandler` — the modern prescribed approach is entirely absent, so 401/403 challenges return empty bodies. Uses the deprecated SharpGrip FluentValidation auto-validation pipeline instead of explicit `ValidateAsync` (CancellationToken not threaded). Two inconsistent error schemas. Response is hand-rolled `JsonSerializer.Serialize` with no content negotiation/fallback. No `traceId`. `exception.Message` is echoed into client `Detail` on all 4xx/503 (potential leak). `type` points at the third-party `httpstatuses.io`.

### Observability — 3.5/5
**Conforms:** Structured logging is done correctly everywhere — named PascalCase message templates with no interpolation, exceptions passed as the dedicated parameter, semantically correct levels with a proper 4xx=Warning/5xx=Error split, per-category overrides, JSON console sink with `Enrich.FromLogContext` and flush-on-shutdown, CorrelationId/UserId log scopes, recursive secret redaction via `LogSanitizer`, OWASP security events logged, log-injection neutralized by JSON re-serialization, and a body-size guard.
**Key gaps:** No OpenTelemetry/distributed tracing at all (no `ActivitySource`, OTLP exporter, or W3C trace-context propagation across the two outbound FX clients). Logs are not enriched with TraceId/SpanId. No source-generated `[LoggerMessage]` on the hot request/response path and no stable `EventId`s. Missing service-name/version/client-IP fields. Full request/response bodies logged at Information level (PII exposure beyond the deny-list), and expensive body read/sanitize work runs without an `IsEnabled` guard.

### Resilience — 3/5
**Conforms:** All outbound HTTP goes through `IHttpClientFactory` typed clients (the only `new HttpClient()` is in test fakes); `BaseAddress`/timeout/headers configured at registration; exactly one resilience handler per client (no stacking); exponential backoff (`2^attempt`); short-lived clients not pinned in singletons; transient transport failures translated to a domain `FxUnavailableException` → HTTP 503 with `IMemoryCache` shielding the dependency; default handler lifetime left intact for DNS rotation.
**Key gaps:** Built on the **deprecated** `Microsoft.Extensions.Http.Polly` (`AddPolicyHandler`/`WaitAndRetryAsync`) — must migrate to `Microsoft.Extensions.Http.Resilience` / `AddStandardResilienceHandler`. The pipeline is retry-only: no circuit breaker (a hard outage burns the full retry budget hammering a dead dependency), no jitter (thundering-herd risk), no 429 handling, and no total-vs-attempt timeout budget (worst case ~40s+ uncapped). No unsafe-method retry guard, no resilience telemetry/enricher, and no User-Agent header (PrivatBank's p24api can reject without one).

### Docker — 4/5
**Conforms:** Correct multi-stage build (SDK builds, `aspnet:9.0` runtime ships) with fully-qualified MCR images matching the net9 TFM and pinned SDK; optimized layer caching (csproj/global.json copied before restore); runs as non-root `USER app`; binds the unprivileged 8080 via `ASPNETCORE_HTTP_PORTS`; exec-form ENTRYPOINT; thorough `.dockerignore`; db healthcheck with `depends_on: service_healthy` ordering; named `pgdata` volume; secrets injected at runtime from a git-ignored `.env`.
**Key gaps:** No healthcheck on the `app` service despite an existing `/health` endpoint. Secrets passed as plain `environment:` vars (visible in `docker inspect`) rather than compose `secrets:`. No chiseled/distroless runtime image (misses ~100MB and attack-surface reduction). No SHA digest pinning. `publish` lacks `--no-restore` (redundant restore). `ASPNETCORE_ENVIRONMENT=Development` in compose. `COPY --from=build` placed after the `USER` switch.

### Testing — 3/5
**Conforms:** Deterministic `FixedClock`/`IClock` seam everywhere (no static `DateTime.Now`); descriptive behavior-documenting test names; clean single-Act AAA bodies with no control flow; data-driven `[DataRow]` parameterization instead of loops; querying abstracted behind repository interfaces returning materialized collections (never `IQueryable`), stubbed with hand-rolled fakes; HTTP faked deterministically via `StubHttpMessageHandler` with good FX edge-case coverage; thin controller tests with factory helpers; constants hoisted; private methods tested through public callers.
**Key gaps:** No unit/integration separation — all three `*.UnitTesting` projects reference `EFCore.InMemory`. Heavy use of the EF team's explicitly discouraged InMemory provider, including for `ReportRepository.GetTotalsAsync` GroupBy/Sum (which Postgres evaluates differently). DB-touching tests mislabeled as unit tests. Zero integration layer — no `Mvc.Testing`/`WebApplicationFactory<Program>` (despite `Program` being made partial for exactly this), so auth, the middleware pipeline, validation→400, and FX→503 are never tested end-to-end. Test-double naming mixes stub/mock semantics.

---

## 4. Prioritized Recommendations

### High Priority
- **Wire the modern Problem Details pipeline** — add `AddProblemDetails()`, `UseExceptionHandler()`, `UseStatusCodePages()`, and configure SharpGrip's result factory (or `InvalidModelStateResponseFactory`) to emit the same shape. One change fixes the two divergent error schemas *and* the empty-body 401/403/404/405 responses *and* adds `traceId`. *(REST, Validation)*
- **Migrate off deprecated `Microsoft.Extensions.Http.Polly`** to `Microsoft.Extensions.Http.Resilience` with one `AddStandardResilienceHandler` per client. This single swap closes the deprecated-package, missing-circuit-breaker, missing-jitter, 429-handling, and total-vs-attempt-timeout gaps at once. *(Resilience)*
- **Stop committing the JWT secret** — `git rm --cached appsettings.Development.json`, add a `<UserSecretsId>`, and move the dev secret to `dotnet user-secrets`. *(Security)*
- **Introduce API versioning from the first release** — add `Asp.Versioning.Mvc(.ApiExplorer)`, annotate `[ApiVersion("1.0")]`, and move routes to `api/v{version:apiVersion}/...`. *(REST)*
- **Add a real integration test project** (`Sdk=Microsoft.NET.Sdk.Web` + `Mvc.Testing` + `WebApplicationFactory<Program>`, already prepared) covering login→JWT→create→soft-delete→report, 401, 400 ProblemDetails, and FX→503. *(Testing)*
- **Resolve the architecture label** — either rename `ApplicationCore`→`Application` / `Data`→`Infrastructure` to stop implying Clean Architecture, or do the real inversion (move entities + interfaces to a framework-free Core and flip the reference). Add a `NetArchTest`/`ArchUnitNET` guardrail asserting Web/services never depend on `AppDbContext`. *(Architecture)*

### Medium Priority
- **Add pagination** (`limit`/`offset`, default 25, enforced server-side max, paging pushed to the repository) plus **filtering/sorting** on the operations collection. *(REST)*
- **Stop using the EF InMemory provider** — run data/report tests against real Postgres via Testcontainers/Respawn (or SQLite-in-memory), and physically split DB tests out of the `*.UnitTesting` projects so they carry no infra packages. *(Testing)*
- **Adopt OpenTelemetry** (`AddOpenTelemetry().WithTracing(...)` with ASP.NET Core + HttpClient instrumentation and OTLP export) for W3C trace propagation across the FX clients, and enrich logs with TraceId/SpanId. *(Observability)*
- **Add a compose healthcheck on the `app` service** probing the existing `/health` endpoint. *(Docker)*
- **Add rate limiting + lockout** on `/api/auth/login` and **fail startup on a default/unset seed admin password**. *(Security)*
- **Stop echoing `exception.Message` into client `Detail` for 4xx/503**; map each domain exception to a curated public message and log the raw message server-side only. *(Validation)*
- **Collapse table-level repositories toward aggregate roots** (treat Wallet as the root for OperationType/Operation) and push invariants (base-currency immutability, ownership, FX integrity) onto the entities. *(Architecture)*

### Low Priority
- Declare `[Consumes("application/json")]` on POST/PUT and set `ReturnHttpNotAcceptable=true` for proper 406/415. *(REST)*
- Convert hot-path request/response and auth/exception logs to source-generated `[LoggerMessage]` with stable `EventId`s, and guard expensive body work with `IsEnabled`. Enrich with service name/version and client IP. *(Observability)*
- Switch the runtime image to `aspnet:9.0-noble-chiseled`, add `--no-restore` to publish, move secrets to compose `secrets:`, pin base images by SHA, and set `ASPNETCORE_ENVIRONMENT=Production`. *(Docker)*
- Add `EnableRetryOnFailure()`, seal `AppDbContext`, and prefer idempotent SQL migration scripts over runtime `Migrate()` if production is ever in scope. *(EF Core)*
- Set an explicit User-Agent on both FX clients and add `DisableForUnsafeHttpMethods()` to encode the GET-only invariant. *(Resilience)*
- Strengthen the password policy (≥8–12 chars + breached-password check) and document the HS256/OIDC trade-off in `ARCHITECTURE.md`. *(Security)*
- Change the ProblemDetails `type` from `httpstatuses.io` to `about:blank` or an app-owned URI; fix the test-double stub/mock naming. *(Validation, Testing)*

---

## 5. Where We Deliberately Diverge From Official Samples — and Why

These divergences are intentional, mostly to match sibling student projects and keep scope contained. Framed honestly:

| Divergence | Standard says | Verdict |
|---|---|---|
| **Hand-rolled `DbContextFactory` (singleton) vs `AddDbContext`/`IDbContextFactory<T>`** | Inject `DbContextOptions<T>`; don't hard-code the connection string in `OnConfiguring`. | **Acceptable.** Functionally a context-per-unit-of-work and thread-safe; a conscious convention choice. Only revisit if framework-idiom alignment matters or multi-step transactions are needed. |
| **Repositories over EF directly** | Repositories are optional-but-valid for decoupling/testing. | **Acceptable and well-executed** — applied consistently and the reason the services are fully unit-testable. The *one-per-table* shape (vs per-aggregate-root) is the weaker part, not the repository choice itself. |
| **Hand-rolled PBKDF2 vs Identity's `PasswordHasher<TUser>`** | MS Learn steers new apps to `PasswordHasher` (versioned hashes + rehash-on-verify), not direct KDF. | **Borderline.** Parameters are sound (100k iters, 128-bit salt, constant-time compare), so it's safe — but it's the primitive the guidance explicitly steers away from. Acceptable for a student project; would be a finding in production. |
| **MSTest with hand-rolled fakes vs Moq/xUnit** | No mandate on the framework; doubles are fine. | **Acceptable** — the fakes are clean, behavior-focused, and keep EF out of service tests. The genuine weakness is adjacent (InMemory provider + no integration tests), not the choice of MSTest/fakes. |
| **Dual FX provider (Frankfurter + PrivatBank)** | Use IHttpClientFactory typed clients with resilience. | **Acceptable design** — each provider is its own typed client with caching and graceful 503 degradation. The real issue is the *deprecated resilience package and retry-only pipeline*, not having two providers. |

**Net assessment:** the DbContextFactory, repository, MSTest-fakes, and dual-FX choices are reasonable, defensible scoping decisions. The **genuine weaknesses** that are not excused by "student project" framing are: the committed JWT secret, the deprecated resilience package with no circuit breaker, the missing modern ProblemDetails pipeline with two error shapes and empty-body 401s, the EF InMemory reliance with zero integration coverage, and the inverted/mislabeled "Core" dependency direction. These are the items worth prioritizing regardless of scope.
