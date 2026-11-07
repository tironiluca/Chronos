import { TestBed } from '@angular/core/testing';
import { GanttTaskEditorComponent, dependentsOf } from './gantt-task-editor.component';
import { GanttTaskDto } from '../../core/api/project.model';
import { makeTask } from '../../testing/fake-project-service';
import { directoryUsers } from '../../testing/fake-user-service';

const design = makeTask({ id: 'design', name: 'Design', startDate: '2026-01-05', endDate: '2026-01-09' });
const build = makeTask({ id: 'build', name: 'Build', startDate: '2026-01-12', endDate: '2026-01-16', dependencies: ['design'] });
const test = makeTask({ id: 'test', name: 'Test', startDate: '2026-01-19', endDate: '2026-01-23', dependencies: ['build'] });
const docs = makeTask({ id: 'docs', name: 'Docs', startDate: '2026-01-19', endDate: '2026-01-20', progressPercent: 30 });
const all = [design, build, test, docs];

describe('dependentsOf', () => {
  it('collects direct and transitive successors only', () => {
    expect([...dependentsOf(all, 'design')].sort()).toEqual(['build', 'test']);
    expect([...dependentsOf(all, 'test')]).toEqual([]);
  });
});

describe('GanttTaskEditorComponent', () => {
  function setup(task: GanttTaskDto, tasks: GanttTaskDto[] = all) {
    TestBed.configureTestingModule({ imports: [GanttTaskEditorComponent] });
    const fixture = TestBed.createComponent(GanttTaskEditorComponent);
    fixture.componentRef.setInput('task', task);
    fixture.componentRef.setInput('tasks', tasks);
    fixture.componentRef.setInput('users', directoryUsers);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;
    const query = <T extends Element>(selector: string) => element.querySelector<T & Element>(selector) as T;
    return { fixture, element, query };
  }

  function type(input: HTMLInputElement, value: string) {
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  it('emits a reschedule with the edited dates', () => {
    const { fixture, query } = setup(docs);
    const emitted: unknown[] = [];
    fixture.componentInstance.reschedule.subscribe((e) => emitted.push(e));

    type(query<HTMLInputElement>('input[aria-label="End date for Docs"]'), '2026-01-22');
    fixture.detectChanges();
    query<HTMLButtonElement>('form button[type="submit"]').click();

    expect(emitted).toEqual([{ startDate: '2026-01-19', endDate: '2026-01-22' }]);
  });

  it('blocks an inverted range and says why', () => {
    const { fixture, query, element } = setup(docs);
    const emitted: unknown[] = [];
    fixture.componentInstance.reschedule.subscribe((e) => emitted.push(e));

    type(query<HTMLInputElement>('input[aria-label="End date for Docs"]'), '2026-01-10');
    fixture.detectChanges();
    fixture.componentInstance.onReschedule(new Event('submit'));

    expect(emitted).toEqual([]);
    expect(element.textContent).toContain('End date cannot precede start date.');
    expect(query<HTMLButtonElement>('form button[type="submit"]').disabled).toBe(true);
  });

  it('emits progress only for a changed whole number in 0..100', () => {
    const { fixture, query } = setup(docs);
    const emitted: number[] = [];
    fixture.componentInstance.progress.subscribe((e) => emitted.push(e));
    const input = query<HTMLInputElement>('input[aria-label="Progress for Docs"]');

    type(input, '150');
    fixture.componentInstance.onProgress();
    type(input, '30'); // unchanged
    fixture.componentInstance.onProgress();
    type(input, '80');
    fixture.componentInstance.onProgress();

    expect(emitted).toEqual([80]);
  });

  it('resets drafts when the task input changes (after a reload)', () => {
    const { fixture, query } = setup(docs);
    type(query<HTMLInputElement>('input[aria-label="Progress for Docs"]'), '80');

    fixture.componentRef.setInput('task', { ...docs, progressPercent: 80 });
    fixture.detectChanges();

    expect(query<HTMLInputElement>('input[aria-label="Progress for Docs"]').value).toBe('80');
    expect(fixture.componentInstance['progressDirty']()).toBe(false);
  });

  it('lists current predecessors and emits their removal', () => {
    const { fixture, query, element } = setup(build);
    const removed: string[] = [];
    fixture.componentInstance.removeDependency.subscribe((id) => removed.push(id));

    expect(element.textContent).toContain('Design');
    query<HTMLButtonElement>('button[aria-label="Remove dependency on Design"]').click();

    expect(removed).toEqual(['design']);
  });

  it('offers predecessors excluding itself, existing ones, and anything that would close a cycle', () => {
    const { query } = setup(build);

    const select = query<HTMLSelectElement>('select[aria-label="New predecessor for Build"]');
    // design: already a predecessor; test: depends on build (cycle); build: itself.
    expect(Array.from(select.options).map((o) => o.value)).toEqual(['', 'docs']);
  });

  it('emits a dependency draft with the picked type and lag', () => {
    const { fixture, query } = setup(docs);
    const emitted: unknown[] = [];
    fixture.componentInstance.addDependency.subscribe((e) => emitted.push(e));

    const predecessor = query<HTMLSelectElement>('select[aria-label="New predecessor for Docs"]');
    predecessor.value = 'build';
    predecessor.dispatchEvent(new Event('change'));
    const kind = query<HTMLSelectElement>('select[aria-label="Dependency type for Docs"]');
    kind.value = 'StartToStart';
    kind.dispatchEvent(new Event('change'));
    type(query<HTMLInputElement>('input[aria-label="Lag days for Docs"]'), '-2');
    fixture.componentInstance.onAddDependency(new Event('submit'));

    expect(emitted).toEqual([{ predecessorTaskId: 'build', type: 'StartToStart', lagDays: -2 }]);
  });

  it('emits the picked assignee', () => {
    const { fixture, query } = setup(docs);
    const emitted: (string | null)[] = [];
    fixture.componentInstance.assign.subscribe((e) => emitted.push(e));

    const select = query<HTMLSelectElement>('select[aria-label="Assignee for Docs"]');
    select.value = 'user-1';
    select.dispatchEvent(new Event('change'));

    expect(emitted).toEqual(['user-1']);
  });
});
