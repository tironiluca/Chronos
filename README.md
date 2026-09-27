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
  "DatabaseProvider": "SqlServer" // or "Sqlite" / "PostgreSql"
}
```

Each provider has a matching entry under `ConnectionStrings`. `Chronos.Infrastructure.DependencyInjection`
is the single place that branches on this value (`AddChronosInfrastructure`) — see that file before
adding anything provider-specific elsewhere. Avoid provider-specific SQL (raw SQL, date functions) in
queries so LINQ stays portable across all three; if something is genuinely unavoidable, isolate it
behind an interface in Application rather than branching inline.

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
into an existing `OrganizationId` as an `Employee` (the only role self-registration can create —
promoting to `Approver`/`Admin` is an admin-only operation, **not implemented yet**). Passwords
are hashed with PBKDF2-SHA256 (`Pbkdf2PasswordHasher`, BCL only, no extra dependency).

The token carries `NameIdentifier` (user id), `Role`, and a custom `org` claim (organization id).
`Chronos.Api.Security.ClaimsPrincipalExtensions` reads these back out. Every endpoint derives
`OrganizationId`/requester/approver identity from the token, never from the request body — a
client cannot act as another user or write into another organization by supplying a different id
in the payload. `/approve` and `/reject` additionally require the `ApproverOrAdmin` policy;
`/cancel` allows the original requester or an Admin (checked in `CancelLeaveRequestCommandHandler`,
not just at the endpoint).

**Known gaps**, in order of what to fix before real use: the `SigningKey` in `appsettings.json`
is a placeholder and must move to user-secrets/environment/Key Vault before this is anything but
local dev; there's no refresh-token flow (the frontend just treats an expired token as logged
out); registration accepts any `OrganizationId` from the caller with no invite/verification step;
and an Admin only ever sees their own organization's leave requests (no cross-org view yet).

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
dotnet test
cd frontend/chronos-web && npm test
```

E2E:
```
cd e2e/chronos-e2e
npm install
npx playwright test
```
