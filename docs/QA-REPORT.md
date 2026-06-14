# QA + Code-Review Report — task11 .NET 9 Personal-Finance Web API

## 1. Executive Summary

The task11 service is a structurally sound, conventionally-organized Clean-Architecture .NET 9 Web API (controllers → services → repositories, EF Core 9 + Npgsql, JWT auth, FX integration with Polly resilience). The core happy paths work, malformed-input handling is mostly correct (truncated JSON, wrong-type values, and most boundary checks return clean 400s), and authorization/ownership (BOLA) checks are present and consistent. However, adversarial QA and code review surfaced a cluster of **real, reproducible defects** — several of which are reachable by a normal authenticated user (and one unauthenticated) and which break two pillars of a finance app: **availability** (unhandled 500s) and **financial correctness** (silently wrong report totals).

**Confirmed findings: 23 of 25 raised** (2 raised findings were filtered out as false positives during adversarial verification).

| Severity | Count |
|----------|-------|
| Critical | 0 |
| High | 8 |
| Medium | 3 |
| Low | 12 |
| **Total confirmed** | **23** |

The defects cluster into three root causes, each of which fixes multiple findings at once:
1. **A log-sanitizer crash** on duplicate JSON keys → unhandled 500 on *every* body endpoint, including unauthenticated `/api/auth/login`.
2. **Soft-delete of an in-use OperationType has no guard**, and the global soft-delete query filter on a *required* navigation then either throws an NRE (reports → 500), silently drops operations from totals (financial under-statement), or yields an out-of-range `Kind=0`.
3. **Committed secrets / default credentials** (dev JWT signing key, `admin/admin`, DB password).

**Production-readiness call:** **Not production-ready as-is.** The high-severity availability and financial-correctness bugs, plus committed default-admin credentials, must be fixed first. As a graded assignment it is a solid, near-complete submission that demonstrates good architecture, but it would not pass a rigorous review until at least the High items are addressed. None of the findings are critical (no unconditional auth bypass, RCE, or destructive data loss), and the fixes are well-scoped and low-risk.

---

## 2. Findings Table (severity-sorted)

| Severity | Area | Title | QA/Review |
|----------|------|-------|-----------|
| High | Availability / Logging | Duplicate JSON key → unhandled 500 on every body endpoint (incl. unauthenticated login) | QA (fuzz) |
| High | Availability / Protocol | Duplicate JSON key → 500 (log-sanitizer crash before controller runs) | QA (protocol) |
| High | Correctness / Reports | Soft-deleting an OperationType makes period/daily reports throw NRE (500) | Review |
| High | Data / Reports | Report totals silently exclude operations whose type was soft-deleted | Review |
| High | Data / Reports | Reports silently drop soft-deleted-type operations (income/expense understated) | Review |
| High | Data Integrity | Deleting an in-use OperationType is not blocked; soft-delete bypasses Restrict FK | Review |
| High | Security / Secrets | Committed, predictable JWT signing secret enables Admin-token forgery (Dev) | Review |
| High | Security / Auth | Default admin credentials `admin/admin` committed and auto-seeded | Review |
| Medium | Concurrency / Data | Create-operation read-then-insert not transactional; no guard vs concurrently soft-deleted type/wallet | Review |
| Medium | Concurrency / Data | Duplicate username / type-name race → 500 instead of 409 | Review |
| Medium | Resilience | `HttpClient.Timeout` (10s) caps whole resilience pipeline, nullifies retries on slow provider | Review |
| Low | Concurrency | Username uniqueness check-then-act race → 500 not 409 | Review |
| Low | Concurrency | Operation-type (WalletId,Name) uniqueness race → 500 not 409 | Review |
| Low | Info Disclosure | Internal .NET/CLR type names leaked in 400 validation messages | QA (fuzz) |
| Low | Boundary | FX-converted amount bypasses the 1,000,000,000 cap | QA (boundary) |
| Low | Protocol | 415 served as `application/json` instead of `application/problem+json` on `[Produces]` controllers | QA (protocol) |
| Low | Correctness | Operation listing maps soft-deleted-type ops to invalid `Kind=0` | Review |
| Low | Security / Secrets | Hardcoded fallback DB credentials in `Program.cs` + committed connection string | Review |
| Low | Data | `HasOperationsAsync` ignores soft-delete filter → wallet currency locked forever | Review |
| Low | Data / Concurrency | Soft-delete via detached `AsNoTracking` entity issues full-row UPDATE → lost update | Review |
| Low | Resilience | DB seeding runs outside the startup migration retry pipeline | Review |
| Low | Observability | Response-logging middleware downstream of `UseExceptionHandler` → error responses unlogged | Review |
| Low | Performance | FX rate cache has no stampede protection (thundering herd on FX providers) | Review |

---

## 3. Detailed Findings

### HIGH

---

#### H-1. Duplicate JSON key → unhandled 500 on every body endpoint (incl. unauthenticated `/api/auth/login`)
**Type:** QA (fuzz) + QA (protocol) — *two independent confirmations of the same root cause.*
**Location:** `task11.Web/Infrastructure/Logging/LogSanitizer.cs:37,46,57`; thrown via `task11.Web/Middleware/RequestResponseLoggingMiddleware.cs:46` (`InvokeAsync`).

**Repro (unauthenticated):**
```bash
curl -s -i -X POST http://localhost:8080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"a","username":"b","password":"x"}'
```
Also reproduces on `/api/users`, `/api/wallets`, `/api/wallets/{id}/operation-types`, `/api/operations` (POST and PUT), and on duplicate keys in **nested** objects.

**Expected:** A duplicate/ambiguous JSON property should be rejected with `400 application/problem+json`, or processed normally (the model binder's `Utf8JsonReader` accepts duplicates, last value wins).

**Actual:** `500 Internal Server Error` (generic ProblemDetails body; no stack leaked to client). Container log shows:
```
System.ArgumentException: An item with the same key has already been added. Key: username
  at System.Text.Json.Nodes.JsonObject.InitializeDictionary()
  at task11.Web.Infrastructure.Logging.LogSanitizer.Redact(...) LogSanitizer.cs:line 57
  at LogSanitizer.Sanitize line 43
  at RequestResponseLoggingMiddleware.InvokeAsync
```

**Root cause (verified in source):** `RequestResponseLoggingMiddleware.InvokeAsync` calls `LogSanitizer.Sanitize` on the raw request body **before** `_next(context)`. `Sanitize` (line 37) calls `JsonNode.Parse`, which parses **lazily** and does *not* throw on duplicate keys. `Redact` (line 57) then calls `obj.ToList()`, which materializes the `JsonObject`'s backing `OrderedDictionary` and throws `ArgumentException` on the duplicate key. The `catch` block (line 46) only catches `JsonException`, so the `ArgumentException` escapes the middleware and `GlobalExceptionHandler` maps it to 500.

**Smoking gun:** The *identical* duplicate-key body padded past the 32 KB `Logging:MaxBodyBytes` threshold returns `201 Created` (last value wins) — because the sanitizer skips parsing oversized bodies and the request reaches the binder successfully. This proves the request is fully processable; only the log sanitizer faults it.

**Impact:** 100%-reproducible, trivially-triggerable fault on **every** body-accepting endpoint, reachable **unauthenticated** via the login endpoint. Pollutes logs with full stack traces. App/health stays up (`/health=200`), no data corruption, no auth bypass, and no stack leaks to the client — hence high, not critical.

**Fix:** Make the logging path fault-tolerant. In `LogSanitizer.Sanitize`, catch `Exception` (not just `JsonException`) around `Parse`+`Redact` and fall back to the `[omitted: N bytes]` placeholder, **or** parse with `JsonDocument`/`Utf8JsonReader` which tolerate duplicate keys. The logging path must never be able to fault a request:
```csharp
catch (Exception)   // was: catch (JsonException)
{
    return $"[omitted: {byteCount} bytes]";
}
```

---

#### H-2. Soft-deleting an OperationType makes period/daily reports throw NullReferenceException (500)
**Type:** Review (correctness).
**Location:** `task11.ApplicationCore/Services/ReportService.cs:102` (`operation.OperationType!.Kind`); enabling load at `task11.Infrastructure/Repositories/ReportRepository.cs:53-60` (`.Include(o => o.OperationType)`); enabling cause `task11.ApplicationCore/Services/OperationTypeService.cs:90-95` (`DeleteAsync` has no in-use guard).

**Repro:**
```bash
TID=$(curl -s -X POST http://localhost:8080/api/wallets/$WID/operation-types \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Salary","kind":1}' | jq -r .id)
curl -s -X POST http://localhost:8080/api/operations -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"walletId\":\"$WID\",\"typeId\":\"$TID\",\"amount\":100,\"date\":\"2026-06-14\"}"
curl -s -X DELETE http://localhost:8080/api/operation-types/$TID -H "Authorization: Bearer $TOKEN"
curl -i "http://localhost:8080/api/reports/daily?walletId=$WID&date=2026-06-14" -H "Authorization: Bearer $TOKEN"
```

**Expected:** A report over a wallet whose operations reference a soft-deleted type returns 200 (Kind taken from the deleted type, or the row handled gracefully).

**Actual:** `500`. `OperationTypeId` is a non-nullable FK (migration `20260613125605_InitialCreate.cs:86`, `nullable:false`), so the nav is **required**. The global `IsDeleted == false` query filter (`ModelBuilderExtensions.cs:26`, wired at `AppDbContext.cs:50`) excludes the soft-deleted principal; on Npgsql the required-nav `.Include()` becomes a LEFT JOIN with the type filter, so `operation.OperationType` materializes as `null` while the dependent operation row is still returned. `MapToLine` then dereferences `operation.OperationType!.Kind` and throws NRE → 500.

**Tell:** `ReportService.cs:101` defensively uses `OperationType?.Name ?? string.Empty` — the nav is *known* to be nullable — but line 102 is unguarded.

**Impact:** An authenticated owner can permanently 500 their own wallet's daily/period reports via fully-supported calls (create type → create operation → delete type), with no API recovery path. High.

**Fix:** Two complementary changes (see also H-4/H-5):
1. Block deleting an in-use type — add `IOperationTypeRepository.HasOperationsAsync(typeId)` (mirroring `WalletRepository.HasOperationsAsync`) and throw `ConflictException` (409) in `OperationTypeService.DeleteAsync`.
2. Harden the read path — in `ReportService.MapToLine`, use `Kind = operation.OperationType?.Kind ?? <sentinel>` and/or `IgnoreQueryFilters()` on the type join so the deleted type's `Kind` still resolves.

---

#### H-3. Report totals silently exclude operations whose type was soft-deleted
**Type:** Review (correctness / data).
**Location:** `task11.Infrastructure/Repositories/ReportRepository.cs:30` (`GroupBy(o => o.OperationType.Kind)` in `GetTotalsAsync`); same enabling cause as H-2.

**Repro:** Reuse H-2's setup, add a second still-active type with an Expense op, then:
```bash
curl -s "http://localhost:8080/api/reports/period?walletId=$WID&startDate=2026-06-01&endDate=2026-06-30" \
  -H "Authorization: Bearer $TOKEN" | jq '{TotalIncome,TotalExpense,NetResult}'
```

**Expected:** `TotalIncome`/`TotalExpense`/`NetResult` include every non-deleted operation in the range, regardless of whether its type was later deleted.

**Actual:** `GroupBy(o => o.OperationType.Kind)` traverses the required nav; with the type filter active EF inner-joins and drops operations whose type is soft-deleted. Their amounts vanish from the totals; `NetResult` is consequently wrong — **with no error surfaced**. The underlying operation rows still physically exist.

**Refinement (verified):** `GetOperationsAsync` uses `.Include` on the same required nav, so on Npgsql it inner-joins and drops the *same* rows — totals and the listed lines stay mutually consistent (both omit the affected op). This makes the bug *worse*: there is no detectable inconsistency to alert anyone.

**Impact:** Silent financial mis-statement on the primary reporting feature, triggered by a normal user action. High.

**Fix:** Same as H-2 — guard the delete, and/or make the report queries filter-tolerant (`IgnoreQueryFilters()` on the type join, or denormalize `Kind` onto the operation row), applied consistently in **both** `GetTotalsAsync` and `GetOperationsAsync`.

---

#### H-4. Reports silently drop soft-deleted-type operations (income/expense understated)
**Type:** Review (data).
**Location:** `task11.Infrastructure/Repositories/ReportRepository.cs:30` and `:57`; `task11.Infrastructure/Persistence/ModelBuilderExtensions.cs:26`; `task11.ApplicationCore/Services/OperationTypeService.cs:90`.

This is the data-integrity framing of H-3, empirically confirmed against the project's own InMemory `AppDbContext`: after the documented create→delete flow, `RAW_OPS=1` (the operation row physically survives) but `REPORT_INCOME=0` (the `GroupBy` over `o.OperationType.Kind` drops it). On the relational Npgsql provider the required-nav `.Include()` is an INNER JOIN with `operation_types.IsDeleted=false`, dropping the root operation from both totals and the line list.

**Correction to the original repro comment:** it claimed the operation "still exists in the wallet operations endpoint." On Postgres that endpoint (`OperationRepository.ListByWalletAsync:41`) uses the same `.Include(o => o.OperationType)`, so the op vanishes there too — making the data issue *worse*, not a false positive.

**Repro (full sequence):**
```bash
TOKEN=$(curl -s -X POST localhost:8080/api/auth/login -H 'Content-Type: application/json' -d '{"username":"admin","password":"admin"}' | jq -r .accessToken)
WID=$(curl -s -X POST localhost:8080/api/wallets -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d '{"name":"W","baseCurrency":"UAH"}' | jq -r .id)
TID=$(curl -s -X POST localhost:8080/api/wallets/$WID/operation-types -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d '{"name":"Salary","kind":0}' | jq -r .id)
curl -s -X POST localhost:8080/api/operations -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d "{\"walletId\":\"$WID\",\"typeId\":\"$TID\",\"amount\":100,\"date\":\"2026-06-14T00:00:00Z\"}"
curl -s -X DELETE localhost:8080/api/operation-types/$TID -H "Authorization: Bearer $TOKEN"
curl -s -X POST localhost:8080/api/reports/period -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d "{\"walletId\":\"$WID\",\"startDate\":\"2026-06-14\",\"endDate\":\"2026-06-14\"}"   # totalIncome=0 instead of 100
```

**Fix:** Same remediation as H-2/H-3 (guard the delete is the cleanest single fix that resolves H-2 through H-6 and L-Kind together).

---

#### H-5. Deleting an in-use OperationType is not blocked; soft-delete bypasses the Restrict FK
**Type:** Review (data integrity).
**Location:** `task11.ApplicationCore/Services/OperationTypeService.cs:90-95`; `task11.Infrastructure/Repositories/OperationTypeRepository.cs:73-80`; `task11.Infrastructure/Persistence/Interceptors/SoftDeleteInterceptor.cs:45-56`.

**Repro:**
```bash
# Succeeds with 204 even though an operation references this type:
curl -s -o /dev/null -w '%{http_code}\n' -X DELETE localhost:8080/api/operation-types/$TID -H "Authorization: Bearer $TOKEN"
```

**Expected:** An operation type with financial operations cannot be removed — consistent with the `OnDelete(DeleteBehavior.Restrict)` FK (`OperationTypeEntityConfiguration.cs:30-33`) and the analogous wallet `HasOperationsAsync` guard.

**Actual:** `DeleteAsync` soft-deletes with **no** dependency check. The DB FK is `Restrict`, but `SoftDeleteInterceptor` rewrites the `Deleted` state to `Modified` (an UPDATE setting `IsDeleted=true`), so the row physically stays and the FK **never fires**. Result: live operations pointing at a soft-deleted type → the cascade of H-2/H-3/H-4 plus an inconsistent state where `GetByIdAsync` still returns the type but listings don't. The codebase already knows the correct pattern (`WalletRepository.HasOperationsAsync` + `WalletService.cs:71`); the omission for types is a clear asymmetry, not handled elsewhere.

**Impact:** Silent report corruption + orphaned-pointer inconsistency, contradicting the declared `Restrict` invariant. High (recoverable by restoring `IsDeleted`, so not critical).

**Fix:** Add `OperationTypeRepository.HasOperationsAsync(typeId)` using `IgnoreQueryFilters().AnyAsync(o => o.OperationTypeId == typeId && !o.IsDeleted, ...)` and throw `ConflictException` (409) in `DeleteAsync` before soft-delete; **or** cascade-soft-delete the dependent operations.

---

#### H-6. Committed, predictable JWT signing secret enables Admin-token forgery (Development)
**Type:** Review (security).
**Location:** `task11.Web/appsettings.Development.json:9` (consumed at `task11.Web/Program.cs:56-71`).

**Repro:** Offline-forge an HS256 token with key `dev-only-insecure-secret-change-me-32+chars` and claims `{"sub":"<guid>","role":"Admin","iss":"PersonalFinance","aud":"PersonalFinance"}`, then:
```bash
curl -H "Authorization: Bearer <forged>" http://localhost:5000/api/users
```

**Expected:** The HS256 key is a server-only secret supplied at runtime via env/user-secrets, never committed.

**Actual:** `Jwt:Secret = "dev-only-insecure-secret-change-me-32+chars"` is hardcoded and git-tracked, with `Issuer`/`Audience = "PersonalFinance"` in the same file. The Web csproj has no `UserSecretsId`, so nothing overrides it by default. `Program.cs:59` validates **only length ≥ 32** — no secrecy/entropy/known-default check. `CurrentUser.cs:25` derives `IsAdmin` solely from the token `role` claim, and the access checks (`ReportService.cs:68-70`, `WalletService.cs:108-110`) short-circuit to allow when `IsAdmin`. So anyone with repo access who can reach a `ASPNETCORE_ENVIRONMENT=Development` instance can mint a valid Admin token and bypass auth + all per-wallet BOLA checks.

**Impact:** Full auth bypass when reachable; scoped to Development config (Production supplies a different secret via env, which bounds exposure) → high, not critical.

**Fix:** Remove the secret from committed config; load from user-secrets/Key Vault/env only; fail startup if a known-default secret value is detected; reject well-known default strings in the `Program.cs` guard.

---

#### H-7. Default admin credentials `admin/admin` committed and auto-seeded
**Type:** Review (security / auth).
**Location:** `task11.Web/appsettings.json:28-30` (and `appsettings.Development.json:27-30`); `task11.Web/Program.cs:343-344` (`SeedAsync`).

**Repro:**
```bash
curl -s -X POST http://localhost:5000/api/auth/login -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"admin"}'   # returns a valid Admin accessToken on a freshly-seeded DB
```

**Expected:** No default/guessable admin password ships; the seed password is supplied securely at deploy time or rotated on first use.

**Actual:** `Seed:AdminUsername="admin"` / `Seed:AdminPassword="admin"` are committed, and `Program.cs:343-344` also defaults to `admin/admin` when unset. `SeedAsync` runs on every startup when the Users table is empty (`Program.cs:336-367`), creating an `Admin` user with password `admin`. `PasswordHasher.Hash` only does `ThrowIfNullOrEmpty` — the 6-char minimum-length rule lives in the API-side validator, **not** the seed path — so the 5-char `admin` is hashed fine. `AuthService.LoginAsync` does a plain `Verify` and returns a valid Admin token. No fail-fast, env override, or first-use rotation changes this.

**Impact:** Default-admin-credentials exposure granting full access to all users and wallets, gated only on the DB being empty at first startup. High.

**Fix:** Require `Seed:AdminPassword` to be explicitly provided (fail startup otherwise); drop the committed default; never commit a real default password; consider forcing rotation on first login.

---

### MEDIUM

---

#### M-1. Create-operation read-then-insert is not transactional; no guard vs concurrently soft-deleted type/wallet
**Type:** Review (concurrency / data).
**Location:** `task11.ApplicationCore/Services/OperationService.cs:55-80`; `task11.Infrastructure/Repositories/OperationRepository.cs:48-66`.

**Expected:** An operation cannot be created against a wallet/type that no longer effectively exists; the read-validate-insert sequence is consistent.

**Actual:** Each repository call opens its own `AppDbContext` via the singleton `DbContextFactory`, so `CreateAsync` reads the wallet (context 1), reads the type via `GetTypeForWalletAsync` (context 2), then inserts via `AddAsync` (context 3) — three separate transactions. If the type/wallet is soft-deleted between read and insert, the insert still succeeds: the FK is `Restrict` and the parent row physically remains after soft-delete, so the DB cannot reject it. There is no `RowVersion`/`xmin`/concurrency token (grep confirms none). Net: a live operation referencing a deleted parent, feeding the report bugs above. The same orphaned state is also reachable non-racily via H-5.

**Impact:** Data-consistency (dangling reference), narrow race window, no auth/money/data-loss impact → medium.

**Fix:** Perform read-validate-insert inside a single `DbContext`/transaction (unit-of-work) so existence is re-validated atomically, or add a DB-level rule rejecting inserts that reference a soft-deleted parent.

---

#### M-2. Duplicate username / operation-type name race → 500 instead of 409
**Type:** Review (data / concurrency).
**Location:** `UserService.cs:41-53`; `OperationTypeService.cs:47-61`; `UserRepository.cs:44-61`; `OperationTypeRepository.cs:37-62`; unmapped in `GlobalExceptionHandler.cs:31-39`.

**Repro:**
```bash
for i in 1 2; do curl -s -o /dev/null -w '%{http_code}\n' -X POST localhost:8080/api/users \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"username":"raceuser","password":"pw12345","role":"User"}' & done; wait
```

**Expected:** Exactly one 201 and one 409 Conflict.

**Actual:** The existence check (`UsernameExistsAsync`/`NameExistsAsync`) runs in a different `DbContext` than `AddAsync`, so check-then-insert is non-atomic. The filtered unique indexes (`IX_users_Username`, `IX_operation_types_WalletId_Name`, both filtered on `IsDeleted=false`) correctly reject the duplicate row, but the resulting `DbUpdateException` (Npgsql `23505`) is **not** translated to `ConflictException`. `GlobalExceptionHandler`'s switch has no `DbUpdateException`/`PostgresException` case, so it falls to the default `_ =>` arm → 500. Data integrity is preserved; only the status code (and a noisy error log) is wrong.

**Impact:** Wrong status under a real but narrow concurrency window → medium.

**Fix:** Catch `DbUpdateException` whose inner is a unique-violation (`PostgresException.SqlState == "23505"`) in the `AddAsync`/`UpdateAsync` paths and rethrow as `ConflictException`. (This single fix also resolves L-1 and L-2.)

---

#### M-3. `HttpClient.Timeout` (10s) caps the whole resilience pipeline and nullifies retries
**Type:** Review (resilience). *Severity adjusted high→medium during verification.*
**Location:** `task11.Web/Program.cs:135,139-142` (FrankfurterClient) and `:152,156-159` (PrivatBankClient).

**Repro:** Point `Fx:BaseUrl` at a sink that delays ~11s, then POST `/api/operations` with a `transactionCurrency` requiring conversion.

**Expected:** The standard resilience handler owns timeouts: `AttemptTimeout` per try, `TotalRequestTimeout` across retries (defaults 10s / 30s). `HttpClient.Timeout` should be `Timeout.InfiniteTimeSpan` (or ≥ total) so it doesn't fight the pipeline.

**Actual:** `client.Timeout = TimeSpan.FromSeconds(10)` is set on both typed clients, and both register `.AddStandardResilienceHandler()` configuring **only** `Retry.MaxRetryAttempts` (defaults 3). With HttpClientFactory the resilience `DelegatingHandler` sits *inside* the typed client, so `HttpClient.Timeout`'s CTS wraps the **entire** pipeline (all retries + inter-attempt delays). The 10s client timeout fires at ~the same moment as the first attempt's 10s `AttemptTimeout`, so configured retries get little-to-no budget on a slow provider. `FrankfurterClient.cs:62-67` catches `TaskCanceledException` → `FxUnavailableException` → 503. Note the "nullifies retries entirely" framing is slightly overstated: fast-failing responses (quick 5xx / connection-refused) still retry within the 10s window; the bug specifically bites the *slow-provider* case.

**Impact:** Resilience degradation that fails safe (503), no correctness/security impact → medium.

**Fix:** `client.Timeout = Timeout.InfiniteTimeSpan;` and configure `resilience.AttemptTimeout` / `TotalRequestTimeout` explicitly (keep `SamplingDuration >= 2*AttemptTimeout` to satisfy the handler's validation).

---

### LOW

---

#### L-1. Username uniqueness check-then-act race → 500 not 409
**Type:** Review (concurrency). *Adjusted high→low.*
**Location:** `UserService.cs:41-53`; unmapped in `GlobalExceptionHandler.cs:31-39`. Same defect in `UserService.UpdateAsync:65-78`.

Concrete instance of M-2 for users specifically. The losing concurrent create returns 500 instead of the documented `[ProducesResponseType(409)]` (`AuthController.cs:59`). Low because it is admin-only (`[Authorize(Roles="Admin")]`), the unique index preserves integrity, and the sequential path correctly returns 409. **Fix:** same unique-violation→`ConflictException` mapping as M-2.

---

#### L-2. Operation-type (WalletId,Name) uniqueness race → 500 not 409
**Type:** Review (concurrency). *Adjusted high→low.*
**Location:** `OperationTypeService.cs:47-61` (and `UpdateAsync:75-85`); unmapped in `GlobalExceptionHandler.cs:31-39`.

Same root cause and remediation as M-2/L-1, for the `operation_types(WalletId,Name)` filtered unique index. Requires the same owner racing themselves; the user can simply retry. **Fix:** map the unique violation to 409. *(Note: the original finding cited `task11.Data/Repositories/...`; the actual path is `task11.Infrastructure/Repositories/OperationTypeRepository.cs`.)*

---

#### L-3. Internal .NET/CLR type names leaked in 400 validation messages
**Type:** QA (fuzz).
**Location:** `task11.Web/Program.cs:185-204` (`InvalidModelStateResponseFactory`) + `AddControllers()` at `:238`; messages originate from `System.Text.Json` deserialization.

**Repro:**
```bash
curl -s -X POST http://localhost:8080/api/operations -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"typeId":"<otid>","amount":"abc","date":"2025-01-01T00:00:00Z","walletId":"<wid>"}'
```

**Actual (verified live):** `400` (correct status) but the message leaks `System.Decimal`, and probing further yields `System.Guid`, `System.DateTime`, `System.String`, and the internal model FQN `task11.ApplicationCore.Models.CreateOperationModel`. `AllowInputFormatterExceptionMessages` defaults to `true` and is never overridden; the custom factory copies `ModelState` verbatim. **Impact:** minor info disclosure (framework/class names, no secrets/stack/data); auth still required → low. **Fix:** set `JsonOptions.AllowInputFormatterExceptionMessages = false` or sanitize `ModelState` messages to generic field-level text.

---

#### L-4. FX-converted amount bypasses the 1,000,000,000 cap
**Type:** QA (boundary).
**Location:** `task11.ApplicationCore/Services/OperationService.cs:135-165` (`ApplyConversionAsync`); cap only enforced pre-conversion in `CreateOperationModelValidator.cs:18` / `UpdateOperationModelValidator.cs:15`.

**Repro:** POST an operation with `amount:1000000000, transactionCurrency:"EUR"` into a USD wallet → `201` with persisted `amount=1153900000.00` (>1e9). A direct base-currency `1000000001` is correctly rejected with 400. `GET /api/reports/daily` aggregates the over-cap value into `totalIncome`. **Impact:** boundary inconsistency only (the persisted value is mathematically correct for the rate); no crash/corruption → low. **Fix:** re-check the converted base-currency amount against the cap in `ApplyConversionAsync`, or explicitly document that the cap applies only to the transaction-currency input.

---

#### L-5. 415 served as `application/json` instead of `application/problem+json`
**Type:** QA (protocol).
**Location:** `WalletsController.cs:10`, `OperationsController.cs:10`, `OperationTypesController.cs:10` (`[Produces("application/json")]`).

**Repro:**
```bash
curl -s -D - -o /dev/null -X POST http://localhost:8080/api/wallets \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: text/plain' -d 'hi'
```
Returns `415` with `Content-Type: application/json` even though the body is a valid RFC 9110 problem document. `AuthController` (no `[Produces]`) correctly returns `application/problem+json`. The controller-level `[Produces]` overrides the problem content type. **Impact:** cosmetic header mismatch; clients branching on `application/problem+json` won't recognize the error payload → low. **Fix:** remove the blanket `[Produces("application/json")]`, or add `application/problem+json` to the produced types.

---

#### L-6. Operation listing maps soft-deleted-type ops to invalid `Kind=0`
**Type:** Review (correctness).
**Location:** `task11.ApplicationCore/Services/OperationService.cs:180` (`Kind = operation.OperationType?.Kind ?? default`); list source `OperationRepository.cs:39-45`.

After soft-deleting an in-use type, the included nav is null and `Map` falls back to `default(OperationKind)` = `0`, which is **not** a defined member (enum is `Income=1`/`Expense=2`). `GET /api/wallets/{id}/operations` and `GET /api/operations/{id}` then return `kind:0`. Unlike the report path it does not throw, but produces an out-of-range value. Same root cause/fix as H-2..H-5 (guard type deletion or surface the deleted type's `Kind`). Low.

---

#### L-7. Hardcoded fallback DB credentials + committed connection string
**Type:** Review (security / secrets).
**Location:** `task11.Web/Program.cs:48-49`; `appsettings.json:2-4` and `appsettings.Development.json:2-4`.

Verified: `Program.cs:49` has the literal fallback `"Host=localhost;Port=5432;Database=finance;Username=finance;Password=finance"`, and both (git-tracked) appsettings files contain the same string. These are unmistakable placeholder dev creds, so no real secret is exposed, but committing connection strings + duplicating them as a source fallback normalizes a bad practice and would be a real exposure if reused in a shared/staging environment → low. **Fix:** remove the hardcoded fallback; keep connection strings out of committed config (env/user-secrets).

---

#### L-8. `HasOperationsAsync` ignores the soft-delete filter → wallet currency locked forever
**Type:** Review (data).
**Location:** `task11.Infrastructure/Repositories/WalletRepository.cs:45-53`; `task11.ApplicationCore/Services/WalletService.cs:71-77`.

**Verified in source:** `HasOperationsAsync` calls `.IgnoreQueryFilters().AnyAsync(o => o.WalletId == walletId, ...)` with **no** `!o.IsDeleted` predicate, so it counts soft-deleted operations. After every operation in a wallet is soft-deleted, the rows remain (`IsDeleted=true`) and the method still returns `true`, so `WalletService.UpdateAsync` permanently throws `ConflictException` ("cannot be changed once operations exist") — the guard never clears. **Impact:** over-restricts the rare "change base currency after deleting all operations" case; fail-safe, no security/corruption → low. **Fix:** drop `IgnoreQueryFilters()` here, or add `&& !o.IsDeleted`.

---

#### L-9. Soft-delete via detached `AsNoTracking` entity issues full-row UPDATE → lost update
**Type:** Review (data / concurrency).
**Location:** `OperationRepository.cs:77-84`; `WalletRepository.cs:73-80`; `OperationTypeRepository.cs:73-80`; `UserRepository.cs:72-80`; `SoftDeleteInterceptor.cs:45-56`.

`DeleteAsync` paths load the entity `AsNoTracking()` in the service, then pass the detached instance to `ctx.X.Remove(entity)` in a fresh context. `SoftDeleteInterceptor` flips `Deleted`→`Modified`; because the entity is attached fresh (no original-value snapshot to diff), EF marks **all** properties modified and emits a full-column UPDATE carrying the detached snapshot. With **no** concurrency token (`xmin`/`RowVersion`; grep confirms none), an edit committed by another request between the read and the delete is silently overwritten. `UpdateAsync` shares the pattern. **Impact:** real lost-update risk, but timing-dependent and bounded to overlapping mutations of the same row → low. **Fix:** load+mutate within one context; or add an `xmin`/`RowVersion` concurrency token (surfaces 409 on stale writes); or scope the soft-delete UPDATE to `IsDeleted`/`DeletedAtUtc`/`UpdatedAtUtc` via `entry.Property(...).IsModified`.

---

#### L-10. DB seeding runs outside the startup migration retry pipeline
**Type:** Review (resilience). *Adjusted medium→low.*
**Location:** `task11.Web/Program.cs:323` (call site) and `:326-367` (`SeedAsync`); retry pipeline at `:297-321`.

The 10-attempt exponential Polly pipeline wraps only `factory.MigrateIfRelational()` (`:317-321`). `SeedAsync` (`:323`) — which runs `AnyAsync(db.Users)` + `SaveChangesAsync()` against the same DB — executes **after** the pipeline with no retry; a transient fault there is caught at `:265`, logged Fatal, and rethrown, crashing startup. Exposure is narrow (the DB was just proven reachable by a successful migration), and an orchestrator restart recovers → low. The `AnyAsync` idempotency guard makes retrying safe. **Fix:** run `SeedAsync` inside the same `pipeline.ExecuteAsync(...)`.

---

#### L-11. Response-logging middleware downstream of `UseExceptionHandler` → error responses unlogged
**Type:** Review (observability).
**Location:** `task11.Web/Program.cs:245,247` vs `:253` (`UseMiddleware<RequestResponseLoggingMiddleware>`).

`RequestResponseLoggingMiddleware` is registered **after** `UseExceptionHandler` (`:245`) and `UseStatusCodePages` (`:247`). When a controller throws, the exception unwinds through the logging middleware's `finally` **before** `GlobalExceptionHandler` sets status 503 and writes the ProblemDetails body — so the log records `StatusCode=200` with an empty body, and the actual 503/ProblemDetails the client receives is never captured. **Note:** the original finding's secondary claim (auth at `:252`/the logging middleware itself would 500 with no upstream handler) is **incorrect** — those lines are downstream of `UseExceptionHandler` and *are* mapped; only `CorrelationIdMiddleware` (`:242`) sits above it. Clients still get correct responses; this is purely logging fidelity → low. **Fix:** move `UseMiddleware<RequestResponseLoggingMiddleware>()` to immediately after `CorrelationIdMiddleware`, before `UseExceptionHandler`.

---

#### L-12. FX rate cache has no stampede protection (thundering herd)
**Type:** Review (performance / concurrency).
**Location:** `task11.Infrastructure/Currency/CurrencyConverter.cs:56-74`.

`TryGetValue` (`:56`) misses for all concurrent callers on a cold key, so every in-flight request independently calls `_privatBank`/`_frankfurter` (`:62-64`) then `Set` (`:69/73`). No per-key `SemaphoreSlim`/`GetOrCreateAsync`-with-lock/`AsyncLazy` exists. `IMemoryCache` is thread-safe and last-writer-wins, so no corruption — purely redundant upstream load / wasted FX-provider quota → low. **Fix:** coalesce concurrent identical conversions for a cold key behind a per-key `SemaphoreSlim` (or `LazyCache`/`GetOrCreateAsync` with locking).

---

## 4. What We Tried That Held Up (Coverage)

The following adversarial tests and review dimensions did **not** surface defects — these areas are solid:

- **Malformed-JSON fuzzing (non-duplicate-key):** Truncated/invalid JSON and wrong-type scalar values return clean `400 "Validation failed"` everywhere. The framework's input handling is correct; only the *duplicate-key* path (H-1) and the *message text* (L-3) are problematic — the status codes are right.
- **BOLA / object-ownership:** Per-wallet/per-report ownership checks (`EnsureCanAccess` in `ReportService`/`WalletService`) are present and consistent — a non-owner non-admin gets `403 Forbidden`. No horizontal privilege escalation found via legitimate (unforged) tokens. (The only Admin bypass is via the *committed dev secret*, H-6, not a logic flaw.)
- **Auth tampering / token handling:** JWT validation enforces issuer/audience/signature; the startup guard rejects a missing or <32-char signing secret (`Program.cs:59-63`), and Production leaves `Jwt:Secret` blank to force env injection. The weakness is committed *dev* config, not the validation logic.
- **Boundary checks (non-FX):** Direct base-currency amount cap (`1,000,000,000`) is correctly enforced (`1000000001` → 400). The only gap is the *post-conversion* path (L-4).
- **HTTP protocol conformance:** Correct status codes throughout (`400/401/403/404/409/415/503`); RFC 9110/7807 ProblemDetails bodies are well-formed. The only protocol nit is the 415 `Content-Type` header on `[Produces]` controllers (L-5) — body and status are correct.
- **Unique-constraint integrity:** Filtered unique indexes on `users(Username)` and `operation_types(WalletId,Name)` correctly prevent duplicate rows even under the concurrency races — data integrity is *preserved*; only the HTTP status on the loser is wrong (M-2/L-1/L-2).
- **Resilience / fail-safe behavior:** FX provider failures fail *safe* to `503 FxUnavailableException` (no crash, no partial writes). The retry config is sub-optimal (M-3) but not unsafe.
- **Process/health stability:** Even the 100%-reproducible H-1 fault leaves `/health=200` and the process up; no stack traces leak to clients; no observed memory/connection leak. Individual malformed requests fail in isolation.
- **SQL injection / raw SQL:** All data access goes through EF Core LINQ with parameterization; no raw/string-concatenated SQL was found.

---

## 5. Prioritized Fix List

### High (fix before any deployment)
1. **`LogSanitizer` crash (H-1):** catch `Exception` (not just `JsonException`) around `Parse`+`Redact`, or parse with `JsonDocument`/`Utf8JsonReader`. *One-line fix; clears the highest-impact, unauthenticated 500.*
2. **Guard in-use OperationType deletion (H-2, H-3, H-4, H-5, and L-6):** add `OperationTypeRepository.HasOperationsAsync` + throw `ConflictException` in `OperationTypeService.DeleteAsync` (mirror the wallet guard). *Single change resolves the entire soft-delete report/data cluster.*
3. **Harden `ReportService.MapToLine` / report queries (defense-in-depth for H-2..H-4):** replace `OperationType!.Kind` with a null-safe projection and/or `IgnoreQueryFilters()` so a stray soft-deleted type can never NRE or silently drop a row.
4. **Remove committed dev JWT secret (H-6):** load from user-secrets/env; reject known-default secret values at startup.
5. **Remove default admin credentials (H-7):** require `Seed:AdminPassword` explicitly; fail startup on missing/default; drop the committed default.

### Medium
6. **Map unique-violation → 409 (M-2, L-1, L-2):** catch `DbUpdateException`/`PostgresException 23505` in `Add`/`Update` paths and rethrow `ConflictException`. *Single helper resolves three findings.*
7. **Atomic create-operation (M-1):** wrap read-validate-insert in one `DbContext`/transaction.
8. **FX timeout config (M-3):** `HttpClient.Timeout = Timeout.InfiniteTimeSpan`; set `AttemptTimeout`/`TotalRequestTimeout` on the resilience handler.

### Low (hardening / hygiene)
9. Sanitize/disable internal-type leakage in validation messages — `AllowInputFormatterExceptionMessages = false` (L-3).
10. Re-apply the amount cap to the FX-converted value (L-4).
11. Allow `application/problem+json` on `[Produces]` controllers (L-5).
12. Remove hardcoded fallback DB connection string / keep connection strings out of committed config (L-7).
13. Fix `HasOperationsAsync` to count only live operations (`&& !o.IsDeleted`) (L-8).
14. Add an `xmin`/`RowVersion` concurrency token (or scope soft-delete UPDATE columns) to prevent lost updates (L-9).
15. Run `SeedAsync` inside the startup retry pipeline (L-10).
16. Move `RequestResponseLoggingMiddleware` above `UseExceptionHandler` for correct error-response logging (L-11).
17. Add FX-cache stampede protection (per-key lock / `GetOrCreateAsync`) (L-12).

**Note on path drift:** the recent in-progress refactor (`task11.Data/*` → `task11.ApplicationCore/Entities/*`, `task11.Infrastructure/Repositories/*`) means a couple of original findings cite `task11.Data/...`; the live, verified paths are under `task11.Infrastructure/...` and `task11.ApplicationCore/...` as referenced above.