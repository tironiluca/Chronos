import { TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
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

@Component({
  standalone: true,
  imports: [GanttChartComponent],
  template: `<chronos-gantt-chart [tasks]="tasks" />`
})
class HostComponent {
  tasks: GanttTaskDto[] = [
    { id: '1', name: 'Install PLC', startDate: '2026-01-05', endDate: '2026-01-10', progressPercent: 0, dependencies: [] }
  ];
}

describe('GanttChartComponent', () => {
  let fakeRenderer: FakeGanttRenderer;

  beforeEach(() => {
    fakeRenderer = new FakeGanttRenderer();

    TestBed.overrideComponent(GanttChartComponent, {
      set: { providers: [{ provide: GANTT_RENDERER, useValue: fakeRenderer }] }
    });

    TestBed.configureTestingModule({ imports: [HostComponent] });
  });

  it('renders the tasks passed in through the input signal', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();

    expect(fakeRenderer.renderedTasks).toEqual(fixture.componentInstance.tasks);
  });

  it('delegates to updateTasks on subsequent changes, not render again', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();

    fixture.componentInstance.tasks = [
      ...fixture.componentInstance.tasks,
      { id: '2', name: 'Test PLC', startDate: '2026-01-11', endDate: '2026-01-12', progressPercent: 0, dependencies: ['1'] }
    ];
    fixture.detectChanges();

    expect(fakeRenderer.updatedTasks).toHaveLength(2);
  });
});
