import { Component, inject, input, output, signal } from '@angular/core';
import { LeaveService } from '../../core/api/leave.service';
import { LeaveType } from '../../core/api/leave.model';

// Plain signal-bound form rather than Angular Signal Forms: kept intentionally simple so its
// behaviour doesn't depend on an API surface still settling post-v22. Revisit once that's verified.
@Component({
  selector: 'chronos-leave-request-form',
  standalone: true,
  template: `
    <form (submit)="onSubmit($event)">
      <label>
        Type
        <select [value]="type()" (change)="type.set($any($event.target).value)">
          <option value="Vacation">Vacation</option>
          <option value="SickLeave">Sick leave</option>
          <option value="Unpaid">Unpaid</option>
          <option value="Compensatory">Compensatory</option>
        </select>
      </label>
      <label>
        Start date
        <input type="date" [value]="startDate()" (input)="startDate.set($any($event.target).value)" />
      </label>
      <label>
        End date
        <input type="date" [value]="endDate()" (input)="endDate.set($any($event.target).value)" />
      </label>
      <button type="submit" [disabled]="submitting()">Request</button>
      @if (error()) {
        <p class="error">{{ error() }}</p>
      }
    </form>
  `
})
export class LeaveRequestFormComponent {
  organizationId = input.required<string>();
  requesterId = input.required<string>();
  created = output<string>();

  private readonly leaveService = inject(LeaveService);

  protected readonly type = signal<LeaveType>('Vacation');
  protected readonly startDate = signal('');
  protected readonly endDate = signal('');
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  onSubmit(event: Event): void {
    event.preventDefault();
    this.error.set(null);
    this.submitting.set(true);

    this.leaveService
      .create({
        organizationId: this.organizationId(),
        requesterId: this.requesterId(),
        type: this.type(),
        startDate: this.startDate(),
        endDate: this.endDate()
      })
      .subscribe({
        next: (id) => {
          this.submitting.set(false);
          this.created.emit(id);
        },
        error: (err) => {
          this.submitting.set(false);
          this.error.set(err?.error ?? 'Request failed.');
        }
      });
  }
}
