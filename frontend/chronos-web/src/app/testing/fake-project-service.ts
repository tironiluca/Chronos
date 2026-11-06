import { computed, signal } from '@angular/core';
import { of } from 'rxjs';
import { GanttTaskDto, ProjectDto } from '../core/api/project.model';

export function makeTask(overrides: Partial<GanttTaskDto> & Pick<GanttTaskDto, 'id' | 'startDate' | 'endDate'>): GanttTaskDto {
  return {
    projectId: 'project-1',
    name: `Task ${overrides.id}`,
    progressPercent: 0,
    parentTaskId: null,
    assignedUserId: null,
    dependencies: [],
    ...overrides
  };
}

export function makeProject(overrides: Partial<ProjectDto> & Pick<ProjectDto, 'id'>): ProjectDto {
  return {
    name: `Project ${overrides.id}`,
    code: overrides.id.toUpperCase(),
    status: 'Planned',
    startDate: '2026-01-01',
    endDate: null,
    tasks: [],
    ...overrides
  };
}

// Fakes ProjectService at the resource level (not the HTTP transport), same convention as the
// other page specs. projectResource records the reactive id source so tests can assert on it.
export function createFakeProjectService(projects: ProjectDto[] = []) {
  const projectsReload = jest.fn();
  const projectReload = jest.fn();
  const requestedIds: (() => string | null)[] = [];
  return {
    projectsResource: () => ({
      value: signal<ProjectDto[] | undefined>(projects),
      isLoading: signal(false),
      error: signal(undefined),
      reload: projectsReload
    }),
    projectResource: (id: () => string | null) => {
      requestedIds.push(id);
      return {
        // Lazy, like the real httpResource -- the id source may read a required input that
        // isn't set until after construction.
        value: computed<ProjectDto | undefined>(() => projects.find((p) => p.id === id())),
        isLoading: signal(false),
        error: signal(undefined),
        reload: projectReload
      };
    },
    create: jest.fn().mockReturnValue(of('project-new')),
    createTask: jest.fn().mockReturnValue(of('task-new')),
    projectsReload,
    projectReload,
    requestedIds
  };
}
