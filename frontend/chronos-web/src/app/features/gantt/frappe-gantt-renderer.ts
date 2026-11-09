import { Injectable } from '@angular/core';
import { GanttTaskDto } from '../../core/api/project.model';
import { GanttChangeHandlers, IGanttRenderer } from './gantt-renderer';

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

const DAY_MS = 24 * 60 * 60 * 1000;

// Local calendar date of the midnight nearest to `date`, as 'YYYY-MM-DD'. Frappe derives dragged
// dates by adding fractional hours to its chart origin, so a bar dropped across a DST boundary can
// land at 23:00 or 01:00 instead of midnight -- rounding absorbs that.
export function toNearestIsoDate(date: Date): string {
  const rounded = new Date(date.getTime() + DAY_MS / 2);
  const y = rounded.getFullYear();
  const m = String(rounded.getMonth() + 1).padStart(2, '0');
  const d = String(rounded.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

// Frappe 0.6 treats a date-only end as exclusive (end + 24h) internally and reports a dragged end
// as (exclusive end - 1 second). Map that back to our inclusive end date.
export function frappeEndToIsoDate(end: Date): string {
  return toNearestIsoDate(new Date(end.getTime() + 1000 - DAY_MS));
}

// Adapter around Frappe Gantt (MIT license -- see README for the library choice rationale).
// Kept deliberately thin: all Frappe-specific mapping lives here, nowhere else.
@Injectable()
export class FrappeGanttRenderer implements IGanttRenderer {
  private instance: FrappeGanttInstance | null = null;
  private container: HTMLElement | null = null;

  render(container: HTMLElement, tasks: GanttTaskDto[], handlers?: GanttChangeHandlers): void {
    const Gantt = frappeGantt();
    this.container = container;
    this.instance = new Gantt(container, this.toFrappeTasks(tasks), {
      view_mode: 'Week',
      date_format: 'YYYY-MM-DD',
      // Frappe fires these once per completed drag (mouseup), not per pixel moved.
      on_date_change: (task: { id: string }, start: Date, end: Date) =>
        handlers?.dateChange(task.id, toNearestIsoDate(start), frappeEndToIsoDate(end)),
      on_progress_change: (task: { id: string }, progress: number) =>
        handlers?.progressChange(task.id, Math.max(0, Math.min(100, Math.round(progress))))
    });
    this.fitHeight();
  }

  updateTasks(tasks: GanttTaskDto[]): void {
    this.instance?.refresh(this.toFrappeTasks(tasks));
    this.fitHeight();
  }

  // Frappe 0.6 sizes its SVG at grid height + 100px (room for its popup), which leaves a large
  // empty band under the last bar. Trim the SVG to the grid it actually drew.
  private fitHeight(): void {
    const svg = this.container?.querySelector<SVGSVGElement>('svg.gantt');
    const gridHeight = svg?.querySelector('.grid-background')?.getAttribute('height');
    if (svg && gridHeight) svg.setAttribute('height', gridHeight);
  }

  destroy(): void {
    this.instance = null;
    this.container = null;
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
