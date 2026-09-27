import { Injectable } from '@angular/core';
import Gantt from 'frappe-gantt/dist/frappe-gantt.js';
import 'frappe-gantt/dist/frappe-gantt.css';
import { GanttTaskDto } from '../../core/api/project.model';
import { IGanttRenderer } from './gantt-renderer';

// Minimal shape of what we actually call on a Gantt instance. frappe-gantt ships no .d.ts
// (see src/types.d.ts for the ambient module declaration), so the default import itself isn't
// usable as a type -- this local interface is the only thing we type against.
interface FrappeGanttInstance {
  refresh(tasks: unknown[]): void;
}

// Adapter around Frappe Gantt (MIT license -- see README for the library choice rationale).
// Kept deliberately thin: all Frappe-specific mapping lives here, nowhere else.
@Injectable()
export class FrappeGanttRenderer implements IGanttRenderer {
  private instance: FrappeGanttInstance | null = null;

  render(container: HTMLElement, tasks: GanttTaskDto[]): void {
    this.instance = new Gantt(container, this.toFrappeTasks(tasks), {
      view_mode: 'Week',
      date_format: 'YYYY-MM-DD'
    }) as FrappeGanttInstance;
  }

  updateTasks(tasks: GanttTaskDto[]): void {
    this.instance?.refresh(this.toFrappeTasks(tasks));
  }

  destroy(): void {
    this.instance = null;
  }

  private toFrappeTasks(tasks: GanttTaskDto[]) {
    return tasks.map((t) => ({
      id: t.id,
      name: t.name,
      start: t.startDate,
      end: t.endDate,
      progress: t.progressPercent,
      dependencies: t.dependencies.join(',')
    }));
  }
}
