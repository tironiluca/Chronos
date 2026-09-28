# Chronos — Implementation Plan

Status as of the resource-availability query (`GET /api/resources/availability`, step 4 of the
resource-availability epic, not yet committed as of 2026-09-28 — see git status/diff for the
authoritative state). Working reference for picking the project back up — not a spec. Compressed
to gotchas/decisions worth keeping; full narrative is in git history (`git log --oneline`) if ever
needed.

Multiple Claude sessions work this tree concurrently — if this file disagrees with disk,
`git status`/`git diff` win.

## Stack

- Backend: .NET 10, ASP.NET Core minimal API, EF Core, MediatR, FluentValidation
- Multi-DB: SQL Server / SQLite / PostgreSQL, switched at startup via `DatabaseProvider` config
  (`Chronos.Infrastructure.DependencyInjection.AddChronosInfrastructure`)
- Frontend: Angular 22, standalone components, signals, Jest
- E2E: Playwright (scaffolded, not wired into CI)
- Architecture: Clean Architecture / modular monolith (Domain → Application → Infrastructure → Api)

## Done

- **Projects/Gantt**: `Project` aggregate + `GanttTask`/`TaskDependency`, full CQRS slice. UI
  wraps Frappe Gantt behind an `IGanttRenderer` port.
- **Leave (ferie) workflow**: `LeaveRequest` aggregate (create/approve/reject/cancel), full slice.
  Cancel enforces requester-or-Admin ownership.
- **Auth**: JWT bearer, PBKDF2-SHA256 (BCL only), `User` with roles (Employee/Approver/Admin).
  Endpoints derive org/requester/approver identity from JWT claims, never the request body.
  `Jwt:SigningKey` must come from env var/secret manager outside `Development` (`Program.cs`
  throws a clear startup error if missing).
- **Org + user self-registration**: `POST /api/organizations`, `POST /api/auth/register` + frontend
  flow. Intentionally open/ungated (see Known issues).
- **Admin role-promotion**: `PATCH /api/users/{id}/role` (`AdminOnly`), scoped to caller's own org
  (cross-org target = same not-found error, doesn't leak existence).
- **Rights + password rotation**: `Right`/`RoleRight` tables (`UserRole` → permission codes,
  additive, doesn't touch `User.Role`/JWT claims), `GET /api/users/me/rights`. `PasswordHistory`
  blocks reuse of last 5 passwords (`POST /api/auth/change-password`).
- **Real EF Core migrations**: three thin `ChronosDbContext` subclasses (`Sqlite`/`SqlServer`/
  `PostgreSql`), each with its own migrations dir under `Persistence/Migrations/<Provider>/`
  (dialect-specific). Adding a migration going forward = regenerate for all three (see README's
  "Migrations" section).
- **FluentValidation wired into MediatR**: `ValidationBehavior<TRequest, TResponse>` via
  `cfg.AddOpenBehavior`, constrained to `where TResponse : Result`, short-circuits with a typed
  `Failure` before the handler runs (was previously dead code — [[project_validation_pipeline_gap]]
  memory, now stale/fixed). Handler-level guard clauses left in place as redundant
  defense-in-depth; a few still have stale "validators aren't wired yet" comments — harmless, fix
  opportunistically when touching those files.
- **CI**: `.github/workflows/ci.yml`, backend matrixed over 3 DB providers (SqlServer/PostgreSql
  via Testcontainers) + frontend Jest job (`npm ci`, lockfile committed).
- **`Department` entity**: `Department` (`OrganizationId`, `Name`, `Code`, `ParentDepartmentId`
  nullable, self-referencing) + `User.DepartmentId` (nullable — registration doesn't assign one
  yet). Full slice: `POST`/`GET /api/departments`, `PATCH /api/users/{id}/department`.
  `ParentDepartmentId`/`User.DepartmentId` are plain scalars, not EF relationships — matches this
  codebase's convention of referencing other aggregates by id only (`User.OrganizationId`,
  `RoleRight.RightId`). Migration: `AddDepartments`.
- **`GanttTask.AssignedUserId`** (nullable): `AssignUser(Guid?)` method on `GanttTask` itself
  (matches the existing `Reschedule`/`UpdateProgress`/`AddDependency` convention of calling
  methods directly on a `GanttTask` instance, not proxied through `Project`). Migration:
  `AddGanttTaskAssignedUser`.
- **Minimal `GanttTask` CRUD**: `POST`/`GET /api/projects/{projectId}/tasks`, `PATCH
  /api/projects/{projectId}/tasks/{taskId}/assignee`. Same cross-org scoping as elsewhere, no
  extra auth policy (matches `POST /api/projects`).
  **EF bug fixed**: adding a task to a *reloaded* `Project` (vs. one constructed+saved in the same
  call) threw `DbUpdateConcurrencyException`. Cause: `GanttTask.Id` is a non-default `Guid` at
  construction time, so when EF's change tracker discovers the new task only via a mutated
  owned collection (never an explicit `Add()`), it assumes "already exists" and issues a failing
  `UPDATE` instead of `INSERT`. Fixed via `IProjectRepository.TrackNewTask()` (explicit
  `_context.Entry(task).State = EntityState.Added`), called right after `Project.AddTask(...)`.
  Regression test: `ProjectRepositoryTests.AddTask_ToReloadedProject_ThenSaveChanges_Persists`.
  **Still open**: same bug class unverified for `TaskDependency` (also `OwnsMany`'d, one level
  deeper) — no command adds a dependency to an already-persisted task yet.
- **Kanban bounded context**: `Board` (`OrganizationId`, `Name`, `ProjectId` nullable — a board
  doesn't need a linked Gantt project) → `KanbanColumn` (`Name`, `Order`) → `KanbanCard` (`Title`,
  `AssignedUserId` nullable, `GanttTaskId` nullable — an unvalidated cross-reference, deliberately
  not existence-checked, same precedent as `GanttTask.ParentTaskId`: no repository method exists
  to look up a task across an org's projects). Full slice: `IBoardRepository`, `Create{Board,
  Column,Card}Command`, `AssignKanbanCardUserCommand`, `GetBoardsByOrganizationQuery` (flat
  summary) + `GetBoardByIdQuery` (full detail with nested columns/cards). Endpoints under
  `/api/boards` (see README's "Kanban boards" section). Same cross-org scoping/no-extra-policy
  as Project/GanttTask.
  Proactively fixed the `TrackNewTask`-class bug two levels deep before it could bite:
  `BoardRepository.TrackNewColumn`/`TrackNewCard`, with regression tests for both (add column to
  a reloaded `Board`; add card to a column of a reloaded `Board`). Migration: `AddKanbanBoards`.
  No frontend yet.
- **Resource-availability query**: `GET /api/resources/availability` (query params `departmentId?`,
  `projectId?`, `from`, `to`) → one `ResourceAvailabilityDto` per in-scope user, each with
  `ApprovedLeave`/`AssignedTasks`/`AssignedCards` overlapping `[from, to]`. No new aggregate — a
  pure cross-cutting read living in `Chronos.Application.Resources`
  (`GetResourceAvailabilityQuery`/Handler/Validator), the first query handler in this codebase to
  read across four aggregate roots (`User`, `Department`, `Project`→`GanttTask`, `Board`→
  `KanbanColumn`→`KanbanCard`, `LeaveRequest`) rather than just one. Kept to the codebase's
  "Application never touches `ChronosDbContext` directly" rule by injecting all five existing
  repositories and joining/filtering by id in memory (no `IApplicationDbContext`-style escape
  hatch introduced).
  - `departmentId` is inclusive of descendants: BFS over `IDepartmentRepository.GetByOrganizationAsync`'s
    flat list in memory, per the epic's settled design decision — no recursive SQL.
  - `projectId`, if given, scopes which tasks (that project's `Tasks` only) and boards
    (`Board.ProjectId == projectId` only) are considered; it never scopes the `LeaveRequest` side,
    since leave isn't project-related. Both filters independently 400 with `"X 'id' was not
    found."` if the id doesn't resolve within the caller's org (same message/status convention as
    `CreateBoardCommandHandler`'s `ProjectId` check) — cheaply, by checking membership in the
    already org-scoped list rather than an extra repository round-trip.
  - `LeaveRequest`/`GanttTask` are range-filtered (`start <= to && end >= from`); `KanbanCard` has
    no schedule of its own so it's included whenever assigned, unfiltered by date.
  - New repository methods, all added because the existing `GetByOrganizationAsync` on each either
    didn't exist (`IUserRepository`) or didn't eager-load owned collections needed here
    (`IProjectRepository`/`IBoardRepository`'s flat `GetByOrganizationAsync` is unchanged, used
    elsewhere for list-summary queries that don't need `Tasks`/`Columns`): `IUserRepository.
    GetByOrganizationAsync`, `IProjectRepository.GetDetailedByOrganizationAsync` (`.Include(p =>
    p.Tasks)`), `IBoardRepository.GetDetailedByOrganizationAsync` (`.Include(b =>
    b.Columns).ThenInclude(c => c.Cards)`). No new migration — no schema change.
  - No extra auth policy beyond `RequireAuthorization()` — any authenticated org member can query
    availability, matching `GET /api/boards`/`GET /api/departments`.
  - First query-side `IValidator` in the codebase (`GetResourceAvailabilityQueryValidator`: `To >=
    From`) — every other validator so far was command-only; nothing in `ValidationBehavior`
    actually required that, it just hadn't come up yet.
  - No frontend yet (see Planned section below).
- Last verified (2026-09-28): backend 164/164 non-container tests (Domain 37, Application 64,
  Infrastructure 16, Api 47), frontend 18/18. SqlServer/PostgreSql Testcontainers legs and `ng
  build`/`ng serve` (needs Node ≥22.22) can't run locally in this environment — CI is the real
  verification for both, not yet run against this uncommitted change.

### Gotchas learned the hard way (still true, worth not re-discovering)

- `AddValidatorsFromAssembly` needs `using FluentValidation;` — the package reference alone doesn't
  bring it in.
- `IWebHostBuilder.UseEnvironment` — the working overload is in `Microsoft.AspNetCore.Hosting`;
  the same-named one in `Microsoft.Extensions.Hosting` (for `IHostBuilder`) compiles but resolves
  wrong.
- `Chronos.Api.Tests` needs `IsolatedTestFactory.WithIsolatedSqlite()`, not config overrides —
  `AddChronosInfrastructure` reads the connection string eagerly, before test-host config splicing
  happens, so a fresh temp-file SQLite db per factory must be injected via `ConfigureServices`.
- Angular signal inputs don't re-evaluate on a second `detectChanges()` from a mutated host
  property in tests (angular/angular#56863) — drive inputs via `fixture.componentRef.setInput(...)`
  instead of a host-template rebind.
- Enum request bodies need `JsonStringEnumConverter` registered explicitly (`ConfigureHttpJsonOptions`)
  or `"Vacation"` 400s before reaching a handler.
- EF owned collections (`OwnsMany`), any depth: adding to one via mutation of an already-tracked
  parent needs an explicit `_context.Entry(x).State = EntityState.Added` (`TrackNew*` repository
  methods) — see the `GanttTask`/Kanban bug above. Check for this pattern whenever a new
  owned-collection-add code path is written.

## Known issues (still open)

- Cross-org Admin visibility: every query (leave requests, role-promotion) is scoped to the
  caller's own org, by design so far. No cross-org or cross-department aggregate view exists yet.
- Org/user self-registration is fully open — anyone can `POST /api/organizations` or register into
  any org with no invite/verification step. Fine for local dev, not for real deployment.
- Stale "validators aren't wired" comments in a few command handlers (see Done, harmless).

## Not started

- Refresh-token flow on the frontend (expired JWT = logged out, no silent renewal).
- Invite/verification gating for org + user self-registration.
- E2E in CI — needs API + DB running alongside the dev server; deliberately deferred rather than
  built speculatively. Revisit if E2E starts getting skipped in practice.
- Resource-availability epic beyond `Department`/`GanttTask.AssignedUserId`/Kanban/the availability
  query (see below) — all frontend for this epic (including Kanban board UI).

## Planned: cross-department resource availability (shared vacation calendar × Gantt × Kanban)

Settled with the user 2026-09-27: shared vacation calendar across departments, cross-referenced
against Gantt and Kanban, so availability reads against time/project/department at once.
Domain pieces (`Department`, `GanttTask.AssignedUserId`, Kanban) and the availability query are
done — see Done above. Only the frontend for this epic is still unbuilt.

**Settled design decisions** (supersede any earlier open questions):
- Departments are **nested** (`ParentDepartmentId`) — "department Y" in queries means Y **plus
  descendants** by default. Not resolved anywhere yet — do it in the availability query by loading
  `GetDepartmentsByOrganizationAsync` and walking the tree in memory (org department counts are
  small); don't reach for recursive SQL.
- **At most one department per user** (`User.DepartmentId`, nullable 1:1, no matrix membership).
- **Kanban boards may stand alone** (`Board.ProjectId` optional).
- **Shared calendar shows approved leave only** — pending stays private until approved.

**Proposed shape, remaining piece:**
- Frontend: cross-department calendar page (filterable by department/project, rendering
  `GET /api/resources/availability`) + Kanban board component, cross-linked with the existing
  `GanttChartComponent`. No frontend exists yet for departments or Kanban either (no
  picker/management/board UI) — needed before the calendar page is usable. **Next up.**

## Suggested next steps, in order

1. ~~Reconfirm CI is green on all four jobs for `b922aa7`~~ — **done 2026-09-28**, confirmed via
   GitHub check-runs API: Backend (Sqlite/SqlServer/PostgreSql) + Frontend (Jest) all green.
2. ~~The resource-availability query~~ — **done 2026-09-28** (see Done above), not yet committed/
   pushed/CI-verified. Commit it, push, and reconfirm CI green (first pass exercising the new
   cross-aggregate query against real SqlServer/PostgreSql providers, not just Sqlite).
3. Resource-availability epic frontend: department/task/board picker UI (none exists for
   departments or Kanban yet) + the cross-department calendar page rendering
   `GET /api/resources/availability` + a Kanban board component, cross-linked with the existing
   `GanttChartComponent`. The one piece standing between this epic and being user-usable.
4. Cross-org Admin visibility for leave requests — same shape of gap as cross-department
   visibility above, worth solving once rather than building two "who can see whose stuff"
   mechanisms.
5. Refresh-token flow on the frontend.
6. Decide on invite/verification gating for org + user self-registration before this goes beyond
   local dev.
