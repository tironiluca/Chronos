import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    @if (authService.isAuthenticated()) {
      <nav class="app-nav">
        <span class="brand">Chronos</span>
        <a routerLink="/leave">Leave</a>
        <span class="spacer"></span>
        <span class="user">{{ authService.displayName() }}</span>
        <button type="button" (click)="authService.logout()">Log out</button>
      </nav>
    }
    <router-outlet />
  `,
  styles: `
    .app-nav {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 0.75rem 1.5rem;
      background: var(--color-nav-bg);
      color: var(--color-nav-text);
      font-family: var(--font-family-base);

      .brand {
        font-weight: 600;
      }

      a {
        color: var(--color-nav-text-muted);
        text-decoration: none;

        &:hover {
          color: var(--color-nav-text);
        }
      }

      .spacer {
        flex: 1;
      }

      .user {
        font-size: 0.875rem;
        color: var(--color-nav-text-muted);
      }

      button {
        padding: 0.4rem 0.75rem;
        border: none;
        border-radius: var(--radius-sm);
        background: var(--color-nav-button-bg);
        color: var(--color-nav-text);
        font-family: inherit;
        cursor: pointer;

        &:hover {
          background: var(--color-nav-button-bg-hover);
        }
      }
    }
  `
})
export class AppComponent {
  protected readonly authService = inject(AuthService);
}
