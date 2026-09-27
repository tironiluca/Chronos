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
