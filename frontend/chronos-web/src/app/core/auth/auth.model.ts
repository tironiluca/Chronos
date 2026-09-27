export interface LoginPayload {
  email: string;
  password: string;
}

export interface RegisterPayload {
  organizationId: string;
  email: string;
  displayName: string;
  password: string;
}

export type UserRole = 'Employee' | 'Approver' | 'Admin';

export interface LoginResult {
  userId: string;
  organizationId: string;
  email: string;
  displayName: string;
  role: UserRole;
  token: string;
  expiresAtUtc: string;
}
