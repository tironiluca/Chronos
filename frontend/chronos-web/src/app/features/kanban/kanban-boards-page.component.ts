import { Component, inject, input, linkedSignal, signal } from '@angular/core';
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
  imports: [KanbanBoardComponent, ProjectPickerComponent],
  template: `
    <div class="page">
      <header class="page-header">
        <h1>Kanban boards</h1>
      </header>

      <form class="toolbar" (submit)="onCreateBoard($event)">
        <input
          type="text"
          placeholder="New board name"
          [value]="newBoardName()"
          (input)="newBoardName.set($any($event.target).value)" />
        <chronos-project-picker
          [selectedId]="newBoardProjectId()"
          allOptionLabel="No linked project"
          (selectionChange)="newBoardProjectId.set($event)" />
        <button type="submit" [disabled]="submitting()">Create board</button>
        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
      </form>

      @if (resource.isLoading()) {
        <p class="empty-state">Loading boards...</p>
      }
      <ul class="chips">
        @for (board of resource.value(); track board.id) {
          <li>
            <button
              type="button"
              class="chip"
              [class.chip-active]="selectedBoardId() === board.id"
              (click)="selectedBoardId.set(board.id)">
              {{ board.name }}
            </button>
          </li>
        } @empty {
          <li class="empty-state">No boards yet — create one above.</li>
        }
      </ul>

      @if (selectedBoardId(); as boardId) {
        <chronos-kanban-board [boardId]="boardId" />
      }
    </div>
  `,
})
export class KanbanBoardsPageComponent {
  private readonly boardService = inject(BoardService);

  protected readonly resource = this.boardService.boardsResource();
  // Bound from the ?boardId= query param via withComponentInputBinding.
  boardId = input<string | undefined>();

  protected readonly selectedBoardId = linkedSignal<string | null>(() => this.boardId() ?? null);
  protected readonly newBoardName = signal('');
  protected readonly newBoardProjectId = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

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
