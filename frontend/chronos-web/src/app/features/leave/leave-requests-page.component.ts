import { Component } from '@angular/core';
import { LeaveRequestFormComponent } from './leave-request-form.component';
import { LeaveRequestListComponent } from './leave-request-list.component';

// Routed at /leave (see app.routes.ts), behind authGuard. No inputs needed: organizationId and
// the current user's identity both come from the JWT, on both child components.
@Component({
  selector: 'chronos-leave-requests-page',
  standalone: true,
  imports: [LeaveRequestFormComponent, LeaveRequestListComponent],
  template: `
    <div class="page">
      <header class="page-header">
        <h1>Leave requests</h1>
      </header>
      <section class="card">
        <h2>New request</h2>
        <chronos-leave-request-form (created)="list.refresh()" />
      </section>
      <section class="card">
        <h2>Your requests</h2>
        <chronos-leave-request-list #list />
      </section>
    </div>
  `
})
export class LeaveRequestsPageComponent {}
