import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { ProjectGanttPageComponent } from './project-gantt-page.component';
import { GanttChartComponent } from './gantt-chart.component';
import { GANTT_RENDERER, IGanttRenderer } from './gantt-renderer';
import { ProjectService } from '../../core/api/project.service';
import { BoardService } from '../../core/api/board.service';
import { BoardDto } from '../../core/api/board.model';
import { GanttTaskDto } from '../../core/api/project.model';
import { createFakeProjectService, makeProject, makeTask } from '../../testing/fake-project-service';
import { UserService } from '../../core/api/user.service';
import { createFakeUserService } from '../../testing/fake-user-service';

class FakeGanttRenderer implements IGanttRenderer {
  renderedTasks: GanttTaskDto[] | null = null;
  render(_container: HTMLElement, tasks: GanttTaskDto[]): void {
    this.renderedTasks = tasks;
  }
  updateTasks(): void {}
  destroy(): void {}
}

const boards: BoardDto[] = [
  { id: 'board-linked', organizationId: 'org-1', name: 'Line 3 board', projectId: 'p1' },
  { id: 'board-other', organizationId: 'org-1', name: 'Other board', projectId: 'p2' },
  { id: 'board-standalone', organizationId: 'org-1', name: 'Standalone', projectId: null }
];

describe('ProjectGanttPageComponent', () => {
  function setup(tasks: GanttTaskDto[] = []) {
    const fakeRenderer = new FakeGanttRenderer();
    const fakeProjectService = createFakeProjectService([makeProject({ id: 'p1', code: 'L3U', tasks })]);
    TestBed.overrideComponent(GanttChartComponent, {
      set: { providers: [{ provide: GANTT_RENDERER, useValue: fakeRenderer }] }
    });
    TestBed.configureTestingModule({
      imports: [ProjectGanttPageComponent],
      providers: [
        { provide: ProjectService, useValue: fakeProjectService },
        {
          provide: BoardService,
          useValue: {
            boardsResource: () => ({ value: signal(boards), isLoading: signal(false), error: signal(undefined), reload: jest.fn() })
          }
        },
        { provide: UserService, useValue: createFakeUserService() },
        provideRouter([])
      ]
    });
    const fixture = TestBed.createComponent(ProjectGanttPageComponent);
    fixture.componentRef.setInput('projectId', 'p1');
    fixture.detectChanges();
    return { fixture, fakeRenderer, fakeProjectService };
  }

  it('loads the project named by the route param and renders its tasks', () => {
    const task = makeTask({ id: 't1', startDate: '2026-01-05', endDate: '2026-01-10' });
    const { fixture, fakeRenderer, fakeProjectService } = setup([task]);

    expect(fakeProjectService.requestedIds[0]()).toBe('p1');
    expect(fixture.nativeElement.querySelector('h1').textContent).toContain('L3U');
    expect(fakeRenderer.renderedTasks).toEqual([task]);
  });

  it('shows an empty state instead of the chart when the project has no tasks', () => {
    const { fixture, fakeRenderer } = setup([]);

    expect(fakeRenderer.renderedTasks).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('No tasks yet');
  });

  it('lists only boards linked to this project, deep-linking to each', () => {
    const { fixture } = setup();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.textContent).toContain('Line 3 board');
    expect(element.textContent).not.toContain('Other board');
    expect(element.textContent).not.toContain('Standalone');
    const hrefs = Array.from(element.querySelectorAll('a')).map((a) => a.getAttribute('href'));
    expect(hrefs).toContain('/boards?boardId=board-linked');
  });

  it('creates a task and reloads the project', () => {
    const { fixture, fakeProjectService } = setup();
    fixture.componentInstance['taskName'].set('Install PLC');
    fixture.componentInstance['taskStart'].set('2026-01-05');
    fixture.componentInstance['taskEnd'].set('2026-01-10');

    fixture.componentInstance.onCreateTask(new Event('submit'), 'p1');

    expect(fakeProjectService.createTask).toHaveBeenCalledWith('p1', {
      name: 'Install PLC',
      startDate: '2026-01-05',
      endDate: '2026-01-10',
      parentTaskId: null
    });
    expect(fakeProjectService.projectReload).toHaveBeenCalled();
  });

  it('rejects an end date before the start date client-side', () => {
    const { fixture, fakeProjectService } = setup();
    fixture.componentInstance['taskName'].set('Backwards');
    fixture.componentInstance['taskStart'].set('2026-01-10');
    fixture.componentInstance['taskEnd'].set('2026-01-05');

    fixture.componentInstance.onCreateTask(new Event('submit'), 'p1');

    expect(fakeProjectService.createTask).not.toHaveBeenCalled();
    expect(fixture.componentInstance['error']()).toBe('End date cannot precede start date.');
  });

  it('lists tasks with an assignee picker preselected to the current assignee', () => {
    const task = makeTask({ id: 't1', name: 'Install PLC', startDate: '2026-01-05', endDate: '2026-01-10', assignedUserId: 'user-2' });
    const { fixture } = setup([task]);

    const select: HTMLSelectElement = fixture.nativeElement.querySelector('select[aria-label="Assignee for Install PLC"]');
    expect(select.value).toBe('user-2');
  });

  it('assigns a task and reloads the project', () => {
    const { fixture, fakeProjectService } = setup([makeTask({ id: 't1', startDate: '2026-01-05', endDate: '2026-01-10' })]);

    fixture.componentInstance.assignTask('p1', 't1', 'user-1');

    expect(fakeProjectService.assignTaskUser).toHaveBeenCalledWith('p1', 't1', { userId: 'user-1' });
    expect(fakeProjectService.projectReload).toHaveBeenCalled();
  });
});
