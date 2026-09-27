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
    <h1>Leave requests</h1>
    <chronos-leave-request-form (created)="list.refresh()" />
    <chronos-leave-request-list #list />
  `
})
export class LeaveRequestsPageComponent {}
