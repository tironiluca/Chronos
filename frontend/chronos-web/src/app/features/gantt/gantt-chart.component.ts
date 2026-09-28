import { Component, ElementRef, inject, input, viewChild, effect } from '@angular/core';
import { GanttTaskDto } from '../../core/api/project.model';
import { GANTT_RENDERER, IGanttRenderer } from './gantt-renderer';
import { FrappeGanttRenderer } from './frappe-gantt-renderer';

@Component({
  selector: 'chronos-gantt-chart',
  standalone: true,
  providers: [{ provide: GANTT_RENDERER, useClass: FrappeGanttRenderer }],
  template: `
    <div class="page">
      <div #container class="gantt-container card table-scroll"></div>
    </div>
  `,
  styles: [`.gantt-container { width: 100%; }`]
})
export class GanttChartComponent {
  tasks = input.required<GanttTaskDto[]>();

  private readonly renderer = inject<IGanttRenderer>(GANTT_RENDERER);
  private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');
  private rendered = false;

  constructor() {
    effect(() => {
      const tasks = this.tasks();
      if (!this.rendered) {
        this.renderer.render(this.container().nativeElement, tasks);
        this.rendered = true;
      } else {
        this.renderer.updateTasks(tasks);
      }
    });
  }
}
