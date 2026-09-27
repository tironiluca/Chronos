import { Injectable, inject } from '@angular/core';
import { HttpClient, httpResource } from '@angular/common/http';
import { CreateLeaveRequestPayload, LeaveRequestDto } from './leave.model';

@Injectable({ providedIn: 'root' })
export class LeaveService {
  private readonly http = inject(HttpClient);

  // Always the caller's own organization -- the server derives it from the JWT, so there is
  // no organizationId parameter here to get wrong.
  leaveRequestsResource() {
    return httpResource<LeaveRequestDto[]>(() => '/api/leave-requests');
  }

  create(payload: CreateLeaveRequestPayload) {
    return this.http.post<string>('/api/leave-requests', payload);
  }

  // approverId is derived server-side from the caller's JWT (see LeaveEndpoints), so it is
  // not something the client passes -- it always means "the currently logged-in user".
  approve(leaveRequestId: string) {
    return this.http.post<void>(`/api/leave-requests/${leaveRequestId}/approve`, {});
  }

  reject(leaveRequestId: string, reason: string) {
    return this.http.post<void>(`/api/leave-requests/${leaveRequestId}/reject`, { reason });
  }

  cancel(leaveRequestId: string) {
    return this.http.post<void>(`/api/leave-requests/${leaveRequestId}/cancel`, {});
  }
}
