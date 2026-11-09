import { Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../shared/ui/icon.component';
import { BoardService } from '../../core/api/board.service';
import { UserService } from '../../core/api/user.service';
import { UserPickerComponent } from '../users/user-picker.component';
import { ProjectService } from '../../core/api/project.service';
import { BoardDetailDto } from '../../core/api/board.model';

// Renders one board's columns/cards; cards are assigned via a picker over the org's user
// directory (GET /api/users/directory, any org member), fetched once per board.
//
// Gantt cross-link: a board with a ProjectId links to that project's Gantt page, and new cards
// on it can be linked to one of the project's tasks (KanbanCard.GanttTaskId). Standalone boards
// have neither -- GanttTaskId stays null, since the server doesn't existence-check it and there
// would be no project to pick a task from.
@Component({
  selector: 'chronos-kanban-board',
  standalone: true,
  imports: [RouterLink, UserPickerComponent, IconComponent],
  template: `
    @if (resource.isLoading() && !resource.value()) {
      <p class="empty-state">Loading board…</p>
    }
    @if (resource.error()) {
      <p class="error">Could not load board.</p>
    }
    @if (resource.value(); as board) {
      <div class="kanban-board">
        <div class="board-header">
          <div class="board-title">
            <h2>{{ board.name }}</h2>
            @if (linkedProject(); as project) {
              <a class="gantt-link" [routerLink]="['/projects', project.id, 'gantt']">Gantt: {{ project.code }} ›</a>
            }
          </div>
          <form class="add-column" (submit)="onCreateColumn($event, board)">
            <input
              type="text"
              placeholder="New column"
              aria-label="New column name"
              [value]="newColumnName()"
              (input)="newColumnName.set($any($event.target).value)" />
            <button type="submit" class="btn-icon" aria-label="Add column"><chronos-icon name="plus" [size]="18" /></button>
          </form>
        </div>
        @if (actionError()) {
          <p class="error">{{ actionError() }}</p>
        }

        <div class="kanban-columns">
          @for (column of board.columns; track column.id) {
            <div
              class="kanban-column"
              [class.kanban-column-drop]="dropTargetId() === column.id"
              (dragover)="onDragOver($event, column.id)"
              (dragleave)="onDragLeave(column.id)"
              (drop)="onDrop($event, board.id, column.id)">
              <div class="column-header">
                <h3>{{ column.name }}</h3>
                <span class="count">{{ column.cards.length }}</span>
              </div>
              <ul class="kanban-cards">
                @for (card of column.cards; track card.id) {
                  <li class="kanban-card" draggable="true" (dragstart)="onDragStart($event, column.id, card.id)" (dragend)="onDragEnd()">
                    <div class="kanban-card-title">{{ card.title }}</div>
                    @if (card.ganttTaskId && taskNames().get(card.ganttTaskId); as taskName) {
                      <a class="kanban-card-task" [routerLink]="['/projects', board.projectId, 'gantt']">↳ {{ taskName }}</a>
                    }
                    <div class="kanban-card-actions">
                      <chronos-user-picker
                        [users]="directory.value() ?? []"
                        [selectedId]="card.assignedUserId"
                        [ariaLabel]="'Assignee for ' + card.title"
                        (selectionChange)="assign(board.id, column.id, card.id, $event)" />
                      @if (board.columns.length > 1) {
                        <select
                          class="move-select"
                          [attr.aria-label]="'Move ' + card.title"
                          (change)="moveCard(board.id, column.id, card.id, $any($event.target).value); $any($event.target).value = ''">
                          <option value="" selected>Move to…</option>
                          @for (target of board.columns; track target.id) {
                            @if (target.id !== column.id) {
                              <option [value]="target.id">{{ target.name }}</option>
                            }
                          }
                        </select>
                      }
                    </div>
                  </li>
                } @empty {
                  <li class="column-empty">Drop cards here</li>
                }
              </ul>
              <form class="kanban-add-card" (submit)="onCreateCard($event, board.id, column.id)">
                <input
                  type="text"
                  placeholder="Add a card…"
                  [attr.aria-label]="'New card in ' + column.name"
                  [value]="newCardTitles()[column.id] ?? ''"
                  (input)="setNewCardTitle(column.id, $any($event.target).value)" />
                @if (linkedProject(); as project) {
                  <select
                    aria-label="Linked Gantt task"
                    [value]="newCardTaskIds()[column.id] ?? ''"
                    (change)="setNewCardTask(column.id, $any($event.target).value)">
                    <option value="">No linked task</option>
                    @for (task of project.tasks; track task.id) {
                      <option [value]="task.id">{{ task.name }}</option>
                    }
                  </select>
                }
                @if (newCardTitles()[column.id]) {
                  <button type="submit" class="btn-sm">Add card</button>
                }
              </form>
            </div>
          }
        </div>
      </div>
    }
  `,
  styles: `
    .kanban-board {
      display: flex;
      flex-direction: column;
      gap: var(--space-4);
    }

    .board-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-3);
      flex-wrap: wrap;
    }

    .board-title {
      display: flex;
      align-items: baseline;
      gap: var(--space-3);
      flex-wrap: wrap;

      h2 {
        margin: 0;
        font-size: var(--font-size-xl);
      }
    }

    .gantt-link {
      font-size: var(--font-size-sm);
      font-weight: 600;
    }

    .add-column {
      display: flex;
      gap: var(--space-2);

      input {
        width: 180px;

        @media (max-width: 767px) {
          width: auto;
          flex: 1;
        }
      }

      @media (max-width: 767px) {
        width: 100%;
      }
    }

    /* Columns scroll sideways; on phones each column is ~85% of the screen and snaps into place. */
    .kanban-columns {
      display: flex;
      gap: var(--space-4);
      overflow-x: auto;
      padding-bottom: var(--space-3);
      scroll-snap-type: x mandatory;
      -webkit-overflow-scrolling: touch;
      overscroll-behavior-x: contain;

      @media (max-width: 767px) {
        gap: var(--space-3);
        margin-inline: calc(-1 * var(--space-4));
        padding-inline: var(--space-4);
        scroll-padding-inline: var(--space-4);
      }
    }

    .kanban-column {
      display: flex;
      flex-direction: column;
      gap: var(--space-3);
      flex: 0 0 290px;
      scroll-snap-align: start;
      background: var(--color-surface-2);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-lg);
      padding: var(--space-3);
      transition: border-color var(--transition-fast), background var(--transition-fast);

      @media (max-width: 767px) {
        flex-basis: 84vw;
      }
    }

    .kanban-column-drop {
      border-color: var(--color-primary);
      border-style: dashed;
      background: var(--color-primary-bg);
    }

    .column-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 2px var(--space-1) 0;

      h3 {
        margin: 0;
        font-size: var(--font-size-sm);
        font-weight: 650;
      }

      .count {
        min-width: 22px;
        height: 22px;
        padding: 0 6px;
        display: inline-grid;
        place-items: center;
        border-radius: var(--radius-full);
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        font-size: var(--font-size-xs);
        font-weight: 600;
        color: var(--color-text-muted);
      }
    }

    .kanban-cards {
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
      min-height: 48px;
    }

    .kanban-card {
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
      background: var(--color-surface);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-md);
      box-shadow: var(--shadow-xs);
      padding: var(--space-3);
      transition: box-shadow var(--transition-fast), border-color var(--transition-fast);

      &:hover {
        box-shadow: var(--shadow-sm);
        border-color: var(--color-border-strong);
      }

      &[draggable='true'] {
        cursor: grab;
      }
    }

    .kanban-card-title {
      font-size: var(--font-size-sm);
      font-weight: 600;
      line-height: 1.35;
    }

    .kanban-card-task {
      align-self: flex-start;
      font-size: var(--font-size-xs);
      font-weight: 600;
      padding: 2px 8px;
      border-radius: var(--radius-full);
      background: var(--color-primary-bg);
    }

    .kanban-card-actions {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: var(--space-2);

      @media (max-width: 767px) {
        grid-template-columns: 1fr;
      }

      select {
        width: 100%;
        font-size: var(--font-size-sm);
        box-shadow: none;
      }

      chronos-user-picker {
        min-width: 0;
      }
    }

    .column-empty {
      display: grid;
      place-items: center;
      height: 56px;
      border: 1px dashed var(--color-border-strong);
      border-radius: var(--radius-md);
      color: var(--color-text-subtle);
      font-size: var(--font-size-xs);
    }

    .kanban-add-card {
      display: flex;
      flex-direction: column;
      gap: var(--space-2);

      input,
      select {
        width: 100%;
        background: transparent;
        box-shadow: none;
        border-style: dashed;
      }

      input:focus {
        background: var(--color-surface);
        border-style: solid;
      }

      button {
        align-self: flex-start;
      }
    }
  `
})
export class KanbanBoardComponent {
  boardId = input.required<string>();

  private readonly boardService = inject(BoardService);
  private readonly userService = inject(UserService);
  private readonly projectService = inject(ProjectService);

  protected readonly resource = this.boardService.boardDetailResource(() => this.boardId());
  protected readonly directory = this.userService.directoryResource();

  // Only fetched for project-linked boards (a null id skips the request).
  private readonly projectResource = this.projectService.projectResource(() => this.resource.value()?.projectId ?? null);
  protected readonly linkedProject = computed(() =>
    this.resource.value()?.projectId ? this.projectResource.value() : undefined
  );
  protected readonly taskNames = computed(
    () => new Map((this.linkedProject()?.tasks ?? []).map((task) => [task.id, task.name] as const))
  );

  protected readonly newColumnName = signal('');
  protected readonly newCardTitles = signal<Record<string, string>>({});
  protected readonly newCardTaskIds = signal<Record<string, string>>({});
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

  setNewCardTask(columnId: string, taskId: string): void {
    this.newCardTaskIds.update((ids) => ({ ...ids, [columnId]: taskId }));
  }

  onCreateCard(event: Event, boardId: string, columnId: string): void {
    event.preventDefault();
    const title = this.newCardTitles()[columnId];
    if (!title) return;

    const ganttTaskId = this.newCardTaskIds()[columnId] || null;
    this.boardService.createCard(boardId, columnId, { title, ganttTaskId }).subscribe({
      next: () => {
        this.setNewCardTitle(columnId, '');
        this.setNewCardTask(columnId, '');
        this.resource.reload();
      },
      error: (err) => this.actionError.set(err?.error ?? 'Could not create card.')
    });
  }

  // Card being dragged (HTML5 DnD). Kept in a signal rather than only in dataTransfer, which
  // browsers don't let you read during dragover.
  private readonly dragging = signal<{ columnId: string; cardId: string } | null>(null);
  protected readonly dropTargetId = signal<string | null>(null);

  onDragStart(event: DragEvent, columnId: string, cardId: string): void {
    this.dragging.set({ columnId, cardId });
    event.dataTransfer?.setData('text/plain', cardId);
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';
  }

  onDragOver(event: DragEvent, columnId: string): void {
    const dragged = this.dragging();
    if (!dragged || dragged.columnId === columnId) return;
    event.preventDefault(); // marks the column as a valid drop target
    this.dropTargetId.set(columnId);
  }

  onDragLeave(columnId: string): void {
    if (this.dropTargetId() === columnId) this.dropTargetId.set(null);
  }

  onDrop(event: DragEvent, boardId: string, columnId: string): void {
    event.preventDefault();
    const dragged = this.dragging();
    this.onDragEnd();
    if (dragged) this.moveCard(boardId, dragged.columnId, dragged.cardId, columnId);
  }

  onDragEnd(): void {
    this.dragging.set(null);
    this.dropTargetId.set(null);
  }

  // columnId = the card's current column. Same-column/empty targets are ignored client-side.
  moveCard(boardId: string, columnId: string, cardId: string, targetColumnId: string): void {
    if (!targetColumnId || targetColumnId === columnId) return;
    this.actionError.set(null);
    this.boardService.moveCard(boardId, columnId, cardId, { targetColumnId }).subscribe({
      next: () => this.resource.reload(),
      error: (err) => this.actionError.set(err?.error ?? 'Could not move card.')
    });
  }

  // userId null unassigns.
  assign(boardId: string, columnId: string, cardId: string, userId: string | null): void {
    this.actionError.set(null);
    this.boardService.assignCardUser(boardId, columnId, cardId, { userId }).subscribe({
      next: () => this.resource.reload(),
      error: (err) => this.actionError.set(err?.error ?? 'Could not assign card.')
    });
  }
}
