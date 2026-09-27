import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'chronos-register',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="auth-page">
      <form class="auth-card" (submit)="onSubmit($event)">
        <h1>Create account</h1>
        <label>
          Organization ID
          <input
            type="text"
            placeholder="GUID provided by your organization"
            [value]="organizationId()"
            (input)="organizationId.set($any($event.target).value)"
          />
        </label>
        <p><a routerLink="/register-organization">Setting up a new organization? Register it here</a></p>
        <label>
          Display name
          <input type="text" [value]="displayName()" (input)="displayName.set($any($event.target).value)" />
        </label>
        <label>
          Email
          <input type="email" [value]="email()" (input)="email.set($any($event.target).value)" />
        </label>
        <label>
          Password
          <input type="password" [value]="password()" (input)="password.set($any($event.target).value)" />
        </label>
        <button type="submit" [disabled]="submitting()">Register</button>
        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
        @if (success()) {
          <p class="success">Account created. You can now log in.</p>
        }
        <p><a routerLink="/login">Already have an account? Log in</a></p>
      </form>
    </div>
  `,
  styleUrl: './auth.styles.scss'
})
export class RegisterComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly organizationId = signal('');
  protected readonly displayName = signal('');
  protected readonly email = signal('');
  protected readonly password = signal('');
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal(false);

  onSubmit(event: Event): void {
    event.preventDefault();
    this.error.set(null);
    this.success.set(false);
    this.submitting.set(true);

    this.authService
      .register({
        organizationId: this.organizationId(),
        displayName: this.displayName(),
        email: this.email(),
        password: this.password()
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.success.set(true);
          setTimeout(() => this.router.navigateByUrl('/login'), 1000);
        },
        error: (err) => {
          this.submitting.set(false);
          this.error.set(err?.error ?? 'Registration failed. Check the organization ID and try again.');
        }
      });
  }
}
