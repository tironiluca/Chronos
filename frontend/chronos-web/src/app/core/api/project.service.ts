import { Injectable } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { ProjectDto } from './project.model';

@Injectable({ providedIn: 'root' })
export class ProjectService {
  // httpResource (stable in Angular 22): exposes .value() / .isLoading() / .error() as signals
  // and re-fetches automatically whenever the reactive projectId source changes.
  projectResource(projectId: () => string) {
    return httpResource<ProjectDto>(() => `/api/projects/${projectId()}`);
  }
}
