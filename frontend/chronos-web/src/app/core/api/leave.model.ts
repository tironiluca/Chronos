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

export interface CreateLeaveRequestPayload {
  organizationId: string;
  requesterId: string;
  type: LeaveType;
  startDate: string;
  endDate: string;
}
