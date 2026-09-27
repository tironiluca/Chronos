import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';

const activeSession = {
  userId: 'u1',
  organizationId: 'org-1',
  email: 'luca@example.com',
  displayName: 'Luca',
  role: 'Approver',
  token: 'jwt-token',
  expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString()
};

describe('AuthService', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('is not authenticated before login', () => {
    const service = TestBed.inject(AuthService);

    expect(service.isAuthenticated()).toBe(false);
  });

  it('becomes authenticated after a successful login and persists the session', () => {
    const service = TestBed.inject(AuthService);

    service.login({ email: 'luca@example.com', password: 'secret' }).subscribe();
    httpMock.expectOne('/api/auth/login').flush(activeSession);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.hasRole('Approver', 'Admin')).toBe(true);
    expect(JSON.parse(localStorage.getItem('chronos.session')!).token).toBe('jwt-token');
  });

  it('treats an expired stored session as not authenticated', () => {
    localStorage.setItem('chronos.session', JSON.stringify({
      ...activeSession,
      expiresAtUtc: new Date(Date.now() - 1000).toISOString()
    }));

    const service = TestBed.inject(AuthService); // first injection here reads the stored session

    expect(service.isAuthenticated()).toBe(false);
  });

  it('logout clears the session and stored data', () => {
    const service = TestBed.inject(AuthService);
    service.login({ email: 'luca@example.com', password: 'secret' }).subscribe();
    httpMock.expectOne('/api/auth/login').flush(activeSession);

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('chronos.session')).toBeNull();
  });
});
