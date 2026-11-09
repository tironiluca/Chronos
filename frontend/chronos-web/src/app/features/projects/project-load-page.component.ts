import { IconComponent } from '../../shared/ui/icon.component';
import { Component, computed, inject, signal } from '@angular/core';
import { ProjectService } from '../../core/api/project.service';
import { ProjectLoadGridComponent } from './project-load-grid.component';
import { isoDate, quarterEnd, quarterLabel, quarterStart } from './iso-week';

// Routed at /project-load (see app.routes.ts), behind authGuard. Multi-project calendar as a
// project x ISO-week grid, paginated one quarter at a time. No department filter -- projects
// aren't department-scoped -- only an optional project subset (empty selection = all projects).
@Component({
  selector: 'chronos-project-load-page',
  standalone: true,
  imports: [IconComponent, ProjectLoadGridComponent],
  template: `
    <div class="page">
      <header class="page-header">
        <div>
          <h1>Project load</h1>
          <p class="page-subtitle">Tasks running per project, week by week.</p>
        </div>
        <div class="stepper" role="group" aria-label="Quarter">
          <button type="button" aria-label="Previous quarter" (click)="shiftQuarter(-1)"><chronos-icon name="chevron-left" /></button>
          <span class="stepper-label">{{ label() }}</span>
          <button type="button" aria-label="Next quarter" (click)="shiftQuarter(1)"><chronos-icon name="chevron-right" /></button>
        </div>
      </header>

      @if (allProjects().length > 0) {
        <ul class="chips" aria-label="Filter projects">
          <li>
            <button type="button" class="chip" [class.chip-active]="selectedIds().size === 0" (click)="clearSelection()">All</button>
          </li>
          @for (project of allProjects(); track project.id) {
            <li>
              <button
                type="button"
                class="chip"
                [class.chip-active]="selectedIds().has(project.id)"
                (click)="toggleProject(project.id)">
                {{ project.code }}
              </button>
            </li>
          }
        </ul>
      }

      @if (resource.error()) {
        <p class="error">Could not load projects.</p>
      }

      <section class="card card-flush">
        <div class="grid-head">
          <span class="legend">Darker cells = more tasks overlapping that week</span>
          @if (resource.isLoading()) {
            <span class="empty-state">Loading…</span>
          }
        </div>
        <div class="table-scroll">
          <chronos-project-load-grid [projects]="visibleProjects()" [from]="fromIso()" [to]="toIso()" />
        </div>
      </section>
    </div>
  `,
  styles: `
    .grid-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-3);
      padding: var(--space-3) var(--space-4);
      border-bottom: 1px solid var(--color-border-light);
    }

    @media (max-width: 767px) {
      .stepper {
        width: 100%;
        justify-content: space-between;
      }
    }
  `
})
export class ProjectLoadPageComponent {
  private readonly projectService = inject(ProjectService);
  // One fetch for all quarters: GET /api/projects returns every project with its full task list,
  // so quarter navigation is purely client-side re-bucketing, no refetch.
  protected readonly resource = this.projectService.projectsResource();

  private readonly quarterOffset = signal(0);
  protected readonly selectedIds = signal<ReadonlySet<string>>(new Set());

  private readonly start = computed(() => quarterStart(new Date(), this.quarterOffset()));
  protected readonly fromIso = computed(() => isoDate(this.start()));
  protected readonly toIso = computed(() => isoDate(quarterEnd(this.start())));
  protected readonly label = computed(() => quarterLabel(this.start()));

  protected readonly allProjects = computed(() => this.resource.value() ?? []);
  protected readonly visibleProjects = computed(() => {
    const selected = this.selectedIds();
    const projects = this.allProjects();
    return selected.size === 0 ? projects : projects.filter((p) => selected.has(p.id));
  });

  shiftQuarter(delta: number): void {
    this.quarterOffset.update((value) => value + delta);
  }

  toggleProject(projectId: string): void {
    this.selectedIds.update((current) => {
      const next = new Set(current);
      if (next.has(projectId)) next.delete(projectId);
      else next.add(projectId);
      return next;
    });
  }

  clearSelection(): void {
    this.selectedIds.set(new Set());
  }
}
