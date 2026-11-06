import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ProjectLoadPageComponent } from './project-load-page.component';
import { ProjectService } from '../../core/api/project.service';
import { createFakeProjectService, makeProject } from '../../testing/fake-project-service';

type PageInternals = {
  fromIso(): string;
  toIso(): string;
  label(): string;
  visibleProjects(): { id: string }[];
};

describe('ProjectLoadPageComponent', () => {
  function setup() {
    TestBed.configureTestingModule({
      imports: [ProjectLoadPageComponent],
      providers: [
        { provide: ProjectService, useValue: createFakeProjectService([makeProject({ id: 'a' }), makeProject({ id: 'b' })]) },
        provideRouter([])
      ]
    });
    const fixture = TestBed.createComponent(ProjectLoadPageComponent);
    fixture.detectChanges();
    return { fixture, page: fixture.componentInstance as unknown as PageInternals & ProjectLoadPageComponent };
  }

  it('shows the current calendar quarter by default', () => {
    const { page } = setup();

    expect(page.fromIso()).toMatch(/-(01|04|07|10)-01$/);
    expect(page.label()).toMatch(/^Q[1-4] \d{4}$/);
    expect(page.toIso() > page.fromIso()).toBe(true);
  });

  it('pages one quarter at a time', () => {
    const { page } = setup();
    const initialFrom = page.fromIso();

    page.shiftQuarter(1);
    const next = page.fromIso();
    expect(next > initialFrom).toBe(true);

    page.shiftQuarter(-2);
    expect(page.fromIso() < initialFrom).toBe(true);

    page.shiftQuarter(1);
    expect(page.fromIso()).toBe(initialFrom);
  });

  it('shows all projects until a subset is selected, then only that subset', () => {
    const { page } = setup();
    expect(page.visibleProjects().map((p) => p.id)).toEqual(['a', 'b']);

    page.toggleProject('b');
    expect(page.visibleProjects().map((p) => p.id)).toEqual(['b']);

    page.toggleProject('b');
    expect(page.visibleProjects().map((p) => p.id)).toEqual(['a', 'b']);

    page.toggleProject('a');
    page.clearSelection();
    expect(page.visibleProjects().map((p) => p.id)).toEqual(['a', 'b']);
  });
});
