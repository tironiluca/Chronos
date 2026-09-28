export interface LeavePeriodDto {
  id: string;
  startDate: string; // ISO date
  endDate: string; // ISO date
  type: string;
}

export interface AssignedTaskDto {
  id: string;
  projectId: string;
  name: string;
  startDate: string; // ISO date
  endDate: string; // ISO date
}

export interface AssignedCardDto {
  id: string;
  boardId: string;
  columnId: string;
  title: string;
}

export interface ResourceAvailabilityDto {
  userId: string;
  displayName: string;
  departmentId: string | null;
  approvedLeave: LeavePeriodDto[];
  assignedTasks: AssignedTaskDto[];
  assignedCards: AssignedCardDto[];
}
