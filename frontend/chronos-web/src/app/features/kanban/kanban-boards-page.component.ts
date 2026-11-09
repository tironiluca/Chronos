import { IconComponent } from '../../shared/ui/icon.component';
import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { BoardService } from '../../core/api/board.service';
import { KanbanBoardComponent } from './kanban-board.component';
import { ProjectPickerComponent } from '../projects/project-picker.component';

// Routed at /boards (see app.routes.ts), behind authGuard. Boards may stand alone
// (Board.ProjectId optional, per the resource-availability epic's settled design), so the
// project picker defaults to "no linked project". ?boardId= preselects a board -- that's how the
// Gantt page's "Linked Kanban boards" list deep-links here.
@Component({
  selector: 'chronos-kanban-boards-page',
  standalone: true,
  imports: [IconComponent, KanbanBoardComponent, ProjectPickerComponent],
  template: `
    <div class="page">
      <header class="page-header">
        <div>
          <h1>Boards</h1>
          <p class="page-subtitle">Kanban boards, optionally linked to a project's Gantt tasks.</p>
        </div>
        <div class="page-actions">
          <button type="button" class="btn-primary" [attr.aria-expanded]="formVisible()" (click)="showForm.set(!showForm())">
            <chronos-icon [name]="formVisible() ? 'close' : 'plus'" [size]="17" /> New board
          </button>
        </div>
      </header>

      @if (formVisible()) {
      <section class="card">
        <form class="form-grid" (submit)="onCreateBoard($event)">
          <label>
            Board name
            <input
              type="text"
              placeholder="Sprint 12"
              [value]="newBoardName()"
              (input)="newBoardName.set($any($event.target).value)" />
          </label>
          <label>
            Linked project
            <chronos-project-picker
              [selectedId]="newBoardProjectId()"
              allOptionLabel="No linked project"
              (selectionChange)="newBoardProjectId.set($event)" />
          </label>
          <button type="submit" [disabled]="submitting()"><chronos-icon name="plus" [size]="17" /> Create board</button>
          @if (error()) {
            <p class="error">{{ error() }}</p>
          }
        </form>
      </section>
      }

      @if (resource.isLoading() && !resource.value()) {
        <p class="empty-state">Loading boards…</p>
      }
      <ul class="chips" aria-label="Boards">
        @for (board of resource.value(); track board.id) {
          <li>
            <button
              type="button"
              class="chip"
              [class.chip-active]="activeBoardId() === board.id"
              (click)="selectedBoardId.set(board.id)">
              {{ board.name }}
            </button>
          </li>
        } @empty {
          @if (!resource.isLoading()) {
            <li class="card empty no-boards">
              <span class="empty-icon"><chronos-icon name="boards" /></span>
              <strong>No boards yet</strong>
              <p>Create one above. It starts with To Do, In Progress and Done columns.</p>
            </li>
          }
        }
      </ul>

      @if (activeBoardId(); as boardId) {
        <chronos-kanban-board [boardId]="boardId" />
      }
    </div>
  `,
  styles: `
    chronos-project-picker {
      display: flex;
      flex-direction: column;
      gap: 4px;
      font-weight: 400;
    }

    .no-boards {
      width: 100%;
    }

    chronos-kanban-board {
      display: block;
    }
  `
})
export class KanbanBoardsPageComponent {
  private readonly boardService = inject(BoardService);

  protected readonly resource = this.boardService.boardsResource();
  // Bound from the ?boardId= query param via withComponentInputBinding.
  boardId = input<string | undefined>();

  protected readonly selectedBoardId = linkedSignal<string | null>(() => this.boardId() ?? null);
  // Falls back to the first board so the page never opens on an empty canvas.
  protected readonly activeBoardId = computed(() => this.selectedBoardId() ?? this.resource.value()?.[0]?.id ?? null);
  protected readonly newBoardName = signal('');
  protected readonly newBoardProjectId = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  // Same pattern as the projects page: the form hides behind "New board" once boards exist.
  protected readonly showForm = signal(false);
  protected readonly formVisible = computed(
    () => this.showForm() || (!this.resource.isLoading() && (this.resource.value() ?? []).length === 0)
  );

  onCreateBoard(event: Event): void {
    event.preventDefault();
    const name = this.newBoardName();
    if (!name) return;

    this.error.set(null);
    this.submitting.set(true);

    this.boardService.create({ name, projectId: this.newBoardProjectId() }).subscribe({
      next: (id) => {
        this.submitting.set(false);
        this.newBoardName.set('');
        this.newBoardProjectId.set(null);
        this.showForm.set(false);
        this.resource.reload();
        this.selectedBoardId.set(id);
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err?.error ?? 'Could not create board.');
      }
    });
  }
}
