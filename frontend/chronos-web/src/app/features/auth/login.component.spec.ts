import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => httpMock.verify());

  it('logs in and navigates to /leave on success', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    const navigateSpy = jest.spyOn(router, 'navigateByUrl');

    const instance = fixture.componentInstance as unknown as {
      email: { set(v: string): void };
      password: { set(v: string): void };
    };
    instance.email.set('luca@example.com');
    instance.password.set('secret');

    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));

    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.body).toEqual({ email: 'luca@example.com', password: 'secret' });
    req.flush({
      userId: 'u1',
      organizationId: 'org-1',
      email: 'luca@example.com',
      displayName: 'Luca',
      role: 'Employee',
      token: 'jwt',
      expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString()
    });

    expect(navigateSpy).toHaveBeenCalledWith('/leave');
  });

  it('shows an error message on invalid credentials', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();

    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));

    const req = httpMock.expectOne('/api/auth/login');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.error').textContent).toContain('Invalid email or password.');
  });
});
