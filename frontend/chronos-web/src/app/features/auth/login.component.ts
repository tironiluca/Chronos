import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'chronos-login',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="auth-page">
      <form class="auth-card" (submit)="onSubmit($event)">
        <h1>Log in</h1>
        <label>
          Email
          <input type="email" [value]="email()" (input)="email.set($any($event.target).value)" />
        </label>
        <label>
          Password
          <input type="password" [value]="password()" (input)="password.set($any($event.target).value)" />
        </label>
        <button type="submit" [disabled]="submitting()">Log in</button>
        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
        <p><a routerLink="/register">Need an account? Register</a></p>
        <p><a routerLink="/register-organization">New organization? Register it here</a></p>
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
