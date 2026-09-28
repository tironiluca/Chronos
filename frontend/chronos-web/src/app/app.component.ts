import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    @if (authService.isAuthenticated()) {
      <nav class="app-nav">
        <span class="brand">Chronos</span>
        <a routerLink="/leave" routerLinkActive="active">Leave</a>
        <a routerLink="/availability" routerLinkActive="active">Availability</a>
        <a routerLink="/departments" routerLinkActive="active">Departments</a>
        <a routerLink="/boards" routerLinkActive="active">Boards</a>
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
      gap: 1.25rem;
      padding: 0.75rem 1.5rem;
      background: var(--color-nav-bg);
      color: var(--color-nav-text);
      font-family: var(--font-family-base);
      position: sticky;
      top: 0;
      z-index: 10;
      box-shadow: var(--shadow-md);

      .brand {
        font-weight: 700;
        letter-spacing: 0.01em;
        margin-right: 0.25rem;
      }

      a {
        color: var(--color-nav-text-muted);
        text-decoration: none;
        font-size: 0.875rem;
        font-weight: 500;
        padding: 0.35rem 0.65rem;
        border-radius: var(--radius-sm);
        transition: background 120ms ease, color 120ms ease;

        &:hover {
          color: var(--color-nav-text);
          background: var(--color-nav-button-bg);
        }

        &.active {
          color: var(--color-nav-text);
          background: var(--color-nav-button-bg);
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
