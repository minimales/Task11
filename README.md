# Personal Finance Web API

ASP.NET Core 9 Web API for personal finance management: users + JWT auth, per-user wallets,
wallet-scoped operation types, financial operations with online historical currency conversion,
and daily / period reports. PostgreSQL 16 via EF Core 9 (Npgsql). Fully dockerized.

## Run with Docker (recommended)

```bash
cp .env.example .env        # then edit secrets (POSTGRES_PASSWORD, JWT_SECRET >= 32 chars)
docker compose up -d
```

This starts PostgreSQL and the API. The app waits for the database to be healthy, applies EF Core
migrations on startup (with a Polly retry), and seeds a default admin user and a shared wallet.

- Swagger UI: <http://localhost:8080/swagger>
- Health check: <http://localhost:8080/health>

Data persists across `docker compose down` / `up` via the named `pgdata` volume.

### Default credentials

A default administrator is seeded on first run:

- **Username:** `admin`
- **Password:** `admin`

Change these in production via the `Seed:*` configuration keys.

## Get a JWT and authorize in Swagger

1. `POST /api/auth/login` with `{ "username": "admin", "password": "admin" }`.
2. Copy the `token` from the response.
3. In Swagger, click **Authorize**, enter `Bearer {token}`, and confirm.
4. All secured endpoints now send the token.

## Endpoints

| Method | Route | Auth |
|---|---|---|
| POST | `/api/auth/login` | Anonymous |
| GET/POST | `/api/users`, `/api/users/{id}` (GET/PUT/DELETE) | Admin |
| GET/POST/PUT/DELETE | `/api/wallets`, `/api/wallets/{id}` | Authenticated (owner) |
| GET/POST | `/api/wallets/{walletId}/operation-types` | Authenticated |
| GET/PUT/DELETE | `/api/operation-types/{id}` | Authenticated |
| GET | `/api/wallets/{walletId}/operations` | Authenticated |
| GET/POST/PUT/DELETE | `/api/operations`, `/api/operations/{id}` | Authenticated |
| GET | `/api/reports/daily?walletId=&date=` | Authenticated |
| GET | `/api/reports/period?walletId=&startDate=&endDate=` | Authenticated |
| GET | `/health`, `/swagger` | Anonymous |

All routes except `login`, `/health` and `/swagger*` require a valid JWT (a global fallback
authorization policy fails closed if `[Authorize]` is ever forgotten).

## UTC / Npgsql note

Npgsql 8 maps every `DateTime` to `timestamp with time zone` and **rejects values whose
`Kind` is not `Utc`**. The API converts all inbound dates to UTC at the boundary
(`DateTime.SpecifyKind(..., Utc)` / `.ToUniversalTime()`), and audit timestamps come from the
injected `IClock`, which returns UTC. Currency conversion uses the operation's UTC date for the
historical rate lookup.

## Currency conversion

A wallet has a **base currency**; every operation is stored in that base currency. When an operation
is entered in a different `transactionCurrency`, the historical rate **for the operation date**
(back-dating supported) is fetched, the amount is converted, and the original is appended to the
note, e.g. `[Original: 100 EUR @ 48.3734 on 2025-10-02 → 4837.34 UAH]`.

Two free, key-less providers are used, selected by currency:

- **PrivatBank** (`exchange_rates` API) for any pair involving **UAH** — uses the NBU official rate.
  Frankfurter (ECB) does not publish the hryvnia, so PrivatBank covers the task's base-currency example.
- **Frankfurter** (ECB rates) for all other pairs (USD/EUR/GBP/…).

Historical rates are immutable and cached in-memory by `(from, to, date)`. If a rate cannot be
obtained after retries, the API returns **503** rather than storing an unconverted amount.

## Local development (without Docker)

Requires the .NET 9 SDK and a reachable PostgreSQL instance.

```bash
dotnet restore
dotnet build task11.sln
dotnet test task11.sln                 # 113 unit tests
dotnet run --project task11.Web        # applies the InitialCreate migration on startup
```

Set `Jwt:Secret` (or `JWT__Secret`) to a value of at least 32 characters; the app refuses to
start otherwise. The `InitialCreate` migration already ships in the repo and is applied
automatically on startup; to add a *new* migration later, a `DesignTimeDbContextFactory` lets
`dotnet ef migrations add <Name>` run without a live database or the web host.

## Project layout

```
task11.Data/             Data layer (AppDbContext, Entities, EntityConfigurations, Interceptors, Migrations)
task11.ApplicationCore/  Application layer (Services, Repositories, Models, Validators, Auth, Currency)
task11.Web/              Host / API (Program, Controllers, Middleware, Infrastructure)
task11.{Data,ApplicationCore,Web}.Tests.UnitTesting.MSTest/   MSTest unit tests
```
