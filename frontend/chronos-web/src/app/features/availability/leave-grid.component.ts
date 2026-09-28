import { Component, computed, input } from '@angular/core';
import { ResourceAvailabilityDto } from '../../core/api/resource-availability.model';

interface DayColumn {
  date: string; // ISO yyyy-MM-dd
  day: number; // day-of-month, for the header
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
    columns.push({ date: isoDate(new Date(Date.UTC(year, month, day))), day });
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
            <th>{{ column.day }}</th>
          }
        </tr>
      </thead>
      <tbody>
        @for (row of rows(); track row.userId) {
          <tr>
            <td class="employee-col">{{ row.displayName }}</td>
            @for (column of columns(); track column.date) {
              <td [class.on-leave]="row.leaveDays.has(column.date)"></td>
            }
          </tr>
        } @empty {
          <tr>
            <td class="employee-col" [attr.colspan]="columns().length + 1">No employees in scope.</td>
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

    .leave-grid th,
    .leave-grid td {
      border: 1px solid var(--color-border-light);
      text-align: center;
      min-width: 1.75rem;
      padding: 0.3rem 0.15rem;
    }

    .leave-grid thead th {
      background: var(--color-bg);
      position: sticky;
      top: 0;
    }

    .employee-col {
      text-align: left;
      white-space: nowrap;
      padding-right: var(--space-2);
      padding-left: var(--space-2);
      position: sticky;
      left: 0;
      background: var(--color-surface);
      z-index: 1;
    }

    thead .employee-col {
      background: var(--color-bg);
      z-index: 2;
    }

    .on-leave {
      background: color-mix(in srgb, var(--color-primary) 25%, var(--color-surface));
    }
  `
})
export class LeaveGridComponent {
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
