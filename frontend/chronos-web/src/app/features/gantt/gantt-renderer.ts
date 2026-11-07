import { InjectionToken } from '@angular/core';
import { GanttTaskDto } from '../../core/api/project.model';

// User edits made directly on the chart (dragging/resizing a bar, dragging its progress handle).
// Dates are ISO 'YYYY-MM-DD', end inclusive -- same convention as GanttTaskDto, so the adapter
// owns any library-specific date conversion.
export interface GanttChangeHandlers {
  dateChange(taskId: string, startDate: string, endDate: string): void;
  progressChange(taskId: string, progressPercent: number): void;
}

// Port (DIP boundary): GanttChartComponent depends on this abstraction only, never on a
// concrete charting library. Swapping the rendering engine later means writing a new
// adapter, not touching the component or its tests.
export interface IGanttRenderer {
  render(container: HTMLElement, tasks: GanttTaskDto[], handlers?: GanttChangeHandlers): void;
  updateTasks(tasks: GanttTaskDto[]): void;
  destroy(): void;
}

export const GANTT_RENDERER = new InjectionToken<IGanttRenderer>('GANTT_RENDERER');
