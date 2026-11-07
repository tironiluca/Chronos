export interface GanttTaskDto {
  id: string;
  projectId: string;
  name: string;
  startDate: string; // ISO date
  endDate: string; // ISO date
  progressPercent: number;
  parentTaskId: string | null;
  assignedUserId: string | null;
  dependencies: string[]; // predecessor task ids
}

export interface ProjectDto {
  id: string;
  name: string;
  code: string;
  status: string; // Planned | Active | Completed
  startDate: string; // ISO date
  endDate: string | null; // ISO date
  tasks: GanttTaskDto[];
}

// organizationId is NOT sent -- the server derives it from the caller's JWT (see
// ProjectEndpoints), same convention as every other create payload.
export interface CreateProjectPayload {
  name: string;
  code: string;
  startDate: string; // ISO date
}

export interface AssignGanttTaskUserPayload {
  userId: string | null;
}

export interface CreateGanttTaskPayload {
  name: string;
  startDate: string; // ISO date
  endDate: string; // ISO date
  parentTaskId: string | null;
}

export interface RescheduleGanttTaskPayload {
  startDate: string; // ISO date
  endDate: string; // ISO date
}

export interface UpdateGanttTaskProgressPayload {
  progressPercent: number; // 0..100
}

// Serialized as its name -- the API registers JsonStringEnumConverter.
export type DependencyType = 'FinishToStart' | 'StartToStart' | 'FinishToFinish' | 'StartToFinish';

export interface AddGanttTaskDependencyPayload {
  predecessorTaskId: string;
  type: DependencyType;
  lagDays: number; // negative = lead
}
