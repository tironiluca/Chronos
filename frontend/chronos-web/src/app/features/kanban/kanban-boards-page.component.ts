import { Component, inject, signal } from '@angular/core';
import { BoardService } from '../../core/api/board.service';
import { KanbanBoardComponent } from './kanban-board.component';

// Routed at /boards (see app.routes.ts), behind authGuard. Boards may stand alone
// (Board.ProjectId optional, per the resource-availability epic's settled design) so there's no
// project picker here -- just a name.
@Component({
  selector: 'chronos-kanban-boards-page',
  standalone: true,
  imports: [KanbanBoardComponent],
  template: `
    <h1>Kanban boards</h1>

    <form (submit)="onCreateBoard($event)">
      <input
        type="text"
        placeholder="New board name"
        [value]="newBoardName()"
        (input)="newBoardName.set($any($event.target).value)" />
      <button type="submit" [disabled]="submitting()">Create board</button>
      @if (error()) {
        <p class="error">{{ error() }}</p>
      }
    </form>

    @if (resource.isLoading()) {
      <p>Loading boards...</p>
    }
    <ul>
      @for (board of resource.value(); track board.id) {
        <li>
          <button (click)="selectedBoardId.set(board.id)">{{ board.name }}</button>
        </li>
      }
    </ul>

    @if (selectedBoardId(); as boardId) {
      <chronos-kanban-board [boardId]="boardId" />
    }
  `
})
export class KanbanBoardsPageComponent {
  private readonly boardService = inject(BoardService);

  protected readonly resource = this.boardService.boardsResource();
  protected readonly selectedBoardId = signal<string | null>(null);
  protected readonly newBoardName = signal('');
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  onCreateBoard(event: Event): void {
    event.preventDefault();
    const name = this.newBoardName();
    if (!name) return;

    this.error.set(null);
    this.submitting.set(true);

    this.boardService.create({ name, projectId: null }).subscribe({
      next: (id) => {
        this.submitting.set(false);
        this.newBoardName.set('');
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
