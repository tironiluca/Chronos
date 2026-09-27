# Chronos — Implementation Plan

Status as of commit `885f0a1` plus the Kanban bounded context below (uncommitted). Working
reference for picking the project back up — not a spec. History has been compressed into
gotchas/decisions worth keeping; full narrative for older passes is in git history
(`git log --oneline`) if ever needed.

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
- **Auth**: JWT bearer, PBKDF2-SHA256 hashing (BCL only), `User` with roles
  (Employee/Approver/Admin). All endpoints derive org/requester/approver identity from JWT claims,
  never the request body. `Jwt:SigningKey` must come from env var/secret manager outside
  `Development` — `Program.cs` throws a clear startup error if missing.
- **Org + user self-registration**: `POST /api/organizations` (creates an org) and
  `POST /api/auth/register` (registers a user into one), plus frontend flow
  (`RegisterOrganizationComponent`/`RegisterComponent`). Both endpoints are intentionally open,
  ungated — see Known issues.
- **Admin role-promotion**: `PATCH /api/users/{id}/role` (`AdminOnly`), scoped to caller's own org
  (cross-org target = same not-found error, doesn't leak existence).
- **Rights + password rotation**: `Right`/`RoleRight` tables mapping `UserRole` → permission codes
  (additive, `User.Role`/JWT claims untouched), exposed at `GET /api/users/me/rights`.
  `PasswordHistory` + `POST /api/auth/change-password` blocks reuse of last 5 passwords.
- **Real EF Core migrations** (replacing `EnsureCreated()`): three thin `ChronosDbContext`
  subclasses, one per provider (`Sqlite`/`SqlServer`/`PostgreSql`ChronosDbContext), each with its
  own migrations under `Persistence/Migrations/<Provider>/` since migrations are dialect-specific.
  `AddChronosInfrastructure` registers whichever matches config. Adding a migration going forward
  means regenerating it for all three providers — see README's "Migrations" section.
- **FluentValidation is actually wired into MediatR** (`ValidationBehavior<TRequest, TResponse>`,
  registered via `cfg.AddOpenBehavior`). Before this it was dead code — validators were DI-
  registered but nothing ever invoked them (see [[project_validation_pipeline_gap]] memory, now
  stale/fixed). Constrained to `where TResponse : Result`; short-circuits with a typed `Failure`
  before the handler runs. Pre-existing handler-level guard clauses (`RegisterOrganizationCommandHandler`,
  `ChangePasswordCommandHandler`, etc.) are now redundant defense-in-depth, left in place on
  purpose. Their code comments still claim "validators aren't wired yet" — stale, harmless, fix
  opportunistically when touching those files.
- **CI**: `.github/workflows/ci.yml`, backend matrixed over 3 DB providers (SqlServer/PostgreSql
  legs use Testcontainers), plus frontend Jest job. `npm ci` (lockfile committed).
- **`Department` entity + first resource-availability endpoints**: `Department`
  (`OrganizationId`, `Name`, `Code`, `ParentDepartmentId` nullable, self-referencing) plus
  `User.DepartmentId` (nullable — registration doesn't assign one yet). Full vertical slice:
  `IDepartmentRepository`/`DepartmentRepository`, `CreateDepartmentCommand` + validator + handler,
  `GetDepartmentsByOrganizationQuery` + handler, `AssignUserDepartmentCommand` + validator +
  handler. Endpoints: `POST`/`GET /api/departments`, `PATCH /api/users/{id}/department` (see
  README's Auth section). `ParentDepartmentId` and `User.DepartmentId` are plain scalars, not EF
  relationships — matches this codebase's existing convention of referencing other
  aggregates/records by id only (e.g. `User.OrganizationId`, `RoleRight.RightId`), so no FK
  constraints/cascades to reason about. Migrations regenerated for all three providers
  (`AddDepartments`).
- **`GanttTask.AssignedUserId`** (nullable): `AssignUser(Guid? userId)` method on `GanttTask`
  itself (not a `Project`-level wrapper — matches the existing convention where `Reschedule`/
  `UpdateProgress`/`AddDependency` are already called directly on a `GanttTask` instance obtained
  from `Project.Tasks`/`AddTask`, not proxied through `Project`). No EF configuration change needed
  — `GanttTask` is `OwnsMany`'d (see `ProjectConfiguration`), so a plain scalar property is picked
  up by convention. Migrations regenerated for all three providers (`AddGanttTaskAssignedUser`).
- **Minimal `GanttTask` CRUD** (decided with the user, since none existed at all before this pass —
  `CreateProjectCommand` used to be the *only* Project-related command/endpoint): `POST`/
  `GET /api/projects/{projectId}/tasks` (`CreateGanttTaskCommand`/`GetTasksByProjectQuery`) and
  `PATCH /api/projects/{projectId}/tasks/{taskId}/assignee` (`AssignGanttTaskUserCommand`, body
  `{ "userId": <guid-or-null> }`). Same cross-org scoping as everywhere else (project must belong
  to caller's org; an assignee, if given, must too). No extra authorization policy beyond
  `RequireAuthorization()` — matches `POST /api/projects`, which also has no role restriction today.
  **Real bug found and fixed along the way**: adding a task to a `Project` *reloaded* from the DB
  (as opposed to one just constructed in the same save) threw `DbUpdateConcurrencyException`
  ("expected to affect 1 row(s), but actually affected 0"). Root cause: `GanttTask.Id` is already
  a non-default `Guid` by the time EF's change tracker discovers it (`Entity.Id = Guid.NewGuid()`
  runs at construction, not on save); when the new task is reached only via mutating an
  already-tracked (`Unchanged`) `Project`'s owned collection — never through an explicit `Add()`
  — EF's default heuristic for graph-discovered entities assumes a non-default key means "already
  exists" and marks it `Modified` instead of `Added`, so it issues a failing `UPDATE` instead of an
  `INSERT`. This was never caught before because no test had ever reloaded a `Project` and then
  added a task to it — the one pre-existing test created and saved both in the same call. Fixed
  with an explicit `IProjectRepository.TrackNewTask(GanttTask)` (`_context.Entry(task).State =
  EntityState.Added`), called right after `Project.AddTask(...)` in the handler. Regression test:
  `ProjectRepositoryTests.AddTask_ToReloadedProject_ThenSaveChanges_Persists`. **Worth checking
  whether the same class of bug applies to `TaskDependency`** (also `OwnsMany`'d, one level deeper)
  if/when a command ever adds a dependency to an already-persisted task — not hit yet since no such
  command exists.
- **Kanban bounded context** (resource-availability epic, next domain slice after Department/
  GanttTask): `Board` aggregate (`OrganizationId`, `Name`, `ProjectId` nullable/optional — a board
  doesn't need a linked Gantt project) owning `KanbanColumn` (`Name`, `Order`) owning `KanbanCard`
  (`Title`, `AssignedUserId` nullable — same `AssignUser(Guid?)` treatment as
  `GanttTask.AssignedUserId` — and `GanttTaskId` nullable, an unvalidated plain cross-reference
  like `GanttTask.ParentTaskId`, deliberately **not** existence-checked since there's no repository
  method to look up a task by id across an org's projects and the codebase's precedent for this
  exact kind of optional same-level reference is to leave it unvalidated). Two owned levels deep,
  same `OwnsMany` pattern as `Project`→`GanttTask`→`TaskDependency` (see `BoardConfiguration`).
  Full vertical slice: `IBoardRepository`/`BoardRepository`, `CreateBoardCommand` (validates
  `ProjectId` cross-org like `CreateDepartmentCommand` validates `ParentDepartmentId`),
  `CreateKanbanColumnCommand`, `CreateKanbanCardCommand`, `AssignKanbanCardUserCommand`,
  `GetBoardsByOrganizationQuery` (flat summary list) + `GetBoardByIdQuery` (full detail with
  nested columns/cards — the shape a Kanban view actually needs). Endpoints: `POST`/
  `GET /api/boards`, `GET /api/boards/{id}`, `POST /api/boards/{id}/columns`, `POST
  /api/boards/{id}/columns/{columnId}/cards`, `PATCH
  /api/boards/{id}/columns/{columnId}/cards/{cardId}/assignee` (see README's "Kanban boards"
  section). Same cross-org scoping and no-extra-policy treatment as Project/GanttTask throughout.
  **Proactively addressed the `TrackNewTask`-class EF bug two levels deep**: `IMPLEMENTATION_PLAN.md`
  had flagged (previous entry above) that adding to an owned collection nested inside another
  owned collection of an already-persisted aggregate was untested for `TaskDependency` — built
  `BoardRepository.TrackNewColumn`/`TrackNewCard` (same `_context.Entry(x).State =
  EntityState.Added` fix) from the start here, with regression tests for both: adding a column to
  a reloaded `Board`, and adding a card to a column of a reloaded `Board`
  (`BoardRepositoryTests.AddColumn_ToReloadedBoard_ThenSaveChanges_Persists`/
  `AddCard_ToColumnOfReloadedBoard_ThenSaveChanges_Persists`). Migrations regenerated for all three
  providers (`AddKanbanBoards`). No frontend yet (not in scope for this pass — see Not started).
- Last verified: backend **147/147** non-container tests (Domain 37, Application 57, Infrastructure
  13, Api 40), frontend 16/16 (unchanged, no frontend work this pass). SqlServer/PostgreSql
  Testcontainers legs and `ng build`/`ng serve` (needs Node ≥22.22) can't run locally in this
  environment — CI is the real verification for both. CI was confirmed green on all four jobs for
  `885f0a1` (the commit immediately before this Kanban work, checked via the GitHub API on
  2026-09-27) — the Kanban commit(s) on top of it haven't been pushed/reconfirmed yet.

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
- Resource-availability epic beyond `Department`/`User.DepartmentId`/`GanttTask.AssignedUserId`/
  Kanban above (see next section) — the availability query, and all frontend for this epic
  (including a Kanban board UI/component, not just the backend slice).

## Planned: cross-department resource availability (shared vacation calendar × Gantt × Kanban)

New requirement (settled with the user 2026-09-27): shared vacation calendar across departments of
a company, cross-referenced against Gantt and a new Kanban tool, so availability reads against
time/project/department at once. `Department`/`User.DepartmentId`, `GanttTask.AssignedUserId`, and
now the Kanban bounded context are built (see Done); the availability query and all frontend for
this epic are still a design sketch, unbuilt.

**Settled design decisions** (supersede any earlier open questions):
- Departments are **nested** (self-referencing `ParentDepartmentId`) — "department Y" in queries
  means Y **plus its descendants** by default. Resolving descendants isn't built yet — do it in the
  availability query (below) by loading `GetDepartmentsByOrganizationAsync` and walking the tree in
  memory (department counts per org are small); don't reach for recursive SQL.
- **At most one department per user** (`User.DepartmentId`, nullable 1:1, no matrix membership) —
  nullable because registration doesn't assign one yet, not a deviation from "exactly one" as a
  target end-state.
- **Kanban boards may stand alone** (`Board.ProjectId` optional) — department boards don't require
  a linked Gantt project.
- **Shared calendar shows approved leave only** — pending requests stay private until approved,
  same as today.

**Proposed shape:**
- Domain: `Department` + `User.DepartmentId` — **done** (see Done above).
- Domain + minimal CRUD: `GanttTask.AssignedUserId` — **done** (see Done above).
- Domain + minimal CRUD: Kanban bounded context — `Board`/`KanbanColumn`/`KanbanCard` — **done**
  (see Done above).
- Application: `GET /api/resources/availability` (optional `departmentId` inclusive-of-descendants
  / `projectId` + date range) → per-user approved leave + assigned Gantt tasks + assigned Kanban
  cards overlapping the range. The one query every other view in this epic renders against.
  **Next up.**
- Frontend: cross-department calendar page (filterable by department/project) + new Kanban board
  component, cross-linked with the existing `GanttChartComponent`. No frontend exists yet for
  departments or Kanban either (no picker/management/board UI) — needed before the calendar page
  is usable.

## Suggested next steps, in order

1. Confirm CI is green on all four jobs once the Kanban commit(s) are pushed — this is the first
   pass to exercise a two-owned-levels-deep `OwnsMany` (`Board`→`KanbanColumn`→`KanbanCard`)
   against real SqlServer/PostgreSql providers, not just Sqlite.
2. Continue the resource-availability epic: the availability query next
   (`GET /api/resources/availability`, inclusive-of-descendants department resolution, see design
   decisions above) — Department, GanttTask.AssignedUserId, and Kanban are all built, so this is
   the one piece standing between the backend and a usable frontend. Frontend department/task/
   board picker UI and the calendar page follow once the availability query exists.
3. Cross-org Admin visibility for leave requests — same shape of gap as cross-department
   visibility above, worth solving once rather than building two "who can see whose stuff"
   mechanisms.
4. Refresh-token flow on the frontend.
5. Decide on invite/verification gating for org + user self-registration before this goes beyond
   local dev.
