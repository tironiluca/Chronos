export interface GanttTaskDto {
  id: string;
  name: string;
  startDate: string; // ISO date
  endDate: string;   // ISO date
  progressPercent: number;
  dependencies: string[]; // predecessor task ids
}

export interface ProjectDto {
  id: string;
  name: string;
  code: string;
  status: string;
  tasks: GanttTaskDto[];
}
