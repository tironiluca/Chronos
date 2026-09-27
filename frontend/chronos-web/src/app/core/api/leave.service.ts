import { Injectable, inject } from '@angular/core';
import { HttpClient, httpResource } from '@angular/common/http';
import { CreateLeaveRequestPayload, LeaveRequestDto } from './leave.model';

@Injectable({ providedIn: 'root' })
export class LeaveService {
  private readonly http = inject(HttpClient);

  // GET modelled as a resource (stable in Angular 22): re-fetches whenever organizationId changes,
  // exposes .value() / .isLoading() / .error() / .reload() as signals.
  leaveRequestsResource(organizationId: () => string) {
    return httpResource<LeaveRequestDto[]>(() => `/api/leave-requests?organizationId=${organizationId()}`);
  }

  create(payload: CreateLeaveRequestPayload) {
    return this.http.post<string>('/api/leave-requests', payload);
  }

  approve(leaveRequestId: string, approverId: string) {
    return this.http.post<void>(`/api/leave-requests/${leaveRequestId}/approve`, { approverId });
  }

  reject(leaveRequestId: string, approverId: string, reason: string) {
    return this.http.post<void>(`/api/leave-requests/${leaveRequestId}/reject`, { approverId, reason });
  }

  cancel(leaveRequestId: string) {
    return this.http.post<void>(`/api/leave-requests/${leaveRequestId}/cancel`, {});
  }
}
