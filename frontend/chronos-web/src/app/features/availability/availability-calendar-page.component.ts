import { IconComponent } from '../../shared/ui/icon.component';
import { Component, computed, inject, signal } from '@angular/core';
import { DepartmentPickerComponent } from '../departments/department-picker.component';
import { ProjectPickerComponent } from '../projects/project-picker.component';
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
// further by the department filter below -- inclusive of descendants, resolved server-side --
// and the project filter. Server-side the project filter scopes only AssignedTasks/AssignedCards,
// never ApprovedLeave (leave isn't project-related), so on its own it wouldn't change this grid --
// instead, with a project selected, rows narrow to people with work assigned in that project
// ("who on project X is away this month").
@Component({
  selector: 'chronos-availability-calendar-page',
  standalone: true,
  imports: [IconComponent, DepartmentPickerComponent, ProjectPickerComponent, LeaveGridComponent],
  template: `
    <div class="page">
      <header class="page-header">
        <div>
          <h1>Availability</h1>
          <p class="page-subtitle">Who is away, day by day. Approved leave only.</p>
        </div>
      </header>

      <div class="filters">
        <div class="stepper" role="group" aria-label="Month">
          <button type="button" aria-label="Previous month" (click)="shiftMonth(-1)"><chronos-icon name="chevron-left" /></button>
          <span class="stepper-label">{{ monthLabel() }}</span>
          <button type="button" aria-label="Next month" (click)="shiftMonth(1)"><chronos-icon name="chevron-right" /></button>
        </div>
        <chronos-department-picker
          [selectedId]="departmentId()"
          allOptionLabel="All departments"
          (selectionChange)="departmentId.set($event)" />
        <chronos-project-picker
          [selectedId]="projectId()"
          allOptionLabel="All projects"
          (selectionChange)="projectId.set($event)" />
      </div>

      @if (resource.error()) {
        <p class="error">Could not load availability.</p>
      }

      <section class="card card-flush">
        <div class="grid-head">
          <span class="legend">
            <span><span class="swatch on-leave-swatch"></span>On leave</span>
            <span><span class="swatch weekend-swatch"></span>Weekend</span>
          </span>
          @if (resource.isLoading()) {
            <span class="empty-state">Loading…</span>
          }
        </div>
        <div class="table-scroll">
          <chronos-leave-grid [availability]="rows()" [monthStart]="monthStartIso()" />
        </div>
      </section>
    </div>
  `,
  styles: `
    .filters {
      display: flex;
      flex-wrap: wrap;
      gap: var(--space-3);
      align-items: center;

      @media (max-width: 767px) {
        display: grid;
        grid-template-columns: 1fr 1fr;

        .stepper {
          grid-column: 1 / -1;
          justify-content: space-between;
        }
      }
    }

    .grid-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-3);
      padding: var(--space-3) var(--space-4);
      border-bottom: 1px solid var(--color-border-light);
    }

    .on-leave-swatch {
      background: color-mix(in srgb, var(--color-primary) 55%, var(--color-surface));
    }

    .weekend-swatch {
      background: var(--color-surface-2);
      border: 1px solid var(--color-border);
    }
  `
})
export class AvailabilityCalendarPageComponent {
  protected readonly departmentId = signal<string | null>(null);
  protected readonly projectId = signal<string | null>(null);
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
    projectId: this.projectId(),
    from: this.monthStartIso(),
    to: this.monthEndIso()
  }));

  protected readonly rows = computed(() => {
    const entries = this.resource.value() ?? [];
    return this.projectId()
      ? entries.filter((entry) => entry.assignedTasks.length > 0 || entry.assignedCards.length > 0)
      : entries;
  });

  shiftMonth(delta: number): void {
    this.monthOffset.update((value) => value + delta);
  }
}
