import { Component, computed, input, linkedSignal, output, signal } from '@angular/core';
import { DependencyType, GanttTaskDto } from '../../core/api/project.model';
import { UserDirectoryEntryDto } from '../../core/api/user.model';
import { UserPickerComponent } from '../users/user-picker.component';
import { IconComponent } from '../../shared/ui/icon.component';
import { formatDateRange } from '../../shared/format';

export interface TaskScheduleChange {
  startDate: string;
  endDate: string;
}

export interface TaskDependencyDraft {
  predecessorTaskId: string;
  type: DependencyType;
  lagDays: number;
}

export const DEPENDENCY_TYPES: { value: DependencyType; label: string }[] = [
  { value: 'FinishToStart', label: 'Finish → Start' },
  { value: 'StartToStart', label: 'Start → Start' },
  { value: 'FinishToFinish', label: 'Finish → Finish' },
  { value: 'StartToFinish', label: 'Start → Finish' }
];

// Ids of every task that (transitively) depends on `taskId` -- picking one of them as a
// predecessor would close a cycle. The server rejects that too; this just keeps the picker honest.
export function dependentsOf(tasks: GanttTaskDto[], taskId: string): Set<string> {
  const successors = new Map<string, string[]>();
  for (const task of tasks) {
    for (const predecessorId of task.dependencies) {
      successors.set(predecessorId, [...(successors.get(predecessorId) ?? []), task.id]);
    }
  }

  const found = new Set<string>();
  const pending = [...(successors.get(taskId) ?? [])];
  while (pending.length > 0) {
    const id = pending.pop()!;
    if (found.has(id)) continue;
    found.add(id);
    pending.push(...(successors.get(id) ?? []));
  }
  return found;
}

// Presentational: one task's edit controls. Emits intents; the page performs the HTTP calls and
// reloads, after which the drafts below re-sync to the new task values (linkedSignal).
@Component({
  selector: 'chronos-gantt-task-editor',
  standalone: true,
  imports: [UserPickerComponent, IconComponent],
  template: `
    <details class="task-editor">
      <summary class="task-summary">
        <chronos-icon class="chevron" name="chevron-right" [size]="16" />
        <div class="task-main">
          <strong class="task-name">{{ task().name }}</strong>
          <span class="task-dates">{{ range() }}</span>
        </div>
        <div class="task-progress" [attr.aria-label]="task().progressPercent + '% complete'">
          <span class="bar"><span [style.width.%]="task().progressPercent"></span></span>
          <span class="pct">{{ task().progressPercent }}%</span>
        </div>
        <chronos-user-picker
          class="task-assignee"
          (click)="$event.stopPropagation()"
          [users]="users()"
          [selectedId]="task().assignedUserId"
          [ariaLabel]="'Assignee for ' + task().name"
          (selectionChange)="assign.emit($event)" />
      </summary>

      <div class="task-body">
        <form class="section" (submit)="onReschedule($event)">
          <span class="section-label">Schedule</span>
          <div class="section-controls">
            <input type="date" [attr.aria-label]="'Start date for ' + task().name" [value]="startDraft()"
              (input)="startDraft.set($any($event.target).value)" />
            <input type="date" [attr.aria-label]="'End date for ' + task().name" [value]="endDraft()"
              (input)="endDraft.set($any($event.target).value)" />
            <button type="submit" [disabled]="!scheduleDirty() || !!scheduleError()">Reschedule</button>
          </div>
          @if (scheduleError()) {
            <p class="error">{{ scheduleError() }}</p>
          }
        </form>

        <div class="section">
          <span class="section-label">Progress</span>
          <div class="section-controls">
            <input type="number" min="0" max="100" step="5" class="narrow" inputmode="numeric"
              [attr.aria-label]="'Progress for ' + task().name" [value]="progressDraft()"
              (input)="progressDraft.set($any($event.target).valueAsNumber)" />
            <button type="button" [disabled]="!progressDirty()" (click)="onProgress()">Set progress</button>
          </div>
        </div>

        <div class="section">
          <span class="section-label">Depends on</span>
          <div class="deps">
            @for (predecessor of predecessors(); track predecessor.id) {
              <span class="dep-chip">
                {{ predecessor.name }}
                <button type="button" class="dep-remove" [attr.aria-label]="'Remove dependency on ' + predecessor.name"
                  (click)="removeDependency.emit(predecessor.id)"><chronos-icon name="close" [size]="14" /></button>
              </span>
            } @empty {
              <span class="empty-state">No dependencies</span>
            }
          </div>
          @if (candidates().length > 0) {
            <form class="section-controls" (submit)="onAddDependency($event)">
              <select [attr.aria-label]="'New predecessor for ' + task().name" (change)="predecessorDraft.set($any($event.target).value)">
                <option value="" [selected]="!predecessorDraft()">Add predecessor…</option>
                @for (candidate of candidates(); track candidate.id) {
                  <option [value]="candidate.id" [selected]="candidate.id === predecessorDraft()">{{ candidate.name }}</option>
                }
              </select>
              <select [attr.aria-label]="'Dependency type for ' + task().name" (change)="typeDraft.set($any($event.target).value)">
                @for (type of dependencyTypes; track type.value) {
                  <option [value]="type.value" [selected]="type.value === typeDraft()">{{ type.label }}</option>
                }
              </select>
              <input type="number" min="-365" max="365" class="narrow" title="Lag in days (negative = lead)" inputmode="numeric"
                [attr.aria-label]="'Lag days for ' + task().name" [value]="lagDraft()"
                (input)="lagDraft.set($any($event.target).valueAsNumber)" />
              <button type="submit" [disabled]="!predecessorDraft()">Add</button>
            </form>
          }
        </div>
      </div>
    </details>
  `,
  styles: `
    .task-editor {
      border-bottom: 1px solid var(--color-border-light);

      &[open] {
        .chevron {
          transform: rotate(90deg);
        }

        .task-summary {
          background: var(--color-surface-2);
        }
      }
    }

    .task-summary {
      display: grid;
      grid-template-columns: auto minmax(0, 1fr) 140px 190px;
      align-items: center;
      gap: var(--space-3);
      padding: var(--space-3) var(--space-4);
      cursor: pointer;
      list-style: none;
      transition: background var(--transition-fast);

      &::-webkit-details-marker {
        display: none;
      }

      &:hover {
        background: var(--color-surface-2);
      }

      @media (max-width: 767px) {
        grid-template-columns: auto minmax(0, 1fr) auto;
        row-gap: var(--space-2);

        .task-assignee {
          grid-column: 2 / -1;
        }
      }
    }

    .chevron {
      color: var(--color-text-subtle);
      transition: transform var(--transition-fast);
    }

    .task-main {
      display: flex;
      flex-direction: column;
      min-width: 0;
    }

    .task-name {
      font-weight: 600;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .task-dates {
      font-size: var(--font-size-xs);
      color: var(--color-text-muted);
    }

    .task-progress {
      display: flex;
      align-items: center;
      gap: var(--space-2);

      .bar {
        flex: 1;
        height: 6px;
        border-radius: 3px;
        background: var(--color-surface-hover);
        overflow: hidden;

        span {
          display: block;
          height: 100%;
          background: linear-gradient(90deg, #6366f1, #8b5cf6);
        }
      }

      .pct {
        font-size: var(--font-size-xs);
        font-weight: 600;
        color: var(--color-text-muted);
        min-width: 2.5rem;
        text-align: right;
        font-variant-numeric: tabular-nums;
      }

      @media (max-width: 767px) {
        .bar {
          display: none;
        }
      }
    }

    .task-body {
      display: grid;
      gap: var(--space-4);
      padding: var(--space-3) var(--space-4) var(--space-5) calc(var(--space-4) + 28px);

      @media (max-width: 767px) {
        padding-left: var(--space-4);
      }
    }

    .section {
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
      margin: 0;

      .error {
        margin: 0;
      }
    }

    .section-label {
      font-size: var(--font-size-xs);
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--color-text-muted);
    }

    .section-controls {
      display: flex;
      flex-wrap: wrap;
      gap: var(--space-2);

      input[type='date'],
      select {
        flex: 1 1 160px;
      }

      .narrow {
        width: 6rem;
        flex: none;
      }
    }

    .deps {
      display: flex;
      flex-wrap: wrap;
      gap: var(--space-2);
    }

    .dep-chip {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      height: 30px;
      padding: 0 4px 0 12px;
      border-radius: var(--radius-full);
      background: var(--color-primary-bg);
      color: var(--color-primary);
      font-size: var(--font-size-sm);
      font-weight: 600;
    }

    .dep-remove {
      width: 24px;
      height: 24px;
      padding: 0;
      border: none;
      border-radius: 50%;
      background: transparent;
      box-shadow: none;
      color: inherit;

      &:hover:not(:disabled) {
        background: color-mix(in srgb, var(--color-primary) 15%, transparent);
      }
    }
  `
})
export class GanttTaskEditorComponent {
  task = input.required<GanttTaskDto>();
  tasks = input.required<GanttTaskDto[]>(); // every task of the project, for predecessor lookup
  users = input.required<UserDirectoryEntryDto[]>();

  assign = output<string | null>();
  reschedule = output<TaskScheduleChange>();
  progress = output<number>();
  addDependency = output<TaskDependencyDraft>();
  removeDependency = output<string>();

  protected readonly dependencyTypes = DEPENDENCY_TYPES;
  protected readonly range = computed(() => formatDateRange(this.task().startDate, this.task().endDate));

  protected readonly startDraft = linkedSignal(() => this.task().startDate);
  protected readonly endDraft = linkedSignal(() => this.task().endDate);
  protected readonly progressDraft = linkedSignal(() => this.task().progressPercent);
  protected readonly predecessorDraft = linkedSignal<GanttTaskDto, string>({ source: this.task, computation: () => '' });
  protected readonly typeDraft = signal<DependencyType>('FinishToStart');
  protected readonly lagDraft = signal(0);

  protected readonly scheduleDirty = computed(
    () =>
      !!this.startDraft() &&
      !!this.endDraft() &&
      (this.startDraft() !== this.task().startDate || this.endDraft() !== this.task().endDate)
  );
  protected readonly progressDirty = computed(() => {
    const value = this.progressDraft();
    return Number.isInteger(value) && value >= 0 && value <= 100 && value !== this.task().progressPercent;
  });

  private readonly byId = computed(() => new Map(this.tasks().map((t) => [t.id, t] as const)));
  protected readonly predecessors = computed(() =>
    this.task()
      .dependencies.map((id) => this.byId().get(id))
      .filter((t): t is GanttTaskDto => !!t)
  );
  protected readonly candidates = computed(() => {
    const self = this.task();
    const excluded = dependentsOf(this.tasks(), self.id);
    return this.tasks().filter((t) => t.id !== self.id && !self.dependencies.includes(t.id) && !excluded.has(t.id));
  });

  // An inverted range is easy to produce mid-edit with two date inputs, so it's caught here
  // rather than round-tripped as a 400.
  protected readonly scheduleError = computed(() =>
    this.startDraft() && this.endDraft() && this.endDraft() < this.startDraft() ? 'End date cannot precede start date.' : null
  );

  onReschedule(event: Event): void {
    event.preventDefault();
    if (!this.scheduleDirty() || this.scheduleError()) return;
    this.reschedule.emit({ startDate: this.startDraft(), endDate: this.endDraft() });
  }

  onProgress(): void {
    if (!this.progressDirty()) return;
    this.progress.emit(this.progressDraft());
  }

  onAddDependency(event: Event): void {
    event.preventDefault();
    const predecessorTaskId = this.predecessorDraft();
    if (!predecessorTaskId) return;
    const lag = this.lagDraft();
    this.addDependency.emit({ predecessorTaskId, type: this.typeDraft(), lagDays: Number.isInteger(lag) ? lag : 0 });
  }
}
