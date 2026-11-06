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
  imports: [ProjectLoadGridComponent],
  template: `
    <div class="page">
      <header class="page-header">
        <h1>Project load by week</h1>
      </header>
      <div class="toolbar">
        <button type="button" (click)="shiftQuarter(-1)">‹ Prev</button>
        <span class="quarter-label">{{ label() }}</span>
        <button type="button" (click)="shiftQuarter(1)">Next ›</button>
      </div>

      <ul class="chips">
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
        @if (selectedIds().size > 0) {
          <li><button type="button" class="btn-ghost btn-sm" (click)="clearSelection()">Show all</button></li>
        }
      </ul>

      @if (resource.isLoading()) {
        <p class="empty-state">Loading projects...</p>
      }
      @if (resource.error()) {
        <p class="error">Could not load projects.</p>
      }

      <div class="card table-scroll">
        <chronos-project-load-grid [projects]="visibleProjects()" [from]="fromIso()" [to]="toIso()" />
      </div>
    </div>
  `,
  styles: `
    .quarter-label {
      font-weight: 600;
      min-width: 6rem;
      text-align: center;
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
