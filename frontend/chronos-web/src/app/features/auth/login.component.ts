import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { IconComponent } from '../../shared/ui/icon.component';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'chronos-login',
  standalone: true,
  imports: [RouterLink, IconComponent],
  template: `
    <div class="auth-page">
      <a class="auth-brand" routerLink="/login">
        <span class="brand-mark"><chronos-icon name="clock" [size]="18" [strokeWidth]="2.2" /></span>
        Chronos
      </a>
      <form class="auth-card" (submit)="onSubmit($event)">
        <h1>Log in</h1>
        <p class="auth-subtitle">Welcome back. Sign in to your workspace.</p>
        <label>
          Email
          <input type="email" autocomplete="email" inputmode="email" [value]="email()" (input)="email.set($any($event.target).value)" />
        </label>
        <label>
          Password
          <input type="password" autocomplete="current-password" [value]="password()" (input)="password.set($any($event.target).value)" />
        </label>
        <button type="submit" [disabled]="submitting()">Log in</button>
        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
        <div class="auth-footer">
          <p>New here? <a routerLink="/register">Create an account</a></p>
          <p>Setting up a team? <a routerLink="/register-organization">Register your organization</a></p>
        </div>
      </form>
    </div>
  `,
  styleUrl: './auth.styles.scss'
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly email = signal('');
  protected readonly password = signal('');
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  onSubmit(event: Event): void {
    event.preventDefault();
    this.error.set(null);
    this.submitting.set(true);

    this.authService.login({ email: this.email(), password: this.password() }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigateByUrl('/leave');
      },
      error: () => {
        this.submitting.set(false);
        this.error.set('Invalid email or password.');
      }
    });
  }
}
