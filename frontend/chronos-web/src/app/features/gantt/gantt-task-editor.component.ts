import { Component, computed, input, linkedSignal, output, signal } from '@angular/core';
import { DependencyType, GanttTaskDto } from '../../core/api/project.model';
import { UserDirectoryEntryDto } from '../../core/api/user.model';
import { UserPickerComponent } from '../users/user-picker.component';

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
  imports: [UserPickerComponent],
  template: `
    <div class="task-editor">
      <div class="task-editor-head">
        <strong>{{ task().name }}</strong>
        <span class="empty-state">{{ task().startDate }} → {{ task().endDate }} · {{ task().progressPercent }}%</span>
        <chronos-user-picker
          [users]="users()"
          [selectedId]="task().assignedUserId"
          [ariaLabel]="'Assignee for ' + task().name"
          (selectionChange)="assign.emit($event)" />
      </div>

      <form class="toolbar" (submit)="onReschedule($event)">
        <input type="date" [attr.aria-label]="'Start date for ' + task().name" [value]="startDraft()"
          (input)="startDraft.set($any($event.target).value)" />
        <input type="date" [attr.aria-label]="'End date for ' + task().name" [value]="endDraft()"
          (input)="endDraft.set($any($event.target).value)" />
        <button type="submit" class="btn-sm" [disabled]="!scheduleDirty() || !!scheduleError()">Reschedule</button>

        <input type="number" min="0" max="100" step="5" class="progress-input"
          [attr.aria-label]="'Progress for ' + task().name" [value]="progressDraft()"
          (input)="progressDraft.set($any($event.target).valueAsNumber)" />
        <button type="button" class="btn-sm" [disabled]="!progressDirty()" (click)="onProgress()">Set progress</button>
      </form>
      @if (scheduleError()) {
        <p class="error">{{ scheduleError() }}</p>
      }

      <div class="toolbar">
        <span class="empty-state">Depends on:</span>
        @for (predecessor of predecessors(); track predecessor.id) {
          <span class="chip">
            {{ predecessor.name }}
            <button type="button" class="btn-ghost btn-sm" [attr.aria-label]="'Remove dependency on ' + predecessor.name"
              (click)="removeDependency.emit(predecessor.id)">×</button>
          </span>
        } @empty {
          <span class="empty-state">nothing</span>
        }
      </div>

      @if (candidates().length > 0) {
        <form class="toolbar" (submit)="onAddDependency($event)">
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
          <input type="number" min="-365" max="365" class="progress-input" title="Lag in days (negative = lead)"
            [attr.aria-label]="'Lag days for ' + task().name" [value]="lagDraft()"
            (input)="lagDraft.set($any($event.target).valueAsNumber)" />
          <button type="submit" class="btn-sm" [disabled]="!predecessorDraft()">Add dependency</button>
        </form>
      }
    </div>
  `,
  styles: `
    .task-editor {
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
      padding: var(--space-3) 0;
      border-bottom: 1px solid var(--color-border-light);
    }

    .task-editor-head {
      display: flex;
      align-items: center;
      gap: var(--space-3);
      flex-wrap: wrap;
    }

    .progress-input {
      width: 5rem;
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
