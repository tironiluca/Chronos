import { Injectable } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { ResourceAvailabilityDto } from './resource-availability.model';

export interface ResourceAvailabilityParams {
  departmentId: string | null;
  projectId: string | null;
  from: string; // ISO date (yyyy-MM-dd)
  to: string; // ISO date (yyyy-MM-dd)
}

@Injectable({ providedIn: 'root' })
export class ResourceAvailabilityService {
  // Reactive source re-fetches whenever departmentId/projectId/from/to change (e.g. on month
  // navigation or filter change) -- same httpResource pattern as ProjectService. organizationId
  // is never a param: the server derives the caller's org from the JWT (see
  // ResourceAvailabilityEndpoints).
  availabilityResource(params: () => ResourceAvailabilityParams) {
    return httpResource<ResourceAvailabilityDto[]>(() => {
      const p = params();
      const search = new URLSearchParams({ from: p.from, to: p.to });
      if (p.departmentId) search.set('departmentId', p.departmentId);
      if (p.projectId) search.set('projectId', p.projectId);
      return `/api/resources/availability?${search.toString()}`;
    });
  }
}
