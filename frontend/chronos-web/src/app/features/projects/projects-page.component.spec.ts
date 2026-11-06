import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { throwError } from 'rxjs';
import { ProjectsPageComponent } from './projects-page.component';
import { ProjectService } from '../../core/api/project.service';
import { createFakeProjectService, makeProject } from '../../testing/fake-project-service';

describe('ProjectsPageComponent', () => {
  function setup(fakeService = createFakeProjectService([makeProject({ id: 'p1', code: 'L3U', name: 'Line 3 Upgrade' })])) {
    TestBed.configureTestingModule({
      imports: [ProjectsPageComponent],
      providers: [{ provide: ProjectService, useValue: fakeService }, provideRouter([])]
    });
    const fixture = TestBed.createComponent(ProjectsPageComponent);
    fixture.detectChanges();
    return { fixture, fakeService };
  }

  it('lists projects with a link to each Gantt page', () => {
    const { fixture } = setup();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.textContent).toContain('Line 3 Upgrade');
    const hrefs = Array.from(element.querySelectorAll('a')).map((a) => a.getAttribute('href'));
    expect(hrefs).toContain('/projects/p1/gantt');
  });

  it('creates a project and reloads the list', () => {
    const { fixture, fakeService } = setup();
    fixture.componentInstance['name'].set('Packaging Line');
    fixture.componentInstance['code'].set('PKG');
    fixture.componentInstance['startDate'].set('2026-12-01');

    fixture.componentInstance.onSubmit(new Event('submit'));

    expect(fakeService.create).toHaveBeenCalledWith({ name: 'Packaging Line', code: 'PKG', startDate: '2026-12-01' });
    expect(fakeService.projectsReload).toHaveBeenCalled();
    expect(fixture.componentInstance['name']()).toBe('');
  });

  it('does not submit without a name and code', () => {
    const { fixture, fakeService } = setup();

    fixture.componentInstance.onSubmit(new Event('submit'));

    expect(fakeService.create).not.toHaveBeenCalled();
    expect(fixture.componentInstance['error']()).toBe('Name, code and start date are required.');
  });

  it('surfaces the server error on failed creation', () => {
    const fakeService = createFakeProjectService();
    fakeService.create.mockReturnValue(throwError(() => ({ error: 'Project code is required.' })));
    const { fixture } = setup(fakeService);
    fixture.componentInstance['name'].set('X');
    fixture.componentInstance['code'].set(' ');

    fixture.componentInstance.onSubmit(new Event('submit'));

    expect(fixture.componentInstance['error']()).toBe('Project code is required.');
  });
});
