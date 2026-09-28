import { Component, inject, input, signal } from '@angular/core';
import { BoardService } from '../../core/api/board.service';
import { UserService } from '../../core/api/user.service';
import { BoardDetailDto } from '../../core/api/board.model';

// Renders one board's columns/cards and lets a card be assigned by email (resolved via
// GET /api/users?email=, AdminOnly server-side -- see UserService) since there's no
// organization-wide user list endpoint to build a picker from yet.
@Component({
  selector: 'chronos-kanban-board',
  standalone: true,
  template: `
    @if (resource.isLoading()) {
      <p>Loading board...</p>
    }
    @if (resource.error()) {
      <p class="error">Could not load board.</p>
    }
    @if (resource.value(); as board) {
      <h2>{{ board.name }}</h2>

      <form (submit)="onCreateColumn($event, board)">
        <input
          type="text"
          placeholder="New column name"
          [value]="newColumnName()"
          (input)="newColumnName.set($any($event.target).value)" />
        <button type="submit">Add column</button>
      </form>

      <div class="kanban-columns">
        @for (column of board.columns; track column.id) {
          <div class="kanban-column">
            <h3>{{ column.name }}</h3>
            <ul>
              @for (card of column.cards; track card.id) {
                <li>
                  {{ card.title }}
                  @if (card.assignedUserId) {
                    <span>· assigned</span>
                  }
                  @if (assigningCardId() === card.id) {
                    <input
                      type="email"
                      placeholder="assignee@org.com"
                      [value]="assigneeEmail()"
                      (input)="assigneeEmail.set($any($event.target).value)" />
                    <button (click)="confirmAssign(board.id, column.id, card.id)">Confirm</button>
                  } @else {
                    <button (click)="assigningCardId.set(card.id)">Assign</button>
                    @if (card.assignedUserId) {
                      <button (click)="unassign(board.id, column.id, card.id)">Unassign</button>
                    }
                  }
                </li>
              }
            </ul>
            <form (submit)="onCreateCard($event, board.id, column.id)">
              <input
                type="text"
                placeholder="New card title"
                [value]="newCardTitles()[column.id] ?? ''"
                (input)="setNewCardTitle(column.id, $any($event.target).value)" />
              <button type="submit">Add card</button>
            </form>
          </div>
        }
      </div>
      @if (actionError()) {
        <p class="error">{{ actionError() }}</p>
      }
    }
  `
})
export class KanbanBoardComponent {
  boardId = input.required<string>();

  private readonly boardService = inject(BoardService);
  private readonly userService = inject(UserService);

  protected readonly resource = this.boardService.boardDetailResource(() => this.boardId());

  protected readonly newColumnName = signal('');
  protected readonly newCardTitles = signal<Record<string, string>>({});
  protected readonly assigningCardId = signal<string | null>(null);
  protected readonly assigneeEmail = signal('');
  protected readonly actionError = signal<string | null>(null);

  onCreateColumn(event: Event, board: BoardDetailDto): void {
    event.preventDefault();
    const name = this.newColumnName();
    if (!name) return;

    const order = board.columns.length;
    this.boardService.createColumn(board.id, { name, order }).subscribe({
      next: () => {
        this.newColumnName.set('');
        this.resource.reload();
      },
      error: (err) => this.actionError.set(err?.error ?? 'Could not create column.')
    });
  }

  setNewCardTitle(columnId: string, value: string): void {
    this.newCardTitles.update((titles) => ({ ...titles, [columnId]: value }));
  }

  onCreateCard(event: Event, boardId: string, columnId: string): void {
    event.preventDefault();
    const title = this.newCardTitles()[columnId];
    if (!title) return;

    this.boardService.createCard(boardId, columnId, { title, ganttTaskId: null }).subscribe({
      next: () => {
        this.setNewCardTitle(columnId, '');
        this.resource.reload();
      },
      error: (err) => this.actionError.set(err?.error ?? 'Could not create card.')
    });
  }

  confirmAssign(boardId: string, columnId: string, cardId: string): void {
    const email = this.assigneeEmail();
    if (!email) return;
    this.actionError.set(null);

    this.userService.getByEmail(email).subscribe({
      next: (user) => {
        this.boardService.assignCardUser(boardId, columnId, cardId, { userId: user.id }).subscribe({
          next: () => {
            this.assigningCardId.set(null);
            this.assigneeEmail.set('');
            this.resource.reload();
          },
          error: (err) => this.actionError.set(err?.error ?? 'Could not assign card.')
        });
      },
      error: (err) => this.actionError.set(err?.error ?? 'No user found with that email.')
    });
  }

  unassign(boardId: string, columnId: string, cardId: string): void {
    this.boardService.assignCardUser(boardId, columnId, cardId, { userId: null }).subscribe({
      next: () => this.resource.reload(),
      error: (err) => this.actionError.set(err?.error ?? 'Could not unassign card.')
    });
  }
}
