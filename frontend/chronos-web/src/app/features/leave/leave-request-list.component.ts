import { Component, inject, signal } from '@angular/core';
import { LeaveService } from '../../core/api/leave.service';

@Component({
  selector: 'chronos-leave-request-list',
  standalone: true,
  template: `
    @if (resource.isLoading()) {
      <p>Loading...</p>
    }
    @if (resource.error()) {
      <p class="error">Could not load leave requests.</p>
    }
    <ul>
      @for (request of resource.value(); track request.id) {
        <li>
          {{ request.type }} · {{ request.startDate }} to {{ request.endDate }} · {{ request.status }}
          @if (request.status === 'Pending') {
            <button (click)="approve(request.id)">Approve</button>
            @if (rejectingId() === request.id) {
              <input
                [value]="rejectionReason()"
                (input)="rejectionReason.set($any($event.target).value)"
                placeholder="Reason" />
              <button (click)="confirmReject(request.id)">Confirm reject</button>
            } @else {
              <button (click)="rejectingId.set(request.id)">Reject</button>
            }
            <button (click)="cancel(request.id)">Cancel</button>
          }
        </li>
      }
    </ul>
  `
})
export class LeaveRequestListComponent {
  private readonly leaveService = inject(LeaveService);
  protected readonly resource = this.leaveService.leaveRequestsResource();
  protected readonly rejectingId = signal<string | null>(null);
  protected readonly rejectionReason = signal('');

  refresh(): void {
    this.resource.reload();
  }

  // approve/reject/cancel: the server derives approver/requester identity and the
  // ApproverOrAdmin authorization check from the caller's JWT (see LeaveEndpoints), so this
  // component only needs the leave request id, never the current user's own id.
  approve(id: string): void {
    this.leaveService.approve(id).subscribe(() => this.resource.reload());
  }

  confirmReject(id: string): void {
    const reason = this.rejectionReason();
    if (!reason) return;

    this.leaveService.reject(id, reason).subscribe(() => {
      this.rejectingId.set(null);
      this.rejectionReason.set('');
      this.resource.reload();
    });
  }

  cancel(id: string): void {
    this.leaveService.cancel(id).subscribe(() => this.resource.reload());
  }
}
