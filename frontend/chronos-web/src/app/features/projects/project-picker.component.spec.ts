import { TestBed } from '@angular/core/testing';
import { ProjectPickerComponent } from './project-picker.component';
import { ProjectService } from '../../core/api/project.service';
import { createFakeProjectService, makeProject } from '../../testing/fake-project-service';

describe('ProjectPickerComponent', () => {
  function setup() {
    TestBed.configureTestingModule({
      imports: [ProjectPickerComponent],
      providers: [
        { provide: ProjectService, useValue: createFakeProjectService([makeProject({ id: 'p1', code: 'L3U', name: 'Line 3' })]) }
      ]
    });
    const fixture = TestBed.createComponent(ProjectPickerComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('renders an "all" option followed by each project', () => {
    const fixture = setup();

    const options = Array.from(fixture.nativeElement.querySelectorAll('option') as NodeListOf<HTMLOptionElement>);
    expect(options.map((o) => o.value)).toEqual(['', 'p1']);
    expect(options[1].textContent).toContain('L3U');
  });

  it('emits null for the "all" option and the id otherwise', () => {
    const fixture = setup();
    const emitted: (string | null)[] = [];
    fixture.componentInstance.selectionChange.subscribe((value) => emitted.push(value));

    fixture.componentInstance.onChange('p1');
    fixture.componentInstance.onChange('');

    expect(emitted).toEqual(['p1', null]);
  });
});
