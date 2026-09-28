import { Injectable, inject } from '@angular/core';
import { HttpClient, httpResource } from '@angular/common/http';
import { CreateDepartmentPayload, DepartmentDto } from './department.model';

@Injectable({ providedIn: 'root' })
export class DepartmentService {
  private readonly http = inject(HttpClient);

  // Always the caller's own organization -- same convention as LeaveService. Any authenticated
  // user may list departments (needed to populate pickers), only Admins may create one.
  departmentsResource() {
    return httpResource<DepartmentDto[]>(() => '/api/departments');
  }

  create(payload: CreateDepartmentPayload) {
    return this.http.post<string>('/api/departments', payload);
  }
}
