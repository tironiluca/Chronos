import { Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProjectService } from '../../core/api/project.service';
import { BoardService } from '../../core/api/board.service';
import { GanttChartComponent } from './gantt-chart.component';

// Routed at /projects/:projectId/gantt (see app.routes.ts) -- projectId arrives via
// withComponentInputBinding. Loads the project (GET /api/projects/{id}) and hands its tasks to
// GanttChartComponent, which stays a pure renderer. Cross-links to every Kanban board linked
// to this project (Board.ProjectId), and each board links back here.
@Component({
  selector: 'chronos-project-gantt-page',
  standalone: true,
  imports: [GanttChartComponent, RouterLink],
  template: `
    <div class="page">
      @if (resource.isLoading()) {
        <p class="empty-state">Loading project...</p>
      }
      @if (resource.error()) {
        <p class="error">Could not load project.</p>
      }
      @if (resource.value(); as project) {
        <header class="page-header">
          <h1>{{ project.code }} — {{ project.name }}</h1>
          <a routerLink="/projects">‹ All projects</a>
        </header>

        <section class="card">
          <h2>New task</h2>
          <form class="toolbar" (submit)="onCreateTask($event, project.id)">
            <input type="text" placeholder="Task name" [value]="taskName()" (input)="taskName.set($any($event.target).value)" />
            <input type="date" aria-label="Start date" [value]="taskStart()" (input)="taskStart.set($any($event.target).value)" />
            <input type="date" aria-label="End date" [value]="taskEnd()" (input)="taskEnd.set($any($event.target).value)" />
            <button type="submit" [disabled]="submitting()">Add task</button>
          </form>
          @if (error()) {
            <p class="error">{{ error() }}</p>
          }
        </section>

        @if (project.tasks.length > 0) {
          <chronos-gantt-chart [tasks]="project.tasks" />
        } @else {
          <p class="empty-state">No tasks yet — add one above.</p>
        }

        <section class="card">
          <h2>Linked Kanban boards</h2>
          @for (board of linkedBoards(); track board.id) {
            <div class="item-row">
              <span>{{ board.name }}</span>
              <a routerLink="/boards" [queryParams]="{ boardId: board.id }">Open board ›</a>
            </div>
          } @empty {
            <p class="empty-state">No boards linked to this project.</p>
          }
        </section>
      }
    </div>
  `
})
export class ProjectGanttPageComponent {
  projectId = input.required<string>();

  private readonly projectService = inject(ProjectService);
  private readonly boardService = inject(BoardService);

  protected readonly resource = this.projectService.projectResource(() => this.projectId());
  private readonly boards = this.boardService.boardsResource();
  protected readonly linkedBoards = computed(() =>
    (this.boards.value() ?? []).filter((board) => board.projectId === this.projectId())
  );

  protected readonly taskName = signal('');
  protected readonly taskStart = signal('');
  protected readonly taskEnd = signal('');
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  onCreateTask(event: Event, projectId: string): void {
    event.preventDefault();
    if (!this.taskName() || !this.taskStart() || !this.taskEnd()) {
      this.error.set('Name, start and end date are required.');
      return;
    }
    if (this.taskEnd() < this.taskStart()) {
      this.error.set('End date cannot precede start date.');
      return;
    }

    this.error.set(null);
    this.submitting.set(true);
    this.projectService
      .createTask(projectId, { name: this.taskName(), startDate: this.taskStart(), endDate: this.taskEnd(), parentTaskId: null })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.taskName.set('');
          this.resource.reload();
        },
        error: (err) => {
          this.submitting.set(false);
          this.error.set(err?.error ?? 'Could not create task.');
        }
      });
  }
}
