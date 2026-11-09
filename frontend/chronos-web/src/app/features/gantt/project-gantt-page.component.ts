import { Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../shared/ui/icon.component';
import { formatDate } from '../../shared/format';
import { ProjectService } from '../../core/api/project.service';
import { BoardService } from '../../core/api/board.service';
import { UserService } from '../../core/api/user.service';
import { GanttChartComponent, GanttDateChange, GanttProgressChange } from './gantt-chart.component';
import { GanttTaskEditorComponent, TaskDependencyDraft } from './gantt-task-editor.component';
import { Observable } from 'rxjs';

// Routed at /projects/:projectId/gantt (see app.routes.ts) -- projectId arrives via
// withComponentInputBinding. Loads the project (GET /api/projects/{id}) and hands its tasks to
// GanttChartComponent, which stays a pure renderer. Cross-links to every Kanban board linked
// to this project (Board.ProjectId), and each board links back here.
@Component({
  selector: 'chronos-project-gantt-page',
  standalone: true,
  imports: [GanttChartComponent, GanttTaskEditorComponent, RouterLink, IconComponent],
  template: `
    <div class="page">
      @if (resource.isLoading() && !resource.value()) {
        <p class="empty-state">Loading project…</p>
      }
      @if (resource.error()) {
        <p class="error">Could not load project.</p>
      }
      @if (resource.value(); as project) {
        <header class="page-header">
          <div>
            <a class="back-link" routerLink="/projects"><chronos-icon name="chevron-left" [size]="16" /> All projects</a>
            <h1>{{ project.code }} — {{ project.name }}</h1>
            <p class="page-subtitle">{{ project.tasks.length }} {{ project.tasks.length === 1 ? 'task' : 'tasks' }} · starts {{ startLabel(project.startDate) }}</p>
          </div>
        </header>

        <section class="card">
          <div class="card-header"><h2>New task</h2></div>
          <form class="form-grid" (submit)="onCreateTask($event, project.id)">
            <label>
              Task name
              <input type="text" placeholder="Install PLC" [value]="taskName()" (input)="taskName.set($any($event.target).value)" />
            </label>
            <label>
              Start
              <input type="date" aria-label="Start date" [value]="taskStart()" (input)="taskStart.set($any($event.target).value)" />
            </label>
            <label>
              End
              <input type="date" aria-label="End date" [value]="taskEnd()" (input)="taskEnd.set($any($event.target).value)" />
            </label>
            <button type="submit" [disabled]="submitting()"><chronos-icon name="plus" [size]="17" /> Add task</button>
            @if (error()) {
              <p class="error">{{ error() }}</p>
            }
          </form>
        </section>

        @if (project.tasks.length > 0) {
          <section class="card card-flush">
            <div class="chart-head">
              <h2>Timeline</h2>
              <span class="item-meta hint">Drag a bar to reschedule, or its handle to set progress.</span>
            </div>
            <chronos-gantt-chart
              [tasks]="project.tasks"
              (dateChange)="onChartDateChange(project.id, $event)"
              (progressChange)="onChartProgressChange(project.id, $event)" />
          </section>

          <section class="card card-flush">
            <div class="chart-head">
              <h2>Tasks</h2>
              <span class="item-meta">Tap a task to edit dates, progress and dependencies.</span>
            </div>
            @for (task of project.tasks; track task.id) {
              <chronos-gantt-task-editor
                [task]="task"
                [tasks]="project.tasks"
                [users]="directory.value() ?? []"
                (assign)="assignTask(project.id, task.id, $event)"
                (reschedule)="rescheduleTask(project.id, task.id, $event.startDate, $event.endDate)"
                (progress)="updateProgress(project.id, task.id, $event)"
                (addDependency)="addDependency(project.id, task.id, $event)"
                (removeDependency)="removeDependency(project.id, task.id, $event)" />
            }
            @if (editError()) {
              <p class="error edit-error">{{ editError() }}</p>
            }
          </section>
        } @else {
          <div class="card empty">
            <span class="empty-icon"><chronos-icon name="projects" /></span>
            <strong>No tasks yet — add one above.</strong>
            <p>Tasks appear on the timeline, where you can drag them to reschedule.</p>
          </div>
        }

        <section class="card">
          <div class="card-header"><h2>Linked Kanban boards</h2></div>
          @for (board of linkedBoards(); track board.id) {
            <div class="item-row">
              <span class="item-main">
                <span class="icon-tile"><chronos-icon name="boards" [size]="17" /></span>
                <span class="item-title">{{ board.name }}</span>
              </span>
              <a class="btn btn-sm" routerLink="/boards" [queryParams]="{ boardId: board.id }">Open board ›</a>
            </div>
          } @empty {
            <p class="empty-state">No boards linked to this project.</p>
          }
        </section>
      }
    </div>
  `,
  styles: `
    .chart-head {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      gap: var(--space-2) var(--space-4);
      flex-wrap: wrap;
      padding: var(--space-4) var(--space-5) var(--space-3);
      border-bottom: 1px solid var(--color-border-light);

      h2 {
        margin: 0;
      }

      @media (max-width: 767px) {
        padding: var(--space-3) var(--space-4);
      }
    }

    chronos-gantt-chart ::ng-deep .gantt-container {
      border: none;
      border-radius: 0;
      box-shadow: none;
      padding: 0;
    }

    chronos-gantt-task-editor:last-of-type ::ng-deep .task-editor {
      border-bottom: none;
    }

    .edit-error {
      margin: 0;
      padding: var(--space-3) var(--space-4);
    }

    @media (pointer: coarse), (max-width: 767px) {
      .hint {
        display: none;
      }
    }
  `
})
export class ProjectGanttPageComponent {
  projectId = input.required<string>();

  private readonly projectService = inject(ProjectService);
  private readonly boardService = inject(BoardService);
  private readonly userService = inject(UserService);

  protected readonly resource = this.projectService.projectResource(() => this.projectId());
  private readonly boards = this.boardService.boardsResource();
  protected readonly directory = this.userService.directoryResource();
  protected readonly linkedBoards = computed(() =>
    (this.boards.value() ?? []).filter((board) => board.projectId === this.projectId())
  );

  protected readonly taskName = signal('');
  protected readonly taskStart = signal('');
  protected readonly taskEnd = signal('');
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly editError = signal<string | null>(null);

  protected startLabel(iso: string): string {
    return formatDate(iso);
  }

  // userId null unassigns. Feeds the resource-availability view (AssignedTasks per user).
  assignTask(projectId: string, taskId: string, userId: string | null): void {
    this.mutate(this.projectService.assignTaskUser(projectId, taskId, { userId }), 'Could not assign task.');
  }

  rescheduleTask(projectId: string, taskId: string, startDate: string, endDate: string): void {
    this.mutate(this.projectService.rescheduleTask(projectId, taskId, { startDate, endDate }), 'Could not reschedule task.');
  }

  updateProgress(projectId: string, taskId: string, progressPercent: number): void {
    this.mutate(this.projectService.updateTaskProgress(projectId, taskId, { progressPercent }), 'Could not update progress.');
  }

  addDependency(projectId: string, taskId: string, draft: TaskDependencyDraft): void {
    this.mutate(this.projectService.addTaskDependency(projectId, taskId, draft), 'Could not add dependency.');
  }

  removeDependency(projectId: string, taskId: string, predecessorTaskId: string): void {
    this.mutate(this.projectService.removeTaskDependency(projectId, taskId, predecessorTaskId), 'Could not remove dependency.');
  }

  onChartDateChange(projectId: string, change: GanttDateChange): void {
    this.rescheduleTask(projectId, change.taskId, change.startDate, change.endDate);
  }

  onChartProgressChange(projectId: string, change: GanttProgressChange): void {
    this.updateProgress(projectId, change.taskId, change.progressPercent);
  }

  // Reloads on failure too: a rejected chart drag must snap the bar back to the server's dates,
  // since Frappe has already moved it on screen.
  private mutate(request: Observable<unknown>, fallbackError: string): void {
    this.editError.set(null);
    request.subscribe({
      next: () => this.resource.reload(),
      error: (err) => {
        this.editError.set(typeof err?.error === 'string' ? err.error : fallbackError);
        this.resource.reload();
      }
    });
  }

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
