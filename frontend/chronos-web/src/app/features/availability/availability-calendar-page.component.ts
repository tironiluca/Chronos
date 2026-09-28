import { Component, computed, inject, signal } from '@angular/core';
import { DepartmentPickerComponent } from '../departments/department-picker.component';
import { LeaveGridComponent } from './leave-grid.component';
import { ResourceAvailabilityService } from '../../core/api/resource-availability.service';

function isoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function firstOfMonth(offsetMonths: number): Date {
  const now = new Date();
  return new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() + offsetMonths, 1));
}

function lastOfMonth(monthStart: Date): Date {
  return new Date(Date.UTC(monthStart.getUTCFullYear(), monthStart.getUTCMonth() + 1, 0));
}

// Routed at /availability (see app.routes.ts), behind authGuard. Cross-department: any
// authenticated org member may query availability (see ResourceAvailabilityEndpoints), scoped
// further by the department filter below -- inclusive of descendants, resolved server-side.
@Component({
  selector: 'chronos-availability-calendar-page',
  standalone: true,
  imports: [DepartmentPickerComponent, LeaveGridComponent],
  template: `
    <div class="page">
      <header class="page-header">
        <h1>Resource availability</h1>
      </header>
      <div class="toolbar">
        <button type="button" (click)="shiftMonth(-1)">‹ Prev</button>
        <span class="month-label">{{ monthLabel() }}</span>
        <button type="button" (click)="shiftMonth(1)">Next ›</button>
        <chronos-department-picker
          [selectedId]="departmentId()"
          allOptionLabel="All departments"
          (selectionChange)="departmentId.set($event)" />
      </div>

      @if (resource.isLoading()) {
        <p class="empty-state">Loading availability...</p>
      }
      @if (resource.error()) {
        <p class="error">Could not load availability.</p>
      }

      <div class="card table-scroll">
        <chronos-leave-grid [availability]="resource.value() ?? []" [monthStart]="monthStartIso()" />
      </div>
    </div>
  `,
  styles: `
    .month-label {
      font-weight: 600;
      min-width: 8rem;
      text-align: center;
    }
  `
})
export class AvailabilityCalendarPageComponent {
  protected readonly departmentId = signal<string | null>(null);
  private readonly monthOffset = signal(0);

  private readonly monthStart = computed(() => firstOfMonth(this.monthOffset()));
  protected readonly monthStartIso = computed(() => isoDate(this.monthStart()));
  private readonly monthEndIso = computed(() => isoDate(lastOfMonth(this.monthStart())));
  protected readonly monthLabel = computed(() =>
    this.monthStart().toLocaleDateString('en-US', { month: 'long', year: 'numeric', timeZone: 'UTC' })
  );

  private readonly availabilityService = inject(ResourceAvailabilityService);
  protected readonly resource = this.availabilityService.availabilityResource(() => ({
    departmentId: this.departmentId(),
    projectId: null,
    from: this.monthStartIso(),
    to: this.monthEndIso()
  }));

  shiftMonth(delta: number): void {
    this.monthOffset.update((value) => value + delta);
  }
}
