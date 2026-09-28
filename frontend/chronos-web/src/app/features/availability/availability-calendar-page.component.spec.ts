import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { AvailabilityCalendarPageComponent } from './availability-calendar-page.component';
import { ResourceAvailabilityService } from '../../core/api/resource-availability.service';
import { DepartmentService } from '../../core/api/department.service';
import { ResourceAvailabilityDto } from '../../core/api/resource-availability.model';

// Both services are faked rather than the transport: this test targets the page's own month
// navigation/filter wiring, not httpResource's fetch-and-cache behaviour (same convention as
// LeaveRequestListComponent's spec).
function createFakeAvailabilityService() {
  const requestedParams: unknown[] = [];
  let params: () => { departmentId: string | null; projectId: string | null; from: string; to: string };
  return {
    availabilityResource: (p: typeof params) => {
      params = p;
      return {
        value: signal<ResourceAvailabilityDto[]>([]),
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
  function setup() {
    const fakeAvailabilityService = createFakeAvailabilityService();
    TestBed.configureTestingModule({
      imports: [AvailabilityCalendarPageComponent],
      providers: [
        { provide: ResourceAvailabilityService, useValue: fakeAvailabilityService },
        { provide: DepartmentService, useValue: createFakeDepartmentService() }
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
});
