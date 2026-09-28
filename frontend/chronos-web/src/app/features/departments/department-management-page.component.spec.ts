import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { DepartmentManagementPageComponent } from './department-management-page.component';
import { DepartmentService } from '../../core/api/department.service';

function createFakeDepartmentService() {
  return {
    departmentsResource: () => ({
      value: signal([]),
      isLoading: signal(false),
      error: signal(undefined)
    }),
    create: jest.fn()
  };
}

describe('DepartmentManagementPageComponent', () => {
  function setup(fakeService: ReturnType<typeof createFakeDepartmentService>) {
    TestBed.configureTestingModule({
      imports: [DepartmentManagementPageComponent],
      providers: [{ provide: DepartmentService, useValue: fakeService }]
    });

    const fixture = TestBed.createComponent(DepartmentManagementPageComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('creates a department with the chosen parent and clears the form on success', () => {
    const fakeService = createFakeDepartmentService();
    fakeService.create.mockReturnValue(of('dept-9'));
    const fixture = setup(fakeService);
    fixture.componentInstance['name'].set('Platform');
    fixture.componentInstance['code'].set('ENG-PLAT');
    fixture.componentInstance['parentDepartmentId'].set('dept-1');

    fixture.componentInstance.onSubmit(new Event('submit'));

    expect(fakeService.create).toHaveBeenCalledWith({
      name: 'Platform',
      code: 'ENG-PLAT',
      parentDepartmentId: 'dept-1'
    });
    expect(fixture.componentInstance['name']()).toBe('');
    expect(fixture.componentInstance['parentDepartmentId']()).toBeNull();
  });

  it('surfaces the server error on failed creation', () => {
    const fakeService = createFakeDepartmentService();
    fakeService.create.mockReturnValue(throwError(() => ({ error: 'Code already in use' })));
    const fixture = setup(fakeService);
    fixture.componentInstance['name'].set('Platform');
    fixture.componentInstance['code'].set('ENG-PLAT');

    fixture.componentInstance.onSubmit(new Event('submit'));

    expect(fixture.componentInstance['error']()).toBe('Code already in use');
  });
});
