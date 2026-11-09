import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../shared/ui/icon.component';
import { OrganizationService } from '../../core/organizations/organization.service';

@Component({
  selector: 'chronos-register-organization',
  standalone: true,
  imports: [RouterLink, IconComponent],
  template: `
    <div class="auth-page">
      <a class="auth-brand" routerLink="/login">
        <span class="brand-mark"><chronos-icon name="clock" [size]="18" [strokeWidth]="2.2" /></span>
        Chronos
      </a>
      <form class="auth-card" (submit)="onSubmit($event)">
        <h1>Register organization</h1>
        <p class="auth-subtitle">Create a workspace for your team.</p>
        <label>
          Organization name
          <input type="text" [value]="name()" (input)="name.set($any($event.target).value)" />
        </label>
        <label>
          Organization code
          <input
            type="text"
            placeholder="Short unique code, e.g. ACME"
            [value]="code()"
            (input)="code.set($any($event.target).value)"
          />
        </label>
        <button type="submit" [disabled]="submitting()">Create organization</button>
        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
        @if (organizationId()) {
          <p class="success">
            Organization created. Its ID is
            <strong>{{ organizationId() }}</strong>
            &mdash; share it with your team so they can register.
          </p>
        }
        <div class="auth-footer">
          <p>Have an organization ID? <a routerLink="/register">Register as a user</a></p>
          <p>Already have an account? <a routerLink="/login">Log in</a></p>
        </div>
      </form>
    </div>
  `,
  styleUrl: '../auth/auth.styles.scss'
})
export class RegisterOrganizationComponent {
  private readonly organizationService = inject(OrganizationService);

  protected readonly name = signal('');
  protected readonly code = signal('');
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly organizationId = signal<string | null>(null);

  onSubmit(event: Event): void {
    event.preventDefault();
    this.error.set(null);
    this.organizationId.set(null);
    this.submitting.set(true);

    this.organizationService.register({ name: this.name(), code: this.code() }).subscribe({
      next: (id) => {
        this.submitting.set(false);
        this.organizationId.set(id);
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err?.error ?? 'Registration failed. Check the organization code and try again.');
      }
    });
  }
}
