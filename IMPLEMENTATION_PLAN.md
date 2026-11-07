# Chronos — Implementation Plan

Status as of 2026-11-07: resource-availability epic **complete**, plus step 3 (task edit
commands + card moves, backend + UI) on branch `ccr-85238cc1-tu6me9`. CI (`ci.yml`) only triggers on push/PR to `main`, so feature-branch pushes
don't run it; provider coverage for this branch was confirmed locally instead (see Done). Working reference for picking
the project back up — not a spec. Compressed to gotchas/decisions worth keeping; full narrative is
in git history (`git log --oneline`) if ever needed.

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
- **`GET /api/users?email=<email>`** (`AdminOnly`, scoped to caller's org): resolves an email to
  `UserDto` (`Id`/`Email`/`DisplayName`/`Role`/`DepartmentId`) — the id role-promotion/department
  assignment need, without direct DB access. Same cross-org not-found shape as everywhere else.
  `GetUserByEmailQuery`/Handler/Validator in `Chronos.Application.Users.Queries.GetUserByEmail`.
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
  - Frontend: see "Resource-availability epic frontend" below.
- **`GET /api/projects` + `GET /api/projects/{id}`**: `GetProjectsByOrganizationQuery` (reuses
  `GetDetailedByOrganizationAsync`, ordered by name) and `GetProjectByIdQuery`, both returning
  `ProjectDto` with tasks inlined. `GanttTaskDto` gained `Dependencies` (predecessor ids) and a
  shared `GanttTaskDto.From(GanttTask)` mapper (the only other construction site,
  `GetTasksByProjectQueryHandler`, now uses it). Not-found and cross-org return the same 400 +
  message, as everywhere else. No new repository method, no migration.
- **User directory + assignee pickers**: `GET /api/users/directory` (any org member;
  `UserDirectoryEntryDto` = `Id`/`DisplayName`/`DepartmentId` only — no email/role, so the
  `AdminOnly` email lookup stays the only way to see those). Frontend `UserPickerComponent` is
  presentational (parent fetches the directory once, so N cards/tasks = 1 request). Kanban cards
  assign through it (the email-lookup flow and the now-unused `UserService.getByEmail` client
  method are gone); the Gantt page gained a task list with an assignee picker per task
  (`PATCH .../tasks/{taskId}/assignee`). Verified live as a plain Employee.
- **Edit commands + UI (step 3)**: `PATCH .../tasks/{taskId}/schedule`, `PATCH .../progress`,
  `POST`/`DELETE .../tasks/{taskId}/dependencies[/{predecessorTaskId}]`, `PATCH
  /api/boards/{id}/columns/{columnId}/cards/{cardId}/column` (README has the shapes).
  - Domain throws, handlers map `InvalidOperationException`/`ArgumentException` to `Failure` (the
    Leave handlers' existing pattern), so invariants live in one place.
  - `Project.AddTaskDependency` owns the graph-level invariants (predecessor in the same project, no
    cycles, DFS over predecessor edges); `GanttTask.AddDependency` owns the task-local ones
    (self-reference, duplicate edge). This breaks the earlier "call methods directly on `GanttTask`"
    convention on purpose: a cycle check needs the whole task graph. A dependency is identified by
    its predecessor (at most one edge per pair), so it needs no id in URLs.
  - `Board.MoveCard(cardId, from, to)` is the only way to move a card (`KanbanColumn.Detach/
    AttachCard` and `KanbanCard.MoveToColumn` are `internal`); a target column on another board is
    just "not found", so cards can't leak across boards or orgs.
  - **Resolved the open `TaskDependency` concern**: adding a dependency to a reloaded project
    persists **without** a `TrackNew*` call. Its key is an EF shadow `Guid` left at default until
    save, so EF classifies it `Added` itself; the `TrackNew*` bug only affects entities whose `Id`
    is set in the constructor. Re-parenting an owned `KanbanCard` keeps its identity. Both are
    covered by infra tests that assert through a **fresh `DbContext`**. The older tests reuse one
    context, where identity resolution can mask a write that never reached the DB.
  - Frontend: `GanttTaskEditorComponent` (presentational, `linkedSignal` drafts re-sync after
    reload); chart drag/resize/progress via new `GanttChangeHandlers` on `IGanttRenderer`; Kanban
    "Move to…" select + native HTML5 drag-and-drop.
- **Resource-availability epic frontend** (Angular, all behind `authGuard`):
  - `/departments`: `DepartmentPickerComponent` (flattened tree) + Admin create form.
  - `/boards`: board list/create (optional linked project via `ProjectPickerComponent`) + board
    detail (columns/cards, assign by email via `GET /api/users?email=`). `?boardId=` preselects.
  - `/availability`: employee × day leave grid (`LeaveGridComponent`), month pagination, department
    + project filters. The project filter can't narrow leave server-side (by design), so the page
    narrows **rows** to people with `AssignedTasks`/`AssignedCards` in that project.
  - `/projects`: project list/create → `/projects/:projectId/gantt`
    (`ProjectGanttPageComponent`: loads via `GET /api/projects/{id}`, add task, renders
    `GanttChartComponent`, lists linked boards).
  - `/project-load`: project × ISO-week grid (`ProjectLoadGridComponent`), quarter pagination,
    cell = count of tasks overlapping the week, project-subset chip filter. Week math in
    `features/projects/iso-week.ts` (ISO-8601 week-year aware — 2026 has a W53). One fetch of
    `GET /api/projects`; quarter navigation is client-side re-bucketing, no refetch.
  - Kanban ↔ Gantt cross-links: a project-linked board links to its Gantt page, offers the
    project's tasks when creating a card (`GanttTaskId`), and shows the linked task on the card;
    the Gantt page lists its boards with `/boards?boardId=` deep links.
  - `withComponentInputBinding()` is on — route params/query params bind to routed components'
    inputs (`projectId`, `boardId`).
  - Spec fakes for `ProjectService` live in `src/app/testing/` (excluded from `tsconfig.app.json`).
- Last verified (2026-11-07, step 3, locally): backend 229/229 non-container tests (Domain 51,
  Application 93, Infrastructure 19, Api 66), frontend 99/99 Jest, `ng build` clean, plus a
  Playwright smoke run against the real API (Sqlite) + `ng serve`: reschedule/progress/add
  dependency via the editor → Frappe draws the arrow → drag a bar one week (it and its dependent
  persist, duration kept) → remove dependency → card moved via select and via drag-and-drop; zero
  console errors. Container suites not re-run for this pass (no schema change).
- Previously verified (2026-11-06, locally): backend 184/184 non-container tests (Domain 37,
  Application 75, Infrastructure 16, Api 56), frontend 73/73 Jest, `ng build` clean, plus a
  Playwright smoke run against the real API (Sqlite) + `ng serve`: projects → board linked to a
  project → card linked to a task → Gantt page renders and adds a task → back to the board via
  `?boardId=` → project-load grid + chip filter → availability project filter; zero console errors.
  Container suites run locally too (`dockerd` is installed in the cloud sandbox, just not started —
  `nohup dockerd &` first): `SqlServerRepositoryTests` 3/3, `PostgreSqlRepositoryTests` 3/3, same
  filters as CI's provider jobs. Docker Hub may 429 the `postgres:15.1` pull — retry `docker pull`.
- Previously verified (2026-09-28): backend 167/167 non-container tests (Domain 37, Application 67,
  Infrastructure 16, Api 50 — the +3/+3 over the availability-query counts is the `GET
  /api/users?email=` lookup endpoint above), frontend 18/18 locally, **plus CI green on all four
  jobs for `78169b2`** (Backend Sqlite/SqlServer/PostgreSql + Frontend Jest, confirmed via GitHub
  check-runs API) — first real proof the cross-aggregate availability query works against real
  SqlServer/PostgreSql, not just Sqlite.

- **Deployment (2026-11-07)**: Vercel project `chronos` (team "Luca's projects", framework
  Services, git-linked to this repo) + Supabase project `chronos` (`dyrpcketvphbkyycxezu`,
  eu-central-1, free plan). README's "Deployment" section has the layout and the env var table.
  - Supabase: schema applied from the PostgreSql EF idempotent script (all 5 migrations recorded
    in `__EFMigrationsHistory`), RLS deny-all on every table, API role `chronos_api` owns the
    tables. Its password was deliberately reset to an unknown random value, so the owner must
    set it (`ALTER ROLE chronos_api PASSWORD ...`) before the API can connect.
  - Vercel: all three env vars are set (`DatabaseProvider` plain; `ConnectionStrings__PostgreSql`
    and `Jwt__SigningKey` sensitive). On the owner's instruction the agent generated the secrets,
    so they appear in that session's transcript; rotate them as described in the README if that
    matters. Live preview: `/api/health` returns `{"status":"ok","database":"ok"}` from `fra1`, so
    the container starts and the startup migration check runs as `chronos_api` through the
    **`aws-1`** session pooler. `aws-0` returned "tenant/user not found".
  - Not run live: the browser smoke. This sandbox's network policy refuses `*.vercel.app` and the
    Vercel fetch tool is GET-only, so register/login/edit flows were covered by CI's PostgreSql job
    plus a privilege check on `chronos_api`, not by a request against the deployment.
  - Production = the project's production branch (`main`). Branch pushes create previews,
    which sit behind Vercel Authentication.

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
- **frappe-gantt 0.6 cannot be `import`ed.** `dist/frappe-gantt.js` is a browser IIFE with no
  export: `import Gantt from ...` errors under `ng serve` (Vite: "does not provide an export named
  'default'") and silently bundles an empty module in `ng build`. Its ES source imports `.scss`,
  which the Angular builder won't load from JS. Fixed by loading the dist script/CSS via
  `angular.json` `scripts`/`styles` and reading `globalThis.Gantt` in `FrappeGanttRenderer`. Until
  this pass the Gantt page had never rendered — no UI linked to it, the route pointed at
  `GanttChartComponent` directly (required `tasks` input never set), and `GET /api/projects/{id}`
  didn't exist.
- `<select [value]="...">` doesn't preselect when the options come from `@for`: the value binding
  is applied before the options exist. Use `[selected]` per `<option>` (all three pickers now do).
- httpResource fakes in specs must be **lazy** (`computed`, not `signal(id())`): reading a
  required input inside the id source at construction throws NG0950, which the real httpResource
  never does.
- `.toolbar` only got flex layout on `<form>` (global `form {display:flex}`) — now flex on any
  element.
- EF owned collections (`OwnsMany`), any depth: adding to one via mutation of an already-tracked
  parent needs an explicit `_context.Entry(x).State = EntityState.Added` (`TrackNew*` repository
  methods) — see the `GanttTask`/Kanban bug above. Check for this pattern whenever a new
  owned-collection-add code path is written.

- frappe-gantt 0.6 dates: a date-only `end` is exclusive internally (end + 24h); `on_date_change`
  reports (exclusive end − 1 s), and dragged dates are fractional-hour offsets from the chart origin,
  which drift across DST. `FrappeGanttRenderer` rounds both to the nearest local midnight.
- Playwright vs this UI: the sticky top nav covers elements that `scrollIntoViewIfNeeded` scrolled
  to the top edge (scroll to top before pointer drags), and a Kanban card's centre is one of its
  `<select>`s, so drag it by `.kanban-card-title`. Boards start with three default columns.

- Vercel container services build with **the Dockerfile's own directory** as context, not the
  service `root`, hence `src/Dockerfile`. Locally, `docker build` in this sandbox needs the agent
  proxy CA added to the SDK stage (a throwaway Dockerfile variant, never committed). Vercel's
  builders don't need it.
- Supabase exposes `public` via PostgREST: any table created there without RLS is readable with
  the anon key. New EF migrations that add tables must be followed by `ALTER TABLE ... ENABLE ROW
  LEVEL SECURITY` (and owned by `chronos_api`). The advisor flags misses as `rls_disabled_in_public`.

## Known issues (still open)

- Cross-org Admin visibility: every query (leave requests, role-promotion) is scoped to the
  caller's own org, by design so far. No cross-org or cross-department aggregate view exists yet.
- Org/user self-registration is fully open — anyone can `POST /api/organizations` or register into
  any org with no invite/verification step. Fine for local dev, not for real deployment.
- Stale "validators aren't wired" comments in a few command handlers (see Done, harmless).
- Dependencies are informational: rescheduling doesn't enforce them, and `GanttTaskDto` exposes only
  predecessor ids (no type/lag), so the UI can't display a dependency's type yet.
- Kanban cards have no in-column order (moves append); no reordering within a column.
- `jest.config.ts` still has a `frappe-gantt` entry in `transformIgnorePatterns`; dead since the
  global-script change, harmless.

## Not started

- Refresh-token flow on the frontend (expired JWT = logged out, no silent renewal).
- Invite/verification gating for org + user self-registration.
- E2E in CI — needs API + DB running alongside the dev server; deliberately deferred rather than
  built speculatively. Revisit if E2E starts getting skipped in practice.
- Renaming/deleting tasks, cards, columns and boards (create/edit/move only so far).

## Done epic: cross-department resource availability (shared vacation calendar × Gantt × Kanban)

Settled with the user 2026-09-27; **complete as of 2026-11-06** (backend + frontend — see Done).
Design decisions kept here because they still constrain future changes:
- Departments are **nested** (`ParentDepartmentId`) — "department Y" means Y **plus descendants**,
  walked in memory from the flat list; no recursive SQL.
- **At most one department per user** (`User.DepartmentId`, nullable 1:1, no matrix membership).
- **Kanban boards may stand alone** (`Board.ProjectId` optional).
- **Shared calendar shows approved leave only** — pending stays private until approved.
- Grids bucket dates **client-side** from raw ranges (leave → days, tasks → ISO weeks); no
  server-side date bucketing. Both grids paginate (month / quarter), never infinite scroll.
- The project × week grid has no department filter (projects aren't department-scoped).

## Suggested next steps, in order

1. ~~Confirm provider coverage for this pass~~ — **done 2026-11-06** locally (container suites,
   see Done). Real CI runs once the branch goes to `main` via PR.
2. ~~Org-wide user directory + assignee pickers~~ — **done 2026-11-06** (see Done).
3. ~~Edit commands + UI~~ — **done 2026-11-07** (see Done).
4. Cross-org Admin visibility for leave requests — worth solving once rather than building two
   "who can see whose stuff" mechanisms.
5. Refresh-token flow on the frontend.
6. Decide on invite/verification gating for org + user self-registration before this goes beyond
   local dev. **Now more pressing**: the app is deployed (behind Vercel Authentication for now).
   Making it public without gating lets anyone create orgs/users.
7. E2E in CI — the Playwright smoke flow from this pass is a ready-made first scenario.
