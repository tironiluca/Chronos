import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Never attach a token to the auth endpoints themselves -- avoids sending a stale token
  // alongside login/register and keeps those requests trivially testable in isolation.
  if (req.url.startsWith('/api/auth/')) {
    return next(req);
  }

  const authService = inject(AuthService);
  const token = authService.isAuthenticated() ? authService.token() : null;

  if (!token) {
    return next(req);
  }

  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
