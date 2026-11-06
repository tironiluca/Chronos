import { signal } from '@angular/core';
import { UserDirectoryEntryDto } from '../core/api/user.model';

export const directoryUsers: UserDirectoryEntryDto[] = [
  { id: 'user-1', displayName: 'Anna', departmentId: null },
  { id: 'user-2', displayName: 'Bruno', departmentId: null }
];

export function createFakeUserService(users: UserDirectoryEntryDto[] = directoryUsers) {
  return {
    directoryResource: () => ({
      value: signal<UserDirectoryEntryDto[] | undefined>(users),
      isLoading: signal(false),
      error: signal(undefined),
      reload: jest.fn()
    })
  };
}
