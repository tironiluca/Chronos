import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
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
  imports: [RouterLink],
  template: `
    <div class="page">
      <header class="page-header">
        <h1>Projects</h1>
        <a routerLink="/project-load">Load by week ›</a>
      </header>

      <section class="card">
        <h2>New project</h2>
        <form class="toolbar" (submit)="onSubmit($event)">
          <input type="text" placeholder="Name" [value]="name()" (input)="name.set($any($event.target).value)" />
          <input type="text" placeholder="Code" [value]="code()" (input)="code.set($any($event.target).value)" />
          <input type="date" [value]="startDate()" (input)="startDate.set($any($event.target).value)" />
          <button type="submit" [disabled]="submitting()">Create project</button>
        </form>
        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
      </section>

      <section class="card">
        <h2>Existing projects</h2>
        @if (resource.isLoading()) {
          <p class="empty-state">Loading projects...</p>
        }
        @if (resource.error()) {
          <p class="error">Could not load projects.</p>
        }
        @for (project of resource.value() ?? []; track project.id) {
          <div class="item-row">
            <div>
              <strong>{{ project.code }}</strong> — {{ project.name }}
              <span class="badge">{{ project.status }}</span>
            </div>
            <div class="actions">
              <span class="empty-state">{{ project.tasks.length }} task(s)</span>
              <a [routerLink]="['/projects', project.id, 'gantt']">Open Gantt ›</a>
            </div>
          </div>
        } @empty {
          @if (!resource.isLoading()) {
            <p class="empty-state">No projects yet — create one above.</p>
          }
        }
      </section>
    </div>
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
        this.resource.reload();
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err?.error ?? 'Could not create project.');
      }
    });
  }
}
