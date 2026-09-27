import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { LoginPayload, LoginResult, RegisterPayload, UserRole } from './auth.model';

const STORAGE_KEY = 'chronos.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly session = signal<LoginResult | null>(this.readStoredSession());

  // No refresh-token flow yet: once the token expires the user is treated as logged out
  // and must log in again. Fine for a scaffold, worth revisiting before real usage.
  readonly isAuthenticated = computed(() => {
    const current = this.session();
    return current !== null && new Date(current.expiresAtUtc).getTime() > Date.now();
  });

  readonly token = computed(() => this.session()?.token ?? null);
  readonly userId = computed(() => this.session()?.userId ?? null);
  readonly organizationId = computed(() => this.session()?.organizationId ?? null);
  readonly displayName = computed(() => this.session()?.displayName ?? null);
  readonly role = computed<UserRole | null>(() => this.session()?.role ?? null);

  login(payload: LoginPayload) {
    return this.http
      .post<LoginResult>('/api/auth/login', payload)
      .pipe(tap((result) => this.setSession(result)));
  }

  register(payload: RegisterPayload) {
    return this.http.post<string>('/api/auth/register', payload);
  }

  logout(): void {
    this.session.set(null);
    this.clearStoredSession();
    this.router.navigateByUrl('/login');
  }

  hasRole(...roles: UserRole[]): boolean {
    const current = this.role();
    return current !== null && roles.includes(current);
  }

  private setSession(result: LoginResult): void {
    this.session.set(result);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(result));
    } catch {
      // Storage can be unavailable (private browsing, quota) -- the session still works
      // in-memory for this tab, it just won't survive a reload.
    }
  }

  private readStoredSession(): LoginResult | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as LoginResult) : null;
    } catch {
      return null;
    }
  }

  private clearStoredSession(): void {
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Nothing to do if storage is unavailable -- in-memory state is already cleared.
    }
  }
}
