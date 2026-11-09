import { Component, inject, signal } from '@angular/core';
import { LeaveService } from '../../core/api/leave.service';
import { IconComponent } from '../../shared/ui/icon.component';
import { dayCount, formatDateRange } from '../../shared/format';

@Component({
  selector: 'chronos-leave-request-list',
  standalone: true,
  imports: [IconComponent],
  template: `
    @if (resource.isLoading() && !resource.value()) {
      <div class="skeleton-list">
        <div class="skeleton"></div>
        <div class="skeleton"></div>
        <div class="skeleton"></div>
      </div>
    }
    @if (resource.error()) {
      <p class="error">Could not load leave requests.</p>
    }
    <ul class="leave-list">
      @for (request of resource.value(); track request.id) {
        <li class="leave-row">
          <span class="icon-tile"><chronos-icon name="leave" [size]="18" /></span>
          <div class="leave-body">
            <div class="leave-top">
              <span class="item-title">{{ typeLabel(request.type) }}</span>
              <span class="badge" [class]="'badge badge-' + request.status.toLowerCase()">{{ request.status }}</span>
            </div>
            <span class="item-meta">
              {{ range(request.startDate, request.endDate) }} · {{ days(request.startDate, request.endDate) }}
              {{ days(request.startDate, request.endDate) === 1 ? 'day' : 'days' }}
            </span>
            @if (request.status === 'Pending') {
              <div class="actions leave-actions">
                <button type="button" class="btn-success btn-sm" (click)="approve(request.id)">
                  <chronos-icon name="check" [size]="15" /> Approve
                </button>
                @if (rejectingId() === request.id) {
                  <input
                    class="reason"
                    [value]="rejectionReason()"
                    (input)="rejectionReason.set($any($event.target).value)"
                    placeholder="Reason for rejecting" />
                  <button type="button" class="btn-danger btn-sm" (click)="confirmReject(request.id)">Confirm reject</button>
                } @else {
                  <button type="button" class="btn-sm" (click)="rejectingId.set(request.id)">Reject</button>
                }
                <button type="button" class="btn-ghost btn-sm" (click)="cancel(request.id)">Cancel</button>
              </div>
            }
          </div>
        </li>
      } @empty {
        @if (!resource.isLoading()) {
          <li class="empty">
            <span class="empty-icon"><chronos-icon name="inbox" /></span>
            <strong>No leave requests yet</strong>
            <p>Requests you submit show up here with their approval status.</p>
          </li>
        }
      }
    </ul>
  `,
  styles: `
    .leave-row {
      display: flex;
      gap: var(--space-3);
      padding: var(--space-3) 0;
      border-bottom: 1px solid var(--color-border-light);

      &:first-child {
        padding-top: 0;
      }

      &:last-child {
        border-bottom: none;
        padding-bottom: 0;
      }
    }

    .leave-body {
      display: flex;
      flex-direction: column;
      gap: 2px;
      min-width: 0;
      flex: 1;
    }

    .leave-top {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-2);
    }

    .leave-actions {
      margin-top: var(--space-2);
    }

    .reason {
      height: 32px;
      flex: 1 1 180px;
      font-size: var(--font-size-sm);
    }

    .skeleton-list {
      display: flex;
      flex-direction: column;
      gap: var(--space-3);

      .skeleton {
        height: 44px;
      }
    }
  `
})
export class LeaveRequestListComponent {
  private readonly leaveService = inject(LeaveService);
  protected readonly resource = this.leaveService.leaveRequestsResource();
  protected readonly rejectingId = signal<string | null>(null);
  protected readonly rejectionReason = signal('');

  private static readonly typeLabels: Record<string, string> = {
    Vacation: 'Vacation',
    SickLeave: 'Sick leave',
    Unpaid: 'Unpaid leave',
    Compensatory: 'Compensatory time'
  };

  protected typeLabel(type: string): string {
    return LeaveRequestListComponent.typeLabels[type] ?? type;
  }

  protected range(start: string, end: string): string {
    return formatDateRange(start, end);
  }

  protected days(start: string, end: string): number {
    return dayCount(start, end);
  }

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
