# Chronos

Intra-group tool for **Gantt scheduling, project tracking and leave (ferie) management**
across multiple companies of the same group.

## Architecture

Clean Architecture / modular monolith. SOLID maps directly onto the layer boundaries:

- **Chronos.Domain** — entities, invariants, zero external dependencies (DIP anchor).
- **Chronos.Application** — use cases (MediatR commands/queries), repository *interfaces*.
  Depends only on Domain.
- **Chronos.Infrastructure** — EF Core, repository *implementations*, one `DbContext`
  shared across providers. The active provider (SQL Server / SQLite / PostgreSQL) is
  selected at startup from configuration — nothing above this layer knows which one is in use.
- **Chronos.Api** — ASP.NET Core minimal API, composition root.

```
src/
  Chronos.Domain/
  Chronos.Application/
  Chronos.Infrastructure/
  Chronos.Api/
tests/
  Chronos.Domain.Tests/          xUnit, pure logic
  Chronos.Application.Tests/     xUnit + NSubstitute
  Chronos.Infrastructure.Tests/  Sqlite in-memory (fast) + Testcontainers fixtures (SqlServer/PostgreSql, CI only)
  Chronos.Api.Tests/             WebApplicationFactory
frontend/
  chronos-web/                   Angular 22, standalone, signals, Jest
e2e/
  chronos-e2e/                   Playwright
```

## Switching database provider

`appsettings.json`:

```json
{
  "DatabaseProvider": "Sqlite" // or "SqlServer" / "PostgreSql" — Sqlite is the default so the app can be self-hosted with zero external DB setup
}
```

Each provider has a matching entry under `ConnectionStrings`. `Chronos.Infrastructure.DependencyInjection`
is the single place that branches on this value (`AddChronosInfrastructure`) — see that file before
adding anything provider-specific elsewhere. Avoid provider-specific SQL (raw SQL, date functions) in
queries so LINQ stays portable across all three; if something is genuinely unavoidable, isolate it
behind an interface in Application rather than branching inline.

### Migrations

EF Core migrations are provider-specific (a `Migration`'s `Up()`/`Down()` bakes in the target SQL
dialect), so a single `DbContext` can't hold one shared history across SQL Server/SQLite/PostgreSQL.
Each provider instead gets its own thin `ChronosDbContext` subclass —
`SqlServerChronosDbContext`/`SqliteChronosDbContext`/`PostgreSqlChronosDbContext`
(`src/Chronos.Infrastructure/Persistence/`), adding nothing but existing so its migrations
(`Persistence/Migrations/<Provider>/`) have somewhere to live. `AddChronosInfrastructure` registers
whichever subclass matches the active `DatabaseProvider` as the concrete implementation behind the
base `ChronosDbContext` service type, so repositories/tests never need to know which one is active.
`Program.cs` calls `Database.Migrate()` at startup (not `EnsureCreated()`), which creates the schema
from scratch on a fresh database and applies only pending migrations otherwise.

To add a migration after a model change, regenerate it for **all three** providers (repeat with
`SqlServer/SqliteChronosDbContext`/`SqlServer` swapped in), e.g. for SQLite:
```
dotnet ef migrations add <Name> --project src/Chronos.Infrastructure --startup-project src/Chronos.Api \
  --context SqliteChronosDbContext --output-dir Persistence/Migrations/Sqlite
```
For SqlServer/PostgreSql, override the provider the startup project reads at design time, since the
default is Sqlite: `DatabaseProvider=SqlServer dotnet ef migrations add ... --context
SqlServerChronosDbContext --output-dir Persistence/Migrations/SqlServer` (same pattern for
PostgreSql/`PostgreSqlChronosDbContext`). `dotnet-ef` is pinned via the local tool manifest
(`.config/dotnet-tools.json`) — run `dotnet tool restore` once after cloning.

## Gantt UI: library choice

**Frappe Gantt** (MIT license, ~6k GitHub stars, used in production by Frappe/ERPNext) was chosen over
Angular-native alternatives (`ngx-gantt`) and commercial libraries (Bryntum, Syncfusion, DHTMLX) because:

- MIT is unambiguous for closed-source commercial use, no license fee, no GPL disclosure obligations.
- It is framework-agnostic (vanilla JS/SVG rendering), which is used here as an advantage: it is
  wrapped behind an `IGanttRenderer` port (`frontend/chronos-web/src/app/features/gantt/gantt-renderer.ts`)
  so the rendering engine can be swapped later without touching `GanttChartComponent` or its consumers —
  DIP applied at the third-party-dependency boundary, and it makes the component trivially unit-testable
  with a fake renderer (no real SVG engine needed in Jest).

If a future requirement needs resource histograms, baselines, or critical-path highlighting beyond
what Frappe Gantt offers, evaluate DHTMLX Gantt Community Edition (GPLv2, requires either open-sourcing
Chronos or a commercial license) at that point — not before, since it isn't license-cost-free.

## Auth

JWT bearer tokens, issued by `POST /api/auth/login`. `POST /api/auth/register` self-registers
into an existing `OrganizationId` (passed as a string and parsed explicitly in `AuthEndpoints.cs`,
returning a clean `400` on a malformed GUID rather than an unhandled exception) as an `Employee`
(the only role self-registration can create). `PATCH /api/users/{id}/role` (`AdminOnly` policy,
body `{ "role": "Approver" }`) promotes/demotes a user within the caller's own organization — see
`PromoteUserRoleCommandHandler`; it's currently the only path to an Approver/Admin account besides
direct DB writes. Passwords are hashed with PBKDF2-SHA256 (`Pbkdf2PasswordHasher`, BCL only, no
extra dependency).

`GET /api/users/me/rights` returns the caller's granular permission codes, resolved from a
`Right`/`RoleRight` mapping table (`Chronos.Domain.Users`) seeded per `UserRole`
(Employee/Approver/Admin) via migration. This is additive alongside the existing role enum, not a
replacement — `User.Role`, role promotion, and JWT role claims are unaffected.

`POST /api/auth/change-password` (authenticated) rotates a user's password: it blocks reuse of the
last 5 passwords via a `PasswordHistory` table (one row per superseded hash) and records the new
supersession on each change.

`POST /api/departments` (`AdminOnly`, scoped to the caller's own org) creates a `Department`
(optionally nested under another via `ParentDepartmentId`); `GET /api/departments` lists the
caller's org's departments and is open to any authenticated user (not Admin-only — needed to
populate department pickers). `PATCH /api/users/{id}/department` (`AdminOnly`) assigns or
unassigns (`null`) a user's department, same cross-org scoping as role promotion. First piece of
the resource-availability epic — see `IMPLEMENTATION_PLAN.md`.

The token carries `NameIdentifier` (user id), `Role`, and a custom `org` claim (organization id).
`Chronos.Api.Security.ClaimsPrincipalExtensions` reads these back out. Every endpoint derives
`OrganizationId`/requester/approver identity from the token, never from the request body — a
client cannot act as another user or write into another organization by supplying a different id
in the payload. `/approve` and `/reject` additionally require the `ApproverOrAdmin` policy;
`/cancel` allows the original requester or an Admin (checked in `CancelLeaveRequestCommandHandler`,
not just at the endpoint).

`Jwt:SigningKey` is **not** in `appsettings.json` — only `appsettings.Development.json` carries a
fixed, non-secret dev/test value. Any other environment must supply it out-of-band (`dotnet
user-secrets set Jwt:SigningKey <value>` locally, a `Jwt__SigningKey` environment variable or Key
Vault reference in real deployments); `Program.cs` fails fast at startup if it's missing.

See `IMPLEMENTATION_PLAN.md` for known gaps and open items (validation pipeline, refresh tokens,
registration, cross-org visibility).

Frontend: `AuthService` holds the session as a signal (persisted to `localStorage`), `authInterceptor`
attaches the bearer token to every request except `/api/auth/*`, `authGuard` protects routes.
`LoginComponent` is routed at `/login`.

## Leave (ferie) workflow

`POST /api/leave-requests` (create, Pending; body is just `{ type, startDate, endDate }` — the
server fills in organization/requester from the token) → `POST /api/leave-requests/{id}/approve`
(`ApproverOrAdmin` only), `/reject` (`ApproverOrAdmin`, body `{ reason }`), or `/cancel`
(requester or Admin). `GET /api/leave-requests` lists the caller's own organization. Domain
invariants (only a Pending request can be approved/rejected/cancelled) are enforced on
`LeaveRequest` itself and surfaced as `400` rather than a `500` — see
`ApproveLeaveRequestCommandHandler` for the try/catch-and-translate pattern reused by Reject/Cancel.

Frontend: `LeaveRequestFormComponent` + `LeaveRequestListComponent` (approve/reject/cancel on
Pending items), composed by `LeaveRequestsPageComponent`, routed at `/leave` behind `authGuard`.
Neither component takes an identity input — both rely on the JWT via `authInterceptor`.

## Running locally

Backend:
```
dotnet restore
dotnet run --project src/Chronos.Api
```

Frontend:
```
cd frontend/chronos-web
npm install
npm start
```

Unit tests:
```
dotnet test                                          # everything, including SqlServer/PostgreSql
                                                      # containers (needs Docker running locally)
dotnet test --filter "Category!=Container"           # fast path, no Docker required
cd frontend/chronos-web && npm test
```

`tests/Chronos.Infrastructure.Tests/Containers/` holds the two tests tagged `Category=Container`:
the same repository round trips as the Sqlite-backed tests elsewhere in that project, but run
against a real SQL Server / PostgreSQL container via Testcontainers, to prove the multi-DB
abstraction actually holds on those engines and not only on Sqlite.

## CI

`.github/workflows/ci.yml` runs a `backend` job as a 3-way matrix (Sqlite, SqlServer, PostgreSql):
the Sqlite leg runs everything except the `Container`-tagged tests; the other two legs run only
their respective Testcontainers-backed repository tests (Docker is preinstalled on GitHub-hosted
runners, no extra setup needed). A separate `frontend` job runs the Jest suite. E2E (Playwright)
is not wired into CI yet -- it needs the API and a database running alongside the frontend dev
server, which is more orchestration than this pass covers; run it locally for now.

E2E:
```
cd e2e/chronos-e2e
npm install
npx playwright test
```
