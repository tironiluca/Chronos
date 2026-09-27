export type LeaveType = 'Vacation' | 'SickLeave' | 'Unpaid' | 'Compensatory';
export type LeaveStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';

export interface LeaveRequestDto {
  id: string;
  organizationId: string;
  requesterId: string;
  type: LeaveType;
  startDate: string; // ISO date
  endDate: string; // ISO date
  status: LeaveStatus;
  approverId: string | null;
  rejectionReason: string | null;
}

// organizationId/requesterId are NOT sent by the client: the server derives both from the
// caller's JWT (see LeaveEndpoints.MapLeaveEndpoints), so a user can only ever create a
// request for themselves in their own organization.
export interface CreateLeaveRequestPayload {
  type: LeaveType;
  startDate: string;
  endDate: string;
}
