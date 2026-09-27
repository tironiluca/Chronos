# Chronos — Implementation Plan

Status snapshot as of commit `8d44228` plus this pass's uncommitted fixes (below). This file
tracks what's built, what's known broken or unverified, and what's next — a working reference
for picking the project back up, not a permanent spec.

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
  preset's `transformIgnorePatterns`. `package-lock.json` is committed; CI uses `npm install`
  still (see Not started).
- **Backend actually builds and all non-container tests actually pass** (previously only
  reasoned about statically — see below for what running things for real turned up).

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

**Verified state after these fixes:**
- Backend: `dotnet build` clean, `dotnet test --filter "Category!=Container"` →
  **50/50 passing** (Domain 14, Application 15, Infrastructure 9, Api 12). SqlServer/PostgreSql
  Testcontainers-backed tests not run locally (no Docker in this environment) — CI covers them.
- Frontend: `npm ci` (627 packages, 0 vulnerabilities) then `npm test -- --ci` →
  **16/16 passing**, 6/6 suites.
- `ng build` / `ng serve` do **not** run locally: Angular 22's CLI requires Node ≥22.22 and this
  machine has Node 20.20. Not a repo bug — CI already pins Node 22
  (`.github/workflows/ci.yml`). Jest doesn't go through the CLI, so tests are unaffected.

## Known issues (still open, not addressed this pass)

- **`package-lock.json` is committed but CI still uses `npm install`, not `npm ci`.** Now that
  the lockfile is real and verified, switch CI to `npm ci` for reproducible installs.
- **No EF Core migrations.** `EnsureCreated()` (added this pass) is a stopgap: fine for
  Sqlite/dev, but it can't express schema *changes* over time, and SqlServer/PostgreSql in
  production would need real migrations before this ships anywhere.

## Not started

- **E2E in CI**: needs the API + a database running alongside the frontend dev server, more
  orchestration than covered so far; currently documented as a manual/local-only step.
- **Cross-org Admin visibility** for leave requests (currently every user, Admin included, only
  ever sees their own organization).
- **Refresh-token flow** on the frontend (an expired JWT is just treated as logged out).
- **Role promotion** (Employee → Approver/Admin): no admin-only endpoint exists yet; only doable
  today by writing directly to the database.
- **`Jwt:SigningKey` in `appsettings.json` is a placeholder** — must move to user-secrets/
  environment/Key Vault before anything beyond local dev.
- **Open self-registration** into any `OrganizationId` supplied by the caller — no invite or
  verification step.

## Suggested next steps, in order

1. Commit this pass's fixes (Program.cs, three Api.Tests files, the new
   `IsolatedTestFactory`, the rewritten gantt-chart spec), push, confirm CI goes green on all
   four jobs (Sqlite/SqlServer/PostgreSql/Frontend) — the SqlServer/PostgreSql legs are still
   unverified locally (no Docker here).
2. Switch frontend CI from `npm install` to `npm ci` now that the lockfile is trustworthy.
3. Wire E2E into CI (or explicitly decide it stays local-only for now).
4. Admin role-promotion endpoint, since it's currently the only way to get an Approver/Admin
   user outside of direct DB writes (tests already do this directly against `ChronosDbContext`,
   which is fine for tests but isn't a real operational path).
5. Move the JWT signing key out of `appsettings.json`.
6. Introduce real EF Core migrations to replace the `EnsureCreated()` stopgap.
