import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { AvailabilityCalendarPageComponent } from './availability-calendar-page.component';
import { ResourceAvailabilityService } from '../../core/api/resource-availability.service';
import { DepartmentService } from '../../core/api/department.service';
import { ResourceAvailabilityDto } from '../../core/api/resource-availability.model';
import { ProjectService } from '../../core/api/project.service';
import { createFakeProjectService } from '../../testing/fake-project-service';

// Both services are faked rather than the transport: this test targets the page's own month
// navigation/filter wiring, not httpResource's fetch-and-cache behaviour (same convention as
// LeaveRequestListComponent's spec).
function createFakeAvailabilityService(entries: ResourceAvailabilityDto[] = []) {
  const requestedParams: unknown[] = [];
  let params: () => { departmentId: string | null; projectId: string | null; from: string; to: string };
  return {
    availabilityResource: (p: typeof params) => {
      params = p;
      return {
        value: signal<ResourceAvailabilityDto[]>(entries),
        isLoading: signal(false),
        error: signal(undefined),
        reload: jest.fn()
      };
    },
    captureParams: () => {
      requestedParams.push(params());
      return params();
    }
  };
}

function createFakeDepartmentService() {
  return {
    departmentsResource: () => ({
      value: signal([]),
      isLoading: signal(false),
      error: signal(undefined),
      reload: jest.fn()
    })
  };
}

describe('AvailabilityCalendarPageComponent', () => {
  function setup(entries: ResourceAvailabilityDto[] = []) {
    const fakeAvailabilityService = createFakeAvailabilityService(entries);
    TestBed.configureTestingModule({
      imports: [AvailabilityCalendarPageComponent],
      providers: [
        { provide: ResourceAvailabilityService, useValue: fakeAvailabilityService },
        { provide: DepartmentService, useValue: createFakeDepartmentService() },
        { provide: ProjectService, useValue: createFakeProjectService() }
      ]
    });

    const fixture = TestBed.createComponent(AvailabilityCalendarPageComponent);
    fixture.detectChanges();
    return { fixture, fakeAvailabilityService };
  }

  it('queries the availability resource with the current month bounds and no department filter by default', () => {
    const { fakeAvailabilityService } = setup();

    const params = fakeAvailabilityService.captureParams();

    expect(params.departmentId).toBeNull();
    expect(params.projectId).toBeNull();
    expect(params.from.endsWith('-01')).toBe(true);
    expect(params.to >= params.from).toBe(true);
  });

  it('shifts the queried month forward and back on navigation', () => {
    const { fixture, fakeAvailabilityService } = setup();
    const before = fakeAvailabilityService.captureParams();

    fixture.componentInstance.shiftMonth(1);
    const afterNext = fakeAvailabilityService.captureParams();
    expect(afterNext.from > before.from).toBe(true);

    fixture.componentInstance.shiftMonth(-1);
    const afterPrev = fakeAvailabilityService.captureParams();
    expect(afterPrev.from).toBe(before.from);
  });

  it('re-queries with the selected department id', () => {
    const { fixture, fakeAvailabilityService } = setup();

    (fixture.componentInstance as unknown as { departmentId: { set(v: string | null): void } }).departmentId.set(
      'dept-1'
    );

    expect(fakeAvailabilityService.captureParams().departmentId).toBe('dept-1');
  });

  it('re-queries with the selected project id and narrows rows to people with work in it', () => {
    const entry = (userId: string, withTask: boolean): ResourceAvailabilityDto => ({
      userId,
      displayName: userId,
      departmentId: null,
      approvedLeave: [],
      assignedTasks: withTask
        ? [{ id: 't1', projectId: 'project-1', name: 'Task', startDate: '2026-01-01', endDate: '2026-01-02' }]
        : [],
      assignedCards: []
    });
    const { fixture, fakeAvailabilityService } = setup([entry('on-project', true), entry('off-project', false)]);
    const page = fixture.componentInstance as unknown as {
      projectId: { set(v: string | null): void };
      rows(): ResourceAvailabilityDto[];
    };
    expect(page.rows().map((r) => r.userId)).toEqual(['on-project', 'off-project']);

    page.projectId.set('project-1');

    expect(fakeAvailabilityService.captureParams().projectId).toBe('project-1');
    expect(page.rows().map((r) => r.userId)).toEqual(['on-project']);
  });
});
