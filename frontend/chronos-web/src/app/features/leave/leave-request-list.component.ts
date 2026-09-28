import { Component, inject, signal } from '@angular/core';
import { LeaveService } from '../../core/api/leave.service';

@Component({
  selector: 'chronos-leave-request-list',
  standalone: true,
  template: `
    @if (resource.isLoading()) {
      <p class="empty-state">Loading...</p>
    }
    @if (resource.error()) {
      <p class="error">Could not load leave requests.</p>
    }
    <ul>
      @for (request of resource.value(); track request.id) {
        <li class="item-row">
          <span>
            {{ request.type }} · {{ request.startDate }} to {{ request.endDate }}
            <span class="badge" [class]="'badge-' + request.status.toLowerCase()">{{ request.status }}</span>
          </span>
          @if (request.status === 'Pending') {
            <span class="actions">
              <button type="button" class="btn-success" (click)="approve(request.id)">Approve</button>
              @if (rejectingId() === request.id) {
                <input
                  [value]="rejectionReason()"
                  (input)="rejectionReason.set($any($event.target).value)"
                  placeholder="Reason" />
                <button type="button" class="btn-danger" (click)="confirmReject(request.id)">Confirm reject</button>
              } @else {
                <button type="button" (click)="rejectingId.set(request.id)">Reject</button>
              }
              <button type="button" class="btn-ghost" (click)="cancel(request.id)">Cancel</button>
            </span>
          }
        </li>
      } @empty {
        <li class="empty-state">No leave requests yet.</li>
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
