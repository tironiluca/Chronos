import { Component, computed, inject, input, output } from '@angular/core';
import { DepartmentService } from '../../core/api/department.service';
import { DepartmentDto } from '../../core/api/department.model';

export interface DepartmentOption {
  id: string;
  name: string;
  code: string;
  depth: number;
}

// Flattens the department tree (parentDepartmentId) into a depth-first, indentation-ready list
// for a plain <select> -- reused wherever a department needs picking (department management's
// "parent department" field, the resource-availability calendar's department filter).
export function flattenTree(departments: DepartmentDto[]): DepartmentOption[] {
  const byParent = new Map<string | null, DepartmentDto[]>();
  for (const department of departments) {
    const siblings = byParent.get(department.parentDepartmentId) ?? [];
    siblings.push(department);
    byParent.set(department.parentDepartmentId, siblings);
  }

  const options: DepartmentOption[] = [];
  const visit = (parentId: string | null, depth: number) => {
    for (const department of byParent.get(parentId) ?? []) {
      options.push({ id: department.id, name: department.name, code: department.code, depth });
      visit(department.id, depth + 1);
    }
  };
  visit(null, 0);
  return options;
}

@Component({
  selector: 'chronos-department-picker',
  standalone: true,
  template: `
    <!-- [selected] per option, not [value] on <select>: the select's value binding is applied
         before @for renders its options, so a preselected id would silently not stick. -->
    <select (change)="onChange($any($event.target).value)">
      @if (includeAllOption()) {
        <option value="" [selected]="!selectedId()">{{ allOptionLabel() }}</option>
      }
      @for (option of options(); track option.id) {
        <option [value]="option.id" [selected]="option.id === selectedId()">{{ '  '.repeat(option.depth) }}{{ option.name }}</option>
      }
    </select>
    @if (resource.isLoading()) {
      <span class="empty-state">Loading departments...</span>
    }
    @if (resource.error()) {
      <span class="error">Could not load departments.</span>
    }
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      gap: 4px;
      min-width: 0;
    }

    select {
      width: 100%;
    }
  `
})
export class DepartmentPickerComponent {
  selectedId = input<string | null>(null);
  includeAllOption = input(true);
  allOptionLabel = input('All departments');
  selectionChange = output<string | null>();

  private readonly departmentService = inject(DepartmentService);
  protected readonly resource = this.departmentService.departmentsResource();
  protected readonly options = computed(() => flattenTree(this.resource.value() ?? []));

  onChange(value: string): void {
    this.selectionChange.emit(value === '' ? null : value);
  }
}
