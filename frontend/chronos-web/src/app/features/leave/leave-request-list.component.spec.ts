import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { LeaveRequestListComponent } from './leave-request-list.component';
import { LeaveService } from '../../core/api/leave.service';
import { LeaveRequestDto } from '../../core/api/leave.model';

// The service is faked rather than the transport: this test targets the component's own
// approve/reject/cancel logic, not httpResource's fetch-and-cache behaviour.
function createFakeLeaveService(initial: LeaveRequestDto[]) {
  const reload = jest.fn();
  return {
    leaveRequestsResource: () => ({
      value: signal<LeaveRequestDto[]>(initial),
      isLoading: signal(false),
      error: signal(undefined),
      reload
    }),
    approve: jest.fn().mockReturnValue(of(undefined)),
    reject: jest.fn().mockReturnValue(of(undefined)),
    cancel: jest.fn().mockReturnValue(of(undefined)),
    reloadSpy: reload
  };
}

const pendingRequest: LeaveRequestDto = {
  id: 'lr-1',
  organizationId: 'org-1',
  requesterId: 'user-1',
  type: 'Vacation',
  startDate: '2026-08-10',
  endDate: '2026-08-17',
  status: 'Pending',
  approverId: null,
  rejectionReason: null
};

describe('LeaveRequestListComponent', () => {
  function setup(fakeService: ReturnType<typeof createFakeLeaveService>) {
    TestBed.configureTestingModule({
      imports: [LeaveRequestListComponent],
      providers: [{ provide: LeaveService, useValue: fakeService }]
    });

    const fixture = TestBed.createComponent(LeaveRequestListComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('approves without needing the current user id (server derives it from the JWT)', () => {
    const fakeService = createFakeLeaveService([pendingRequest]);
    const fixture = setup(fakeService);

    fixture.componentInstance.approve('lr-1');

    expect(fakeService.approve).toHaveBeenCalledWith('lr-1');
    expect(fakeService.reloadSpy).toHaveBeenCalled();
  });

  it('rejects with the entered reason and clears the inline form afterwards', () => {
    const fakeService = createFakeLeaveService([pendingRequest]);
    const fixture = setup(fakeService);
    const instance = fixture.componentInstance as unknown as {
      rejectingId: { set(v: string | null): void; (): string | null };
      rejectionReason: { set(v: string): void };
      confirmReject(id: string): void;
    };

    instance.rejectingId.set('lr-1');
    instance.rejectionReason.set('Coverage gap that week');
    instance.confirmReject('lr-1');

    expect(fakeService.reject).toHaveBeenCalledWith('lr-1', 'Coverage gap that week');
    expect(instance.rejectingId()).toBeNull();
  });

  it('does not call reject when no reason has been entered', () => {
    const fakeService = createFakeLeaveService([pendingRequest]);
    const fixture = setup(fakeService);

    fixture.componentInstance.confirmReject('lr-1');

    expect(fakeService.reject).not.toHaveBeenCalled();
  });

  it('cancels a pending request and reloads the list', () => {
    const fakeService = createFakeLeaveService([pendingRequest]);
    const fixture = setup(fakeService);

    fixture.componentInstance.cancel('lr-1');

    expect(fakeService.cancel).toHaveBeenCalledWith('lr-1');
    expect(fakeService.reloadSpy).toHaveBeenCalled();
  });
});
