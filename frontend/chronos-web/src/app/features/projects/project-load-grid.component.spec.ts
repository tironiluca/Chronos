import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ProjectLoadGridComponent } from './project-load-grid.component';
import { makeProject, makeTask } from '../../testing/fake-project-service';

describe('ProjectLoadGridComponent', () => {
  function render(projects: ReturnType<typeof makeProject>[], from = '2026-10-01', to = '2026-12-31') {
    TestBed.configureTestingModule({ imports: [ProjectLoadGridComponent], providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(ProjectLoadGridComponent);
    fixture.componentRef.setInput('projects', projects);
    fixture.componentRef.setInput('from', from);
    fixture.componentRef.setInput('to', to);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders one column per ISO week overlapping the range', () => {
    const element = render([]);

    const headers = Array.from(element.querySelectorAll('thead th')).map((th) => th.textContent?.trim());
    expect(headers[0]).toBe('Project');
    expect(headers.slice(1)).toEqual(['W40', 'W41', 'W42', 'W43', 'W44', 'W45', 'W46', 'W47', 'W48', 'W49', 'W50', 'W51', 'W52', 'W53']);
  });

  it('counts each project task overlapping each week, including ranges spilling past the quarter', () => {
    const project = makeProject({
      id: 'p1',
      code: 'L3U',
      tasks: [
        // Starts before the quarter, ends in W40 (2026-09-28..10-04).
        makeTask({ id: 't1', startDate: '2026-09-01', endDate: '2026-10-02' }),
        // W40 + W41.
        makeTask({ id: 't2', startDate: '2026-10-03', endDate: '2026-10-06' }),
        // Entirely outside the quarter.
        makeTask({ id: 't3', startDate: '2027-02-01', endDate: '2027-02-05' })
      ]
    });

    const element = render([project]);

    const cells = Array.from(element.querySelectorAll('tbody tr td')).slice(1);
    expect(cells[0].textContent?.trim()).toBe('2');
    expect(cells[0].classList.contains('busy')).toBe(true);
    expect(cells[1].textContent?.trim()).toBe('1');
    expect(cells.slice(2).every((cell) => cell.textContent?.trim() === '')).toBe(true);
  });

  it('links each project row to its Gantt page', () => {
    const element = render([makeProject({ id: 'p1', code: 'L3U' })]);

    expect(element.querySelector('tbody a')?.getAttribute('href')).toBe('/projects/p1/gantt');
  });

  it('shows an empty state with no projects', () => {
    const element = render([]);

    expect(element.querySelector('tbody')?.textContent).toContain('No projects in scope.');
  });
});
