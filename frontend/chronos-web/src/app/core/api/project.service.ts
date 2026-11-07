import { Injectable, inject } from '@angular/core';
import { HttpClient, httpResource } from '@angular/common/http';
import {
  AddGanttTaskDependencyPayload,
  AssignGanttTaskUserPayload,
  CreateGanttTaskPayload,
  CreateProjectPayload,
  ProjectDto,
  RescheduleGanttTaskPayload,
  UpdateGanttTaskProgressPayload
} from './project.model';

@Injectable({ providedIn: 'root' })
export class ProjectService {
  private readonly http = inject(HttpClient);

  // Always the caller's own organization; each project carries its tasks inline (see
  // ProjectDto server-side), which is what the project x ISO-week load grid buckets.
  projectsResource() {
    return httpResource<ProjectDto[]>(() => '/api/projects');
  }

  // httpResource (stable in Angular 22): exposes .value() / .isLoading() / .error() as signals
  // and re-fetches automatically whenever the reactive projectId source changes. A null/empty id
  // maps to undefined, which httpResource treats as "don't fetch".
  projectResource(projectId: () => string | null) {
    return httpResource<ProjectDto>(() => {
      const id = projectId();
      return id ? `/api/projects/${id}` : undefined;
    });
  }

  create(payload: CreateProjectPayload) {
    return this.http.post<string>('/api/projects', payload);
  }

  createTask(projectId: string, payload: CreateGanttTaskPayload) {
    return this.http.post<string>(`/api/projects/${projectId}/tasks`, payload);
  }

  assignTaskUser(projectId: string, taskId: string, payload: AssignGanttTaskUserPayload) {
    return this.http.patch<void>(`/api/projects/${projectId}/tasks/${taskId}/assignee`, payload);
  }

  rescheduleTask(projectId: string, taskId: string, payload: RescheduleGanttTaskPayload) {
    return this.http.patch<void>(`/api/projects/${projectId}/tasks/${taskId}/schedule`, payload);
  }

  updateTaskProgress(projectId: string, taskId: string, payload: UpdateGanttTaskProgressPayload) {
    return this.http.patch<void>(`/api/projects/${projectId}/tasks/${taskId}/progress`, payload);
  }

  // The server rejects predecessors outside the project and edges that would close a cycle.
  addTaskDependency(projectId: string, taskId: string, payload: AddGanttTaskDependencyPayload) {
    return this.http.post<string>(`/api/projects/${projectId}/tasks/${taskId}/dependencies`, payload);
  }

  removeTaskDependency(projectId: string, taskId: string, predecessorTaskId: string) {
    return this.http.delete<void>(`/api/projects/${projectId}/tasks/${taskId}/dependencies/${predecessorTaskId}`);
  }
}
