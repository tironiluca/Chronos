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
      <p class="empty-state">Loading board...</p>
    }
    @if (resource.error()) {
      <p class="error">Could not load board.</p>
    }
    @if (resource.value(); as board) {
      <div class="kanban-board">
        <h2>{{ board.name }}</h2>

        <form class="toolbar" (submit)="onCreateColumn($event, board)">
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
              <ul class="kanban-cards">
                @for (card of column.cards; track card.id) {
                  <li class="kanban-card">
                    <div class="kanban-card-title">{{ card.title }}</div>
                    @if (card.assignedUserId) {
                      <span class="badge badge-assigned">Assigned</span>
                    }
                    <div class="kanban-card-actions">
                      @if (assigningCardId() === card.id) {
                        <input
                          type="email"
                          placeholder="assignee@org.com"
                          [value]="assigneeEmail()"
                          (input)="assigneeEmail.set($any($event.target).value)" />
                        <button type="button" class="btn-sm" (click)="confirmAssign(board.id, column.id, card.id)">
                          Confirm
                        </button>
                      } @else {
                        <button type="button" class="btn-sm btn-ghost" (click)="assigningCardId.set(card.id)">
                          Assign
                        </button>
                        @if (card.assignedUserId) {
                          <button type="button" class="btn-sm btn-danger" (click)="unassign(board.id, column.id, card.id)">
                            Unassign
                          </button>
                        }
                      }
                    </div>
                  </li>
                } @empty {
                  <li class="empty-state">No cards yet.</li>
                }
              </ul>
              <form class="kanban-add-card" (submit)="onCreateCard($event, board.id, column.id)">
                <input
                  type="text"
                  placeholder="New card title"
                  [value]="newCardTitles()[column.id] ?? ''"
                  (input)="setNewCardTitle(column.id, $any($event.target).value)" />
                <button type="submit" class="btn-sm">Add card</button>
              </form>
            </div>
          }
        </div>
        @if (actionError()) {
          <p class="error">{{ actionError() }}</p>
        }
      </div>
    }
  `,
  styles: `
    .kanban-board {
      display: flex;
      flex-direction: column;
      gap: var(--space-4);
    }

    .kanban-columns {
      display: flex;
      gap: var(--space-4);
      overflow-x: auto;
      padding-bottom: var(--space-2);
    }

    .kanban-column {
      display: flex;
      flex-direction: column;
      gap: var(--space-3);
      flex: 0 0 260px;
      background: var(--color-bg);
      border: 1px solid var(--color-border-light);
      border-radius: var(--radius-md);
      padding: var(--space-3);

      h3 {
        margin: 0;
      }
    }

    .kanban-cards {
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
    }

    .kanban-card {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      gap: 0.35rem;
      background: var(--color-surface);
      border-radius: var(--radius-sm);
      box-shadow: var(--shadow-sm);
      padding: var(--space-2) var(--space-3);
    }

    .kanban-card-title {
      font-size: var(--font-size-sm);
      font-weight: 500;
    }

    .kanban-card-actions {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      flex-wrap: wrap;
    }

    .kanban-add-card {
      gap: var(--space-2);
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
