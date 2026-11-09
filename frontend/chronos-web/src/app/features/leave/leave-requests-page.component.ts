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
        <div>
          <h1>Leave</h1>
          <p class="page-subtitle">Request time off and keep track of approvals.</p>
        </div>
      </header>
      <div class="layout-split">
        <section class="card">
          <div class="card-header">
            <div>
              <h2>New request</h2>
              <p class="card-subtitle">Approvers review it from the requests list.</p>
            </div>
          </div>
          <chronos-leave-request-form (created)="list.refresh()" />
        </section>
        <section class="card">
          <div class="card-header">
            <h2>Requests</h2>
          </div>
          <chronos-leave-request-list #list />
        </section>
      </div>
    </div>
  `
})
export class LeaveRequestsPageComponent {}
