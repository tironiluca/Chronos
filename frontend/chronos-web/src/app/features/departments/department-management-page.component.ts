import { Component, inject, signal } from '@angular/core';
import { DepartmentService } from '../../core/api/department.service';
import { DepartmentPickerComponent } from './department-picker.component';

// Routed at /departments (see app.routes.ts), behind authGuard. Creation is AdminOnly
// server-side (see DepartmentEndpoints) -- a non-Admin submitting still gets a clear error from
// the API rather than the button being hidden, since GET /api/users/me/rights isn't consulted
// here yet.
@Component({
  selector: 'chronos-department-management-page',
  standalone: true,
  imports: [DepartmentPickerComponent],
  template: `
    <div class="page">
      <header class="page-header">
        <h1>Departments</h1>
      </header>

      <section class="card">
        <h2>New department</h2>
        <form (submit)="onSubmit($event)">
          <label>
            Name
            <input type="text" [value]="name()" (input)="name.set($any($event.target).value)" />
          </label>
          <label>
            Code
            <input type="text" [value]="code()" (input)="code.set($any($event.target).value)" />
          </label>
          <label>
            Parent department
            <chronos-department-picker
              [selectedId]="parentDepartmentId()"
              [allOptionLabel]="'None (top-level)'"
              (selectionChange)="parentDepartmentId.set($event)" />
          </label>
          <button type="submit" [disabled]="submitting()">Create department</button>
          @if (error()) {
            <p class="error">{{ error() }}</p>
          }
        </form>
      </section>

      <section class="card">
        <h2>Existing departments</h2>
        <chronos-department-picker #list [includeAllOption]="false" />
      </section>
    </div>
  `
})
export class DepartmentManagementPageComponent {
  private readonly departmentService = inject(DepartmentService);

  protected readonly name = signal('');
  protected readonly code = signal('');
  protected readonly parentDepartmentId = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  onSubmit(event: Event): void {
    event.preventDefault();
    this.error.set(null);
    this.submitting.set(true);

    this.departmentService
      .create({ name: this.name(), code: this.code(), parentDepartmentId: this.parentDepartmentId() })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.name.set('');
          this.code.set('');
          this.parentDepartmentId.set(null);
        },
        error: (err) => {
          this.submitting.set(false);
          this.error.set(err?.error ?? 'Could not create department.');
        }
      });
  }
}
