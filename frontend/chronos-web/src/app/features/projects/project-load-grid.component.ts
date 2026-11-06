import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProjectDto } from '../../core/api/project.model';
import { IsoWeekColumn, isoWeeksBetween, overlaps } from './iso-week';

interface ProjectRow {
  id: string;
  code: string;
  name: string;
  counts: number[]; // tasks overlapping each column, same order as columns()
}

// Same "overlap made visible" idea as the employee x day leave grid, on the other axis: rows are
// projects, columns are ISO weeks, each cell = number of the project's GanttTasks overlapping
// that week. Bucketing is client-side from raw task ranges (no server-side date bucketing), and
// the parent page owns pagination (one quarter at a time).
@Component({
  selector: 'chronos-project-load-grid',
  standalone: true,
  imports: [RouterLink],
  template: `
    <table class="load-grid">
      <thead>
        <tr>
          <th class="project-col">Project</th>
          @for (column of columns(); track column.key) {
            <th [attr.title]="column.start + ' → ' + column.end">W{{ column.week }}</th>
          }
        </tr>
      </thead>
      <tbody>
        @for (row of rows(); track row.id) {
          <tr>
            <td class="project-col">
              <a [routerLink]="['/projects', row.id, 'gantt']">{{ row.code }}</a> {{ row.name }}
            </td>
            @for (count of row.counts; track $index) {
              <td [class.busy]="count > 0" [style.--load]="loadPercent(count)">{{ count || '' }}</td>
            }
          </tr>
        } @empty {
          <tr>
            <td class="project-col" [attr.colspan]="columns().length + 1">No projects in scope.</td>
          </tr>
        }
      </tbody>
    </table>
  `,
  styles: `
    .load-grid {
      border-collapse: separate;
      border-spacing: 0;
      font-size: var(--font-size-xs);
      width: 100%;
    }

    .load-grid th,
    .load-grid td {
      border: 1px solid var(--color-border-light);
      text-align: center;
      min-width: 2.5rem;
      padding: 0.3rem 0.15rem;
    }

    .load-grid thead th {
      background: var(--color-bg);
      position: sticky;
      top: 0;
    }

    .load-grid .project-col {
      text-align: left;
      white-space: nowrap;
      padding-right: var(--space-2);
      padding-left: var(--space-2);
      position: sticky;
      left: 0;
      background: var(--color-surface);
      z-index: 1;
    }

    .load-grid thead .project-col {
      background: var(--color-bg);
      z-index: 2;
    }

    .busy {
      font-weight: 600;
      background: color-mix(in srgb, var(--color-primary) var(--load), var(--color-surface));
    }
  `
})
export class ProjectLoadGridComponent {
  projects = input.required<ProjectDto[]>();
  from = input.required<string>(); // ISO date, first day of the visible range
  to = input.required<string>(); // ISO date, last day of the visible range

  protected readonly columns = computed<IsoWeekColumn[]>(() => isoWeeksBetween(this.from(), this.to()));
  protected readonly rows = computed<ProjectRow[]>(() => {
    const columns = this.columns();
    return this.projects().map((project) => ({
      id: project.id,
      code: project.code,
      name: project.name,
      counts: columns.map((column) => project.tasks.filter((t) => overlaps(t.startDate, t.endDate, column)).length)
    }));
  });

  // Shade scales with load, capped so a single busy week doesn't wash out the text.
  protected loadPercent(count: number): string {
    return `${Math.min(15 + count * 12, 60)}%`;
  }
}
