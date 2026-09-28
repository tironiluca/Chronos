import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { DepartmentPickerComponent } from './department-picker.component';
import { DepartmentService } from '../../core/api/department.service';
import { DepartmentDto } from '../../core/api/department.model';

function createFakeDepartmentService(departments: DepartmentDto[]) {
  return {
    departmentsResource: () => ({
      value: signal<DepartmentDto[]>(departments),
      isLoading: signal(false),
      error: signal(undefined)
    })
  };
}

const parent: DepartmentDto = {
  id: 'dept-1',
  organizationId: 'org-1',
  name: 'Engineering',
  code: 'ENG',
  parentDepartmentId: null
};
const child: DepartmentDto = {
  id: 'dept-2',
  organizationId: 'org-1',
  name: 'Platform',
  code: 'ENG-PLAT',
  parentDepartmentId: 'dept-1'
};

describe('DepartmentPickerComponent', () => {
  function setup(departments: DepartmentDto[]) {
    const fakeService = createFakeDepartmentService(departments);
    TestBed.configureTestingModule({
      imports: [DepartmentPickerComponent],
      providers: [{ provide: DepartmentService, useValue: fakeService }]
    });

    const fixture = TestBed.createComponent(DepartmentPickerComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('flattens nested departments depth-first, parent before child', () => {
    const fixture = setup([child, parent]);
    const options = fixture.componentInstance['options']();

    expect(options.map((o) => o.id)).toEqual(['dept-1', 'dept-2']);
    expect(options[1].depth).toBe(1);
  });

  it('emits null when the "all departments" option is chosen', () => {
    const fixture = setup([parent]);
    const emitted: (string | null)[] = [];
    fixture.componentInstance.selectionChange.subscribe((value) => emitted.push(value));

    fixture.componentInstance.onChange('');

    expect(emitted).toEqual([null]);
  });

  it('emits the selected department id otherwise', () => {
    const fixture = setup([parent]);
    const emitted: (string | null)[] = [];
    fixture.componentInstance.selectionChange.subscribe((value) => emitted.push(value));

    fixture.componentInstance.onChange('dept-1');

    expect(emitted).toEqual(['dept-1']);
  });
});
