import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RegisterOrganizationPayload } from './organization.model';

@Injectable({ providedIn: 'root' })
export class OrganizationService {
  private readonly http = inject(HttpClient);

  register(payload: RegisterOrganizationPayload) {
    return this.http.post<string>('/api/organizations', payload);
  }
}
