import { Component, input } from '@angular/core';
import { LeaveRequestFormComponent } from './leave-request-form.component';
import { LeaveRequestListComponent } from './leave-request-list.component';

// Not registered in app.routes.ts yet: it needs organizationId/currentUserId from a real
// session, which doesn't exist in this scaffold. Route it once auth lands, binding
// organizationId from the URL (withComponentInputBinding()) and currentUserId from the
// authenticated user.
@Component({
  selector: 'chronos-leave-requests-page',
  standalone: true,
  imports: [LeaveRequestFormComponent, LeaveRequestListComponent],
  template: `
    <h1>Leave requests</h1>
    <chronos-leave-request-form
      [organizationId]="organizationId()"
      [requesterId]="currentUserId()"
      (created)="list.refresh()" />
    <chronos-leave-request-list
      #list
      [organizationId]="organizationId()"
      [currentUserId]="currentUserId()" />
  `
})
export class LeaveRequestsPageComponent {
  organizationId = input.required<string>();
  currentUserId = input.required<string>();
}
