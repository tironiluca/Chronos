import { Injectable } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { UserDirectoryEntryDto } from './user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  // Any org member -- the caller's own organization, id/display name/department only. Feeds
  // assignee pickers (Kanban cards, Gantt tasks).
  directoryResource() {
    return httpResource<UserDirectoryEntryDto[]>(() => '/api/users/directory');
  }
}
