import { InjectionToken } from '@angular/core';
import { GanttTaskDto } from '../../core/api/project.model';

// Port (DIP boundary): GanttChartComponent depends on this abstraction only, never on a
// concrete charting library. Swapping the rendering engine later means writing a new
// adapter, not touching the component or its tests.
export interface IGanttRenderer {
  render(container: HTMLElement, tasks: GanttTaskDto[]): void;
  updateTasks(tasks: GanttTaskDto[]): void;
  destroy(): void;
}

export const GANTT_RENDERER = new InjectionToken<IGanttRenderer>('GANTT_RENDERER');
