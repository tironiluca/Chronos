import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { IconComponent } from '../../shared/ui/icon.component';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'chronos-register',
  standalone: true,
  imports: [RouterLink, IconComponent],
  template: `
    <div class="auth-page">
      <a class="auth-brand" routerLink="/login">
        <span class="brand-mark"><chronos-icon name="clock" [size]="18" [strokeWidth]="2.2" /></span>
        Chronos
      </a>
      <form class="auth-card" (submit)="onSubmit($event)">
        <h1>Create account</h1>
        <p class="auth-subtitle">Join your organization&rsquo;s workspace.</p>
        <label>
          Organization ID
          <input
            type="text"
            placeholder="GUID provided by your organization"
            [value]="organizationId()"
            (input)="organizationId.set($any($event.target).value)"
          />
        </label>
        <p class="hint">Ask your admin for it, or <a routerLink="/register-organization">register a new organization</a>.</p>
        <label>
          Display name
          <input type="text" [value]="displayName()" (input)="displayName.set($any($event.target).value)" />
        </label>
        <label>
          Email
          <input type="email" autocomplete="email" inputmode="email" [value]="email()" (input)="email.set($any($event.target).value)" />
        </label>
        <label>
          Password
          <input type="password" autocomplete="new-password" [value]="password()" (input)="password.set($any($event.target).value)" />
        </label>
        <button type="submit" [disabled]="submitting()">Register</button>
        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
        @if (success()) {
          <p class="success">Account created. You can now log in.</p>
        }
        <div class="auth-footer">
          <p>Already have an account? <a routerLink="/login">Log in</a></p>
        </div>
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
