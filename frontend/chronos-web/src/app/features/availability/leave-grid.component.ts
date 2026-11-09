import { Component, computed, input } from '@angular/core';
import { ResourceAvailabilityDto } from '../../core/api/resource-availability.model';

interface DayColumn {
  date: string; // ISO date
  day: number; // day of month
  weekday: string; // single-letter weekday, display only
  weekend: boolean;
}

interface EmployeeRow {
  userId: string;
  displayName: string;
  leaveDays: Set<string>; // ISO dates covered by approved leave, clipped to the visible month
}

function isoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function daysInMonth(monthStart: string): DayColumn[] {
  const start = new Date(`${monthStart}T00:00:00Z`);
  const year = start.getUTCFullYear();
  const month = start.getUTCMonth();
  const lastDay = new Date(Date.UTC(year, month + 1, 0)).getUTCDate();

  const columns: DayColumn[] = [];
  for (let day = 1; day <= lastDay; day++) {
    const date = new Date(Date.UTC(year, month, day));
    const dow = date.getUTCDay();
    columns.push({ date: isoDate(date), day, weekday: 'SMTWTFS'[dow], weekend: dow === 0 || dow === 6 });
  }
  return columns;
}

// Leave ranges from the API can extend outside the visible month (the query is overlap-filtered,
// not clipped) -- this only marks the columns that are actually rendered.
function leaveDaysFor(entry: ResourceAvailabilityDto, columns: DayColumn[]): Set<string> {
  const days = new Set<string>();
  for (const leave of entry.approvedLeave) {
    for (const column of columns) {
      if (column.date >= leave.startDate && column.date <= leave.endDate) {
        days.add(column.date);
      }
    }
  }
  return days;
}

// Renders the shared vacation calendar as an employee x day grid for a single month, so
// overlapping leave across people is visible at a glance (see IMPLEMENTATION_PLAN.md's
// "employee x day grid" story). Paginated one month at a time by the parent page, not here.
@Component({
  selector: 'chronos-leave-grid',
  standalone: true,
  template: `
    <table class="leave-grid">
      <thead>
        <tr>
          <th class="employee-col">Employee</th>
          @for (column of columns(); track column.date) {
            <th [class.weekend]="column.weekend" [class.today]="column.date === today" [attr.data-wd]="column.weekday">{{ column.day }}</th>
          }
        </tr>
      </thead>
      <tbody>
        @for (row of rows(); track row.userId) {
          <tr>
            <td class="employee-col">{{ row.displayName }}</td>
            @for (column of columns(); track column.date) {
              <td [class.weekend]="column.weekend" [class.on-leave]="row.leaveDays.has(column.date)"></td>
            }
          </tr>
        } @empty {
          <tr>
            <td class="employee-col empty-row" [attr.colspan]="columns().length + 1">No employees in scope.</td>
          </tr>
        }
      </tbody>
    </table>
  `,
  styles: `
    .leave-grid {
      border-collapse: separate;
      border-spacing: 0;
      font-size: var(--font-size-xs);
      width: 100%;
    }

    th,
    td {
      border: none;
      border-bottom: 1px solid var(--color-border-light);
      text-align: center;
      min-width: 2rem;
      height: 40px;
      padding: 0 2px;
      font-variant-numeric: tabular-nums;
    }

    thead th {
      background: var(--color-surface);
      position: sticky;
      top: 0;
      height: 48px;
      color: var(--color-text-muted);
      font-size: var(--font-size-xs);
      text-transform: none;
      letter-spacing: 0;
      line-height: 1.1;

      &[data-wd]::before {
        content: attr(data-wd);
        display: block;
        font-size: 0.625rem;
        font-weight: 600;
        color: var(--color-text-subtle);
      }

      &.today {
        color: var(--color-primary);

        &::before {
          color: var(--color-primary);
        }
      }
    }

    .weekend {
      background: var(--color-surface-2);
    }

    td.on-leave {
      background: transparent;
      position: relative;

      &::after {
        content: '';
        position: absolute;
        inset: 8px 1px;
        border-radius: 6px;
        background: color-mix(in srgb, var(--color-primary) 55%, var(--color-surface));
      }
    }

    .employee-col {
      text-align: left;
      white-space: nowrap;
      padding: 0 var(--space-4);
      position: sticky;
      left: 0;
      background: var(--color-surface);
      z-index: 1;
      font-weight: 550;
      font-size: var(--font-size-sm);
      min-width: 9rem;
      max-width: 12rem;
      overflow: hidden;
      text-overflow: ellipsis;
      box-shadow: 1px 0 0 var(--color-border-light);
    }

    thead .employee-col {
      z-index: 2;
      font-size: var(--font-size-xs);
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: var(--color-text-muted);
    }

    .empty-row {
      color: var(--color-text-muted);
      font-weight: 400;
      padding: var(--space-5) var(--space-4);
    }

    tbody tr:last-child td {
      border-bottom: none;
    }

    @media (max-width: 767px) {
      .employee-col {
        min-width: 7rem;
        padding: 0 var(--space-3);
      }
    }
  `
})
export class LeaveGridComponent {
  protected readonly today = isoDate(new Date(Date.UTC(new Date().getFullYear(), new Date().getMonth(), new Date().getDate())));

  availability = input.required<ResourceAvailabilityDto[]>();
  monthStart = input.required<string>(); // ISO date, first day of the visible month

  protected readonly columns = computed(() => daysInMonth(this.monthStart()));
  protected readonly rows = computed<EmployeeRow[]>(() => {
    const columns = this.columns();
    return this.availability().map((entry) => ({
      userId: entry.userId,
      displayName: entry.displayName,
      leaveDays: leaveDaysFor(entry, columns)
    }));
  });
}
