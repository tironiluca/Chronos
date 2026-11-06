import { Injectable } from '@angular/core';
import { GanttTaskDto } from '../../core/api/project.model';
import { IGanttRenderer } from './gantt-renderer';

// Minimal shape of what we actually call on a Gantt instance. frappe-gantt ships no .d.ts, so
// this local interface is the only thing we type against.
interface FrappeGanttInstance {
  refresh(tasks: unknown[]): void;
}

type FrappeGanttConstructor = new (container: HTMLElement, tasks: unknown[], options: object) => FrappeGanttInstance;

// frappe-gantt 0.6's dist/frappe-gantt.js is a browser IIFE (`var Gantt = (function () {...})()`)
// with no ES/CommonJS export, so `import Gantt from '...'` yields nothing usable -- the Vite dev
// server rejects it outright and the production bundle gets an empty module. Its ES source
// (package "main") imports a .scss file, which the Angular builder can't load from JS. So the
// prebuilt script + CSS are loaded globally via angular.json "scripts"/"styles", and the
// constructor is read off globalThis at render time -- still only ever touched in this adapter.
function frappeGantt(): FrappeGanttConstructor {
  const ctor = (globalThis as { Gantt?: FrappeGanttConstructor }).Gantt;
  if (!ctor) {
    throw new Error('frappe-gantt is not loaded -- check the "scripts" entry in angular.json.');
  }
  return ctor;
}

// Adapter around Frappe Gantt (MIT license -- see README for the library choice rationale).
// Kept deliberately thin: all Frappe-specific mapping lives here, nowhere else.
@Injectable()
export class FrappeGanttRenderer implements IGanttRenderer {
  private instance: FrappeGanttInstance | null = null;

  render(container: HTMLElement, tasks: GanttTaskDto[]): void {
    const Gantt = frappeGantt();
    this.instance = new Gantt(container, this.toFrappeTasks(tasks), {
      view_mode: 'Week',
      date_format: 'YYYY-MM-DD'
    });
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
