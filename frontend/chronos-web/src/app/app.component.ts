import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { AuthService } from './core/auth/auth.service';
import { IconComponent, IconName } from './shared/ui/icon.component';

interface NavItem {
  path: string;
  label: string;
  icon: IconName;
  group: 'Workspace' | 'Planning' | 'Organization';
  // Shown in the mobile bottom tab bar; everything else lives under "More".
  tab: boolean;
}

const NAV: NavItem[] = [
  { path: '/leave', label: 'Leave', icon: 'leave', group: 'Workspace', tab: true },
  { path: '/availability', label: 'Availability', icon: 'availability', group: 'Workspace', tab: true },
  { path: '/projects', label: 'Projects', icon: 'projects', group: 'Planning', tab: true },
  { path: '/boards', label: 'Boards', icon: 'boards', group: 'Planning', tab: true },
  { path: '/project-load', label: 'Project load', icon: 'load', group: 'Planning', tab: false },
  { path: '/departments', label: 'Departments', icon: 'departments', group: 'Organization', tab: false }
];

// App shell. Desktop (>= 1024px): fixed sidebar. Phones/tablets: compact top bar + bottom tab bar
// with a "More" sheet for the remaining destinations and the account actions. Logged-out pages
// (login/register) render without any chrome.
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, IconComponent],
  template: `
    @if (authService.isAuthenticated()) {
      <div class="shell">
        <aside class="sidebar" aria-label="Main navigation">
          <a class="brand" routerLink="/leave">
            <span class="brand-mark"><chronos-icon name="clock" [size]="18" [strokeWidth]="2.2" /></span>
            <span class="brand-name">Chronos</span>
          </a>

          <nav class="sidebar-nav">
            @for (group of groups; track group) {
              <div class="nav-group">
                <span class="nav-group-label">{{ group }}</span>
                @for (item of itemsIn(group); track item.path) {
                  <a class="nav-item" [routerLink]="item.path" routerLinkActive="active" ariaCurrentWhenActive="page">
                    <chronos-icon [name]="item.icon" [size]="18" />
                    <span>{{ item.label }}</span>
                  </a>
                }
              </div>
            }
          </nav>

          <div class="sidebar-user">
            <span class="avatar">{{ initials() }}</span>
            <span class="user-text">
              <span class="user-name">{{ authService.displayName() }}</span>
              <span class="user-role">{{ authService.role() }}</span>
            </span>
            <button type="button" class="btn-ghost btn-icon" title="Log out" aria-label="Log out" (click)="authService.logout()">
              <chronos-icon name="logout" [size]="18" />
            </button>
          </div>
        </aside>

        <div class="shell-main">
          <header class="topbar">
            <a class="brand" routerLink="/leave">
              <span class="brand-mark"><chronos-icon name="clock" [size]="16" [strokeWidth]="2.2" /></span>
              <span class="brand-name">Chronos</span>
            </a>
            <span class="topbar-title">{{ currentTitle() }}</span>
            <button type="button" class="avatar avatar-button" aria-label="Account and more" (click)="moreOpen.set(true)">
              {{ initials() }}
            </button>
          </header>

          <main class="content">
            <router-outlet />
          </main>
        </div>

        <nav class="tabbar" aria-label="Main navigation">
          @for (item of tabItems; track item.path) {
            <a class="tab" [routerLink]="item.path" routerLinkActive="active" ariaCurrentWhenActive="page">
              <chronos-icon [name]="item.icon" [size]="22" />
              <span>{{ item.label }}</span>
            </a>
          }
          <button type="button" class="tab" [class.active]="moreActive()" aria-label="More" (click)="moreOpen.set(true)">
            <chronos-icon name="more" [size]="22" [strokeWidth]="3" />
            <span>More</span>
          </button>
        </nav>

        @if (moreOpen()) {
          <div class="sheet-backdrop" (click)="moreOpen.set(false)"></div>
          <section class="sheet" role="dialog" aria-modal="true" aria-label="More">
            <div class="sheet-handle"></div>
            <div class="sheet-user">
              <span class="avatar avatar-lg">{{ initials() }}</span>
              <span class="user-text">
                <span class="user-name">{{ authService.displayName() }}</span>
                <span class="user-role">{{ authService.role() }}</span>
              </span>
            </div>
            <div class="sheet-links">
              @for (item of moreItems; track item.path) {
                <a class="sheet-link" [routerLink]="item.path" routerLinkActive="active" (click)="moreOpen.set(false)">
                  <span class="icon-tile"><chronos-icon [name]="item.icon" [size]="18" /></span>
                  <span>{{ item.label }}</span>
                  <chronos-icon class="sheet-chevron" name="chevron-right" [size]="18" />
                </a>
              }
            </div>
            <button type="button" class="btn-danger btn-block" (click)="logout()">
              <chronos-icon name="logout" [size]="18" /> Log out
            </button>
          </section>
        }
      </div>
    } @else {
      <router-outlet />
    }
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent {
  protected readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly groups = ['Workspace', 'Planning', 'Organization'] as const;
  protected readonly tabItems = NAV.filter((item) => item.tab);
  protected readonly moreItems = NAV.filter((item) => !item.tab);
  protected readonly moreOpen = signal(false);

  private readonly url = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects)
    ),
    { initialValue: this.router.url }
  );

  protected readonly currentTitle = computed(() => {
    const url = this.url();
    return NAV.find((item) => url.startsWith(item.path))?.label ?? '';
  });

  protected readonly moreActive = computed(() => this.moreItems.some((item) => this.url().startsWith(item.path)));

  protected readonly initials = computed(() =>
    (this.authService.displayName() ?? '')
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]!.toUpperCase())
      .join('')
  );

  itemsIn(group: NavItem['group']): NavItem[] {
    return NAV.filter((item) => item.group === group);
  }

  logout(): void {
    this.moreOpen.set(false);
    this.authService.logout();
  }
}
