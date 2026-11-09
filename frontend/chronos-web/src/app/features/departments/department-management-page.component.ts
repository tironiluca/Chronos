import { Component, computed, inject, signal } from '@angular/core';
import { IconComponent } from '../../shared/ui/icon.component';
import { DepartmentService } from '../../core/api/department.service';
import { DepartmentPickerComponent, flattenTree } from './department-picker.component';

// Routed at /departments (see app.routes.ts), behind authGuard. Creation is AdminOnly
// server-side (see DepartmentEndpoints) -- a non-Admin submitting still gets a clear error from
// the API rather than the button being hidden, since GET /api/users/me/rights isn't consulted
// here yet.
@Component({
  selector: 'chronos-department-management-page',
  standalone: true,
  imports: [DepartmentPickerComponent, IconComponent],
  template: `
    <div class="page">
      <header class="page-header">
        <div>
          <h1>Departments</h1>
          <p class="page-subtitle">Organize people into teams. Departments can be nested.</p>
        </div>
      </header>

      <div class="layout-split">
        <section class="card">
          <div class="card-header">
            <div>
              <h2>New department</h2>
              <p class="card-subtitle">Admins only.</p>
            </div>
          </div>
          <form class="form-stack" (submit)="onSubmit($event)">
            <div class="form-row-2">
              <label>
                Name
                <input type="text" placeholder="Engineering" [value]="name()" (input)="name.set($any($event.target).value)" />
              </label>
              <label>
                Code
                <input type="text" placeholder="ENG" [value]="code()" (input)="code.set($any($event.target).value)" />
              </label>
            </div>
            <label>
              Parent department
              <chronos-department-picker
                [selectedId]="parentDepartmentId()"
                [allOptionLabel]="'None (top-level)'"
                (selectionChange)="parentDepartmentId.set($event)" />
            </label>
            @if (error()) {
              <p class="error">{{ error() }}</p>
            }
            <button type="submit" class="btn-block" [disabled]="submitting()">
              <chronos-icon name="plus" [size]="17" /> Create department
            </button>
          </form>
        </section>

        <section class="card">
          <div class="card-header">
            <h2>Structure</h2>
            <span class="badge badge-plain">{{ tree().length }}</span>
          </div>
          <ul class="tree">
            @for (department of tree(); track department.id) {
              <li class="tree-row" [style.--depth]="department.depth">
                <span class="icon-tile"><chronos-icon name="departments" [size]="17" /></span>
                <span class="item-title">{{ department.name }}</span>
                <span class="code">{{ department.code }}</span>
              </li>
            } @empty {
              @if (resource.isLoading()) {
                <li class="empty-state">Loading…</li>
              } @else {
                <li class="empty">
                  <span class="empty-icon"><chronos-icon name="departments" /></span>
                  <strong>No departments yet</strong>
                  <p>Create the first one with the form.</p>
                </li>
              }
            }
          </ul>
        </section>
      </div>
    </div>
  `,
  styles: `
    chronos-department-picker {
      display: flex;
      flex-direction: column;
      gap: 4px;
      font-weight: 400;
    }

    .tree {
      display: flex;
      flex-direction: column;
    }

    .tree-row {
      display: flex;
      align-items: center;
      gap: var(--space-3);
      padding: var(--space-2) 0 var(--space-2) calc(var(--depth) * 1.5rem);
      border-bottom: 1px solid var(--color-border-light);
      position: relative;

      &:last-child {
        border-bottom: none;
      }

      .item-title {
        flex: 1;
      }

      .code {
        font-size: var(--font-size-xs);
        font-weight: 700;
        letter-spacing: 0.05em;
        color: var(--color-text-muted);
      }

      .icon-tile {
        width: 32px;
        height: 32px;
        border-radius: 9px;
      }
    }
  `
})
export class DepartmentManagementPageComponent {
  private readonly departmentService = inject(DepartmentService);
  protected readonly resource = this.departmentService.departmentsResource();
  protected readonly tree = computed(() => flattenTree(this.resource.value() ?? []));

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
          this.resource.reload();
        },
        error: (err) => {
          this.submitting.set(false);
          this.error.set(err?.error ?? 'Could not create department.');
        }
      });
  }
}
