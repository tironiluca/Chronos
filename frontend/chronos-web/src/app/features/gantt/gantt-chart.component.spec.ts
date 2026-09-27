import { TestBed } from '@angular/core/testing';
import { GanttChartComponent } from './gantt-chart.component';
import { GANTT_RENDERER, IGanttRenderer } from './gantt-renderer';
import { GanttTaskDto } from '../../core/api/project.model';

// Fake adapter: proves the DIP boundary works -- the component is tested with zero
// dependency on the real Frappe Gantt / SVG rendering engine.
class FakeGanttRenderer implements IGanttRenderer {
  renderedTasks: GanttTaskDto[] | null = null;
  updatedTasks: GanttTaskDto[] | null = null;

  render(_container: HTMLElement, tasks: GanttTaskDto[]): void {
    this.renderedTasks = tasks;
  }

  updateTasks(tasks: GanttTaskDto[]): void {
    this.updatedTasks = tasks;
  }

  destroy(): void {}
}

const task1: GanttTaskDto = {
  id: '1', name: 'Install PLC', startDate: '2026-01-05', endDate: '2026-01-10', progressPercent: 0, dependencies: []
};
const task2: GanttTaskDto = {
  id: '2', name: 'Test PLC', startDate: '2026-01-11', endDate: '2026-01-12', progressPercent: 0, dependencies: ['1']
};

describe('GanttChartComponent', () => {
  let fakeRenderer: FakeGanttRenderer;

  beforeEach(() => {
    fakeRenderer = new FakeGanttRenderer();

    TestBed.overrideComponent(GanttChartComponent, {
      set: { providers: [{ provide: GANTT_RENDERER, useValue: fakeRenderer }] }
    });

    TestBed.configureTestingModule({});
  });

  it('renders the tasks passed in through the input signal', () => {
    const fixture = TestBed.createComponent(GanttChartComponent);
    // GanttChartComponent's `tasks` is a required signal input, set via componentRef.setInput
    // rather than a template-bound host property -- Angular's blessed way to drive signal
    // inputs directly in tests (see angular/angular#56863: a plain host-property rebind can
    // leave the child's signal graph stale across a fixture's later detectChanges() calls).
    fixture.componentRef.setInput('tasks', [task1]);
    fixture.detectChanges();

    expect(fakeRenderer.renderedTasks).toEqual([task1]);
  });

  it('delegates to updateTasks on subsequent changes, not render again', () => {
    const fixture = TestBed.createComponent(GanttChartComponent);
    fixture.componentRef.setInput('tasks', [task1]);
    fixture.detectChanges();

    fixture.componentRef.setInput('tasks', [task1, task2]);
    fixture.detectChanges();

    expect(fakeRenderer.renderedTasks).toEqual([task1]);
    expect(fakeRenderer.updatedTasks).toHaveLength(2);
  });
});
