# Chronos — Implementation Plan

Status snapshot as of commit `d78cd5d` plus this pass's uncommitted work (below). This file
tracks what's built, what's known broken or unverified, and what's next — a working reference
for picking the project back up, not a permanent spec.

Note: this repo had several Claude sessions working in the same working tree concurrently during
this pass (backend items below, plus frontend auth/theming work from other sessions) — if
anything here looks inconsistent with what's actually on disk, `git status`/`git diff` are more
current than this file.

## Stack

- Backend: .NET 10, ASP.NET Core minimal API, EF Core, MediatR, FluentValidation
- Multi-DB: SQL Server / SQLite / PostgreSQL, switched at startup via `DatabaseProvider` config
  (`Chronos.Infrastructure.DependencyInjection.AddChronosInfrastructure`)
- Frontend: Angular 22, standalone components, signals, Jest
- E2E: Playwright (scaffolded, not wired into CI yet)
- Architecture: Clean Architecture / modular monolith (Domain → Application → Infrastructure → Api)

## Done

- **Projects/Gantt**: `Project` aggregate with `GanttTask`/`TaskDependency` children, full
  CQRS vertical slice, repository, endpoint, tests at every layer. Gantt UI wraps Frappe Gantt
  (MIT) behind an `IGanttRenderer` port for testability and to keep the third-party dependency
  swappable.
- **Leave (ferie) workflow**: `LeaveRequest` aggregate (create/approve/reject/cancel), same
  full-slice treatment. Cancel enforces requester-or-Admin ownership.
- **Auth**: JWT bearer tokens, PBKDF2-SHA256 password hashing (BCL only), `User` aggregate with
  roles (Employee/Approver/Admin). Every endpoint derives organization/requester/approver
  identity from the token's claims, never from the request body. `ApproverOrAdmin` policy on
  approve/reject.
- **CI**: `.github/workflows/ci.yml`, backend matrixed over the three DB providers (Sqlite runs
  the fast suite, SqlServer/PostgreSql run their Testcontainers-backed repository tests), plus a
  frontend Jest job.
- **Frontend auth wiring**: `AuthService` (signal-based session, localStorage-persisted,
  expiry-aware), `authInterceptor`, `authGuard`, `LoginComponent`. `LeaveRequestsPageComponent`
  is routed at `/leave` behind the guard.
- **Frontend Jest config**: dependency versions matched to Angular 22 (`jest-preset-angular` v17
  + `jest`/`jest-environment-jsdom` v30), `angular.json` polyfills fixed, `frappe-gantt` typed
  via an ambient module + `IGanttRenderer` port, `jest.config.ts` extends (not replaces) the
  preset's `transformIgnorePatterns`. `package-lock.json` is committed and CI uses `npm ci`.
- **Backend actually builds and all non-container tests actually pass** (previously only
  reasoned about statically — see below for what running things for real turned up).
- **CI confirmed green end-to-end**: after committing the previous pass's fixes, all four CI jobs
  (Sqlite/SqlServer/PostgreSql/Frontend) passed on `main` at commit `d78cd5d` — including the
  Testcontainers-backed SqlServer/PostgreSql legs, which couldn't be verified locally (no Docker
  in this environment) until CI ran them.
- **Frontend CI now uses `npm ci`** instead of `npm install`, now that `package-lock.json` is
  committed and verified (`.github/workflows/ci.yml`).
- **Admin role-promotion endpoint**: `PATCH /api/users/{id}/role` (`AdminOnly` policy), full
  vertical slice (`PromoteUserRoleCommand`/Handler/Validator under
  `Chronos.Application/Users/Commands/PromoteUserRole/`, `Chronos.Api/Endpoints/UserEndpoints.cs`).
  Scoped to the caller's own organization — an Admin can't promote/demote a user in a different
  org; a user not found *or* in another org gets the same failure message, so the response never
  leaks which is which. This is now the only way to get an Approver/Admin account besides a direct
  DB write. Tests at Application (handler) and Api (endpoint, incl. cross-org rejection) layers.
- **`Jwt:SigningKey` moved out of `appsettings.json`**: only `appsettings.Development.json` now
  carries a value (a fixed, non-secret dev/test key). Every other environment must supply it via
  `Jwt__SigningKey` env var, a secret manager, or `dotnet user-secrets`; `Program.cs` throws a
  clear, actionable error at startup if it's missing — verified by actually starting the app
  under `ASPNETCORE_ENVIRONMENT=Production` with no key set.
- **Real EF Core migrations, replacing the `EnsureCreated()` stopgap.** Since EF Core migrations
  are provider-specific (a `Migration`'s generated SQL is tied to the dialect it was generated
  against) and this app switches providers at runtime against one shared `DbContext`, a single
  migrations history can't cover all three. Solution: three thin `ChronosDbContext` subclasses —
  `SqlServerChronosDbContext`/`SqliteChronosDbContext`/`PostgreSqlChronosDbContext`
  (`Chronos.Infrastructure/Persistence/`) — each adding nothing but existing so its own migrations
  (`Persistence/Migrations/<Provider>/`) have somewhere to live; `AddChronosInfrastructure`
  registers whichever one matches the active `DatabaseProvider` as the concrete implementation
  behind the base `ChronosDbContext` service type (`services.AddDbContext<ChronosDbContext,
  TSubclass>(...)`), so nothing upstream needs to know which is active. `Program.cs` now calls
  `Database.Migrate()` at startup instead of `EnsureCreated()`. See README's new "Migrations"
  section for how to add a migration going forward (must be regenerated for all three providers).
  Added `Microsoft.EntityFrameworkCore.Design` to `Chronos.Api`/`Chronos.Infrastructure` and a
  local tool manifest (`.config/dotnet-tools.json`, pinning `dotnet-ef 10.0.11`) since neither
  existed before.

## This pass: found and fixed by actually running the full stack for the first time

Everything here was found by running `dotnet build`/`dotnet test`/`npm test`, not by reading
code. All are fixed in the working tree (uncommitted — see Suggested next steps).

1. **`Program.cs` was missing `using FluentValidation;`.** The `AddValidatorsFromAssembly`
   extension method lives in that namespace even though the package is
   `FluentValidation.DependencyInjectionExtensions`; the package reference alone (already
   present) doesn't get you the `using`. One-line fix.
2. **Three Api.Tests files called `IWebHostBuilder.UseEnvironment` with no matching using.**
   The overload that actually applies to `IWebHostBuilder` lives in
   `Microsoft.AspNetCore.Hosting` — the same-named overload in `Microsoft.Extensions.Hosting`
   (for `IHostBuilder`) is a trap that compiles a `using` but resolves to the wrong extension and
   fails at the call site.
3. **No EF Core migrations exist anywhere in the repo, and nothing ever created the schema.**
   Every repository test builds its own `DbContext` and calls `EnsureCreated()` directly, so this
   was never exercised — but the real `Program.cs` → `AddChronosInfrastructure` path never
   created a table. Every endpoint that touched the database (register, login, create leave
   request, ...) 500'd with `SQLite Error 1: 'no such table'`. Fixed by calling
   `Database.EnsureCreated()` once at startup, scoped, before `app.Run()`. This is a stand-in for
   real migrations (still not started — see below).
4. **No `JsonStringEnumConverter` registered.** `LeaveType` (and any future enum) in a JSON
   request body only accepted its numeric value; a client sending `"Vacation"` (the obvious,
   documented shape) got a 400 `BadHttpRequestException` before the request reached a handler.
   Fixed via `ConfigureHttpJsonOptions` in `Program.cs`, which also makes enum values serialize
   as their name in responses.
5. **`Chronos.Api.Tests` had no database isolation.** All three test classes (`AuthEndpointsTests`,
   `LeaveEndpointsTests`, `ProjectEndpointsTests`) pointed at the same relative
   `chronos.db` from `appsettings.Development.json` — collides across test classes (parallel
   xUnit execution) and across `dotnet test` runs (leftover users trip unique-email checks).
   Fixed with `IsolatedTestFactory.WithIsolatedSqlite()`: overrides the `ChronosDbContext`
   registration itself (via `ConfigureServices`, not `ConfigureAppConfiguration` — the connection
   string in `AddChronosInfrastructure` is read eagerly, before the test host splices in
   config overrides, so overriding config alone doesn't take effect) to a fresh temp-file Sqlite
   db per factory build.
6. **One frontend test was genuinely flaky against a real Angular quirk, not a component bug.**
   `gantt-chart.component.spec.ts`'s "delegates to updateTasks" case wrapped
   `GanttChartComponent` in a host component and mutated a plain host property before calling
   `fixture.detectChanges()` again. `ngDoCheck` fires on the second pass (the host view is
   checked) but the `[tasks]="tasks"` property binding itself is never re-evaluated, so the
   child's signal input goes stale — a known Angular testing rough edge with signal inputs
   (see [angular/angular#56863](https://github.com/angular/angular/issues/56863)). Root cause is
   in Angular's test fixture machinery, not `GanttChartComponent`. Fixed by testing
   `GanttChartComponent` directly and driving its input via `fixture.componentRef.setInput(...)`
   (Angular's documented way to drive signal/required inputs in tests) instead of a host-template
   rebind.

**Verified state after the previous pass's fixes:**
- Backend: `dotnet build` clean, `dotnet test --filter "Category!=Container"` →
  **50/50 passing** (Domain 14, Application 15, Infrastructure 9, Api 12).
- Frontend: `npm ci` (627 packages, 0 vulnerabilities) then `npm test -- --ci` →
  **16/16 passing**, 6/6 suites.
- `ng build` / `ng serve` do **not** run locally: Angular 22's CLI requires Node ≥22.22 and this
  machine has Node 20.20. Not a repo bug — CI already pins Node 22
  (`.github/workflows/ci.yml`). Jest doesn't go through the CLI, so tests are unaffected.

**Verified state after this pass's work** (role promotion + JWT config + migrations, above):
- Backend: `dotnet build` clean, `dotnet test --filter "Category!=Container"` →
  **56/56 passing** (Domain 14, Application 18, Infrastructure 9, Api 15) — the +6 are the new
  `PromoteUserRoleCommandHandler` and `UserEndpoints` tests.
- Manual smoke test: fresh `dotnet run` (no pre-existing db file) + `POST /api/auth/register` →
  `201 Created`, proving `Database.Migrate()` actually creates the schema from the generated
  migrations on a brand-new database, not just under `EnsureCreated()`'s more forgiving behavior.
- SqlServer/PostgreSql migrations are generated and inspected (correct provider-specific column
  types — `uniqueidentifier`/`nvarchar` vs. `uuid`/`character varying`) but **not run against real
  containers locally** (no Docker in this environment); the Testcontainers-backed tests now call
  `MigrateAsync()` instead of `EnsureCreatedAsync()`, so CI is the actual verification for those
  two providers — confirm CI is green on all four jobs before treating this as fully proven.

## Known issues (still open)

- **SqlServer/PostgreSql migrations unverified against real databases locally** (see above) —
  CI needs to confirm this, this pass could only verify Sqlite end-to-end.
- **Cross-org Admin visibility** for leave requests (every user, Admin included, only ever sees
  their own organization) — same gap now also applies to the new role-promotion endpoint, by
  design (see "Admin role-promotion endpoint" above); a true cross-org view is still not started.

## Not started

- **E2E in CI**: needs the API + a database running alongside the frontend dev server, more
  orchestration than covered so far. Decided to keep this local-only/manual for now rather than
  build that orchestration speculatively — revisit if E2E starts getting skipped in practice
  because it's not in CI.
- **Refresh-token flow** on the frontend (an expired JWT is just treated as logged out).
- **Open self-registration** into any `OrganizationId` supplied by the caller — no invite or
  verification step.

## Suggested next steps, in order

1. Confirm CI is green on all four jobs for this pass's commit, same as the previous pass — this
   time the SqlServer/PostgreSql legs are the real first-ever test of the new migrations against
   real containers.
2. Cross-org Admin visibility for leave requests (and reconsider whether role-promotion should
   eventually support it too, e.g. a super-admin managing multiple orgs — not needed today).
3. Refresh-token flow on the frontend.
4. Decide on open self-registration (invite/verification step) before this goes beyond local dev.
