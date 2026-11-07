import { Component, ElementRef, inject, input, output, viewChild, effect } from '@angular/core';
import { GanttTaskDto } from '../../core/api/project.model';
import { GANTT_RENDERER, IGanttRenderer } from './gantt-renderer';
import { FrappeGanttRenderer } from './frappe-gantt-renderer';

export interface GanttDateChange {
  taskId: string;
  startDate: string; // ISO date
  endDate: string; // ISO date, inclusive
}

export interface GanttProgressChange {
  taskId: string;
  progressPercent: number;
}

// Pure renderer: edits made on the chart are emitted, never persisted here -- the parent saves
// them and feeds the reloaded tasks back in through `tasks`.
@Component({
  selector: 'chronos-gantt-chart',
  standalone: true,
  providers: [{ provide: GANTT_RENDERER, useClass: FrappeGanttRenderer }],
  template: `
    <div #container class="gantt-container card table-scroll"></div>
  `,
  styles: [`.gantt-container { width: 100%; }`]
})
export class GanttChartComponent {
  tasks = input.required<GanttTaskDto[]>();
  dateChange = output<GanttDateChange>();
  progressChange = output<GanttProgressChange>();

  private readonly renderer = inject<IGanttRenderer>(GANTT_RENDERER);
  private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');
  private rendered = false;

  constructor() {
    effect(() => {
      const tasks = this.tasks();
      if (!this.rendered) {
        this.renderer.render(this.container().nativeElement, tasks, {
          dateChange: (taskId, startDate, endDate) => this.dateChange.emit({ taskId, startDate, endDate }),
          progressChange: (taskId, progressPercent) => this.progressChange.emit({ taskId, progressPercent })
        });
        this.rendered = true;
      } else {
        this.renderer.updateTasks(tasks);
      }
    });
  }
}
