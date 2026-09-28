import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { UserDto } from './user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);

  // AdminOnly server-side (see UserEndpoints) -- callers without the Admin role get a 403,
  // which is surfaced to the UI as a plain error, not special-cased here.
  getByEmail(email: string) {
    return this.http.get<UserDto>('/api/users', { params: { email } });
  }
}
