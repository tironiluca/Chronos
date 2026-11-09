import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../shared/ui/icon.component';
import { ProjectDto } from '../../core/api/project.model';
import { ProjectService } from '../../core/api/project.service';

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

// Routed at /projects (see app.routes.ts), behind authGuard. Entry point to each project's
// Gantt page -- before this there was no UI route that led to /projects/:id/gantt at all.
// Creation has no extra policy server-side (same as ProjectEndpoints).
@Component({
  selector: 'chronos-projects-page',
  standalone: true,
  imports: [RouterLink, IconComponent],
  template: `
    <div class="page">
      <header class="page-header">
        <div>
          <h1>Projects</h1>
          <p class="page-subtitle">Plan work on a Gantt chart and track load across weeks.</p>
        </div>
        <div class="page-actions">
          <a class="btn" routerLink="/project-load"><chronos-icon name="load" [size]="17" /> Load by week</a>
          <button type="button" class="btn-primary" [attr.aria-expanded]="formVisible()" (click)="showForm.set(!showForm())">
            <chronos-icon [name]="formVisible() ? 'close' : 'plus'" [size]="17" /> New project
          </button>
        </div>
      </header>

      @if (formVisible()) {
      <section class="card">
        <div class="card-header">
          <h2>New project</h2>
        </div>
        <form class="form-grid" (submit)="onSubmit($event)">
          <label>
            Name
            <input type="text" placeholder="Line 3 upgrade" [value]="name()" (input)="name.set($any($event.target).value)" />
          </label>
          <label>
            Code
            <input type="text" placeholder="L3U" [value]="code()" (input)="code.set($any($event.target).value)" />
          </label>
          <label>
            Start date
            <input type="date" [value]="startDate()" (input)="startDate.set($any($event.target).value)" />
          </label>
          <button type="submit" [disabled]="submitting()"><chronos-icon name="plus" [size]="17" /> Create project</button>
          @if (error()) {
            <p class="error">{{ error() }}</p>
          }
        </form>
      </section>
      }

      @if (resource.error()) {
        <p class="error">Could not load projects.</p>
      }

      <div class="project-grid">
        @for (project of resource.value() ?? []; track project.id) {
          <a class="project-card" [routerLink]="['/projects', project.id, 'gantt']">
            <div class="project-card-top">
              <span class="project-code">{{ project.code }}</span>
              <span class="badge" [class]="'badge badge-' + project.status.toLowerCase()">{{ project.status }}</span>
            </div>
            <span class="project-name">{{ project.name }}</span>
            <div class="progress" [attr.aria-label]="progressOf(project) + '% complete'">
              <span [style.width.%]="progressOf(project)"></span>
            </div>
            <div class="project-card-foot">
              <span>{{ project.tasks.length }} {{ project.tasks.length === 1 ? 'task' : 'tasks' }} · {{ progressOf(project) }}%</span>
              <span class="open">Open Gantt <chronos-icon name="arrow-right" [size]="15" /></span>
            </div>
          </a>
        } @empty {
          @if (resource.isLoading()) {
            @for (i of [1, 2, 3]; track i) {
              <div class="project-card"><div class="skeleton" style="height: 88px"></div></div>
            }
          } @else {
            <div class="card empty span-all">
              <span class="empty-icon"><chronos-icon name="projects" /></span>
              <strong>No projects yet</strong>
              <p>Create your first project above, then add tasks on its Gantt chart.</p>
            </div>
          }
        }
      </div>
    </div>
  `,
  styles: `
    .project-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(min(100%, 280px), 1fr));
      gap: var(--space-4);

      .span-all {
        grid-column: 1 / -1;
      }
    }

    .project-card {
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
      padding: var(--space-4) var(--space-5);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-lg);
      background: var(--color-surface);
      box-shadow: var(--shadow-sm);
      color: var(--color-text);
      text-decoration: none;
      transition: border-color var(--transition-fast), box-shadow var(--transition-fast), transform var(--transition-fast);

      &:hover {
        text-decoration: none;
        border-color: color-mix(in srgb, var(--color-primary) 40%, var(--color-border));
        box-shadow: var(--shadow-md);
        transform: translateY(-1px);

        .open {
          color: var(--color-primary);
        }
      }
    }

    .project-card-top {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .project-code {
      font-size: var(--font-size-xs);
      font-weight: 700;
      letter-spacing: 0.06em;
      color: var(--color-text-muted);
    }

    .project-name {
      font-size: var(--font-size-lg);
      font-weight: 650;
      letter-spacing: -0.01em;
    }

    .progress {
      height: 6px;
      border-radius: 3px;
      background: var(--color-surface-hover);
      overflow: hidden;
      margin-top: var(--space-1);

      span {
        display: block;
        height: 100%;
        border-radius: 3px;
        background: linear-gradient(90deg, #6366f1, #8b5cf6);
      }
    }

    .project-card-foot {
      display: flex;
      align-items: center;
      justify-content: space-between;
      font-size: var(--font-size-sm);
      color: var(--color-text-muted);

      .open {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        font-weight: 600;
      }
    }
  `
})
export class ProjectsPageComponent {
  private readonly projectService = inject(ProjectService);

  protected readonly resource = this.projectService.projectsResource();
  protected readonly name = signal('');
  protected readonly code = signal('');
  protected readonly startDate = signal(todayIso());
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  // The create form is tucked behind "New project" once projects exist, so the list stays above
  // the fold on phones; with no projects yet it's the only useful thing on the page.
  protected readonly showForm = signal(false);
  protected readonly formVisible = computed(
    () => this.showForm() || (!this.resource.isLoading() && (this.resource.value() ?? []).length === 0)
  );

  // Average task progress; 0 for a project without tasks.
  protected progressOf(project: ProjectDto): number {
    if (project.tasks.length === 0) return 0;
    return Math.round(project.tasks.reduce((sum, task) => sum + task.progressPercent, 0) / project.tasks.length);
  }

  onSubmit(event: Event): void {
    event.preventDefault();
    if (!this.name() || !this.code() || !this.startDate()) {
      this.error.set('Name, code and start date are required.');
      return;
    }

    this.error.set(null);
    this.submitting.set(true);
    this.projectService.create({ name: this.name(), code: this.code(), startDate: this.startDate() }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.name.set('');
        this.code.set('');
        this.showForm.set(false);
        this.resource.reload();
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err?.error ?? 'Could not create project.');
      }
    });
  }
}
